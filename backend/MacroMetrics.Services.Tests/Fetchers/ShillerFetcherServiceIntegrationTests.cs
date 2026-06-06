using System.Net;
using System.Text;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Services.Fetchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MacroMetrics.Services.Tests.Fetchers;

/// <summary>
/// In-process integration tests for <see cref="ShillerFetcherService"/>.
/// <para>
/// These tests wire the real <see cref="ShillerFetcherService"/> through
/// <see cref="IServiceCollection.AddHttpClient{TClient,TImplementation}"/> — the same DI
/// registration used in production — and stub only the HTTP boundary via a
/// <see cref="FakeHttpMessageHandler"/>. This verifies that:
/// <list type="bullet">
///   <item>the DI registration resolves correctly,</item>
///   <item>the full HTTP → deserialise → map pipeline produces the expected domain objects, and</item>
///   <item>error paths propagate as typed exceptions through the real implementation.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ShillerFetcherServiceIntegrationTests
{
    private const string ShillerBaseUrl = "https://www.multpl.com";

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="ServiceProvider"/> with the real
    /// <see cref="ShillerFetcherService"/> wired via <c>AddHttpClient</c>, using
    /// <paramref name="handler"/> as the primary HTTP handler so no real network
    /// calls are made.
    /// </summary>
    private static IShillerFetcherService BuildSut(FakeHttpMessageHandler handler)
    {
        var services = new ServiceCollection();

        services
            .AddHttpClient<IShillerFetcherService, ShillerFetcherService>(client =>
            {
                client.BaseAddress = new Uri(ShillerBaseUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IShillerFetcherService>();
    }

    private static string BuildShillerJson(params (string Date, double Value)[] points)
    {
        var items = string.Join(",", points.Select(p =>
            $$"""{"date":"{{p.Date}}","value":{{p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}"""));
        return $$"""{"data":[{{items}}]}""";
    }

    private static HttpResponseMessage OkJson(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    // ── DI resolution ─────────────────────────────────────────────────────

    [Fact]
    public void ServiceProvider_ResolvesShillerFetcherService()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson("""{"data":[]}"""));
        var sut = BuildSut(handler);

        Assert.NotNull(sut);
        Assert.IsType<ShillerFetcherService>(sut);
    }

    // ── Happy-path parsing through real DI wiring ─────────────────────────

    [Fact]
    public async Task FetchRawAsync_DiWired_ParsesDataAndReturnsCorrectPoints()
    {
        var json = BuildShillerJson(
            ("Jun 1, 2026", 35.24),
            ("May 1, 2026", 34.80));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("cape");

        Assert.Equal(2, result.Count);
        Assert.Equal("2026-06-01", result[0].Date);
        Assert.Equal(35.24, result[0].Value, precision: 5);
        Assert.Equal("2026-05-01", result[1].Date);
        Assert.Equal(34.80, result[1].Value, precision: 5);
    }

    [Fact]
    public async Task FetchRawAsync_DiWired_CallsCorrectEndpointPath()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson("""{"data":[]}"""));
        var sut = BuildSut(handler);

        await sut.FetchRawAsync("cape");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("/shiller-pe/table/by-month.json",
            handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    // ── Error propagation through DI wiring ──────────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task FetchRawAsync_DiWired_NonSuccessStatus_PropagatesFetcherException(
        HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(
            _ => new HttpResponseMessage(statusCode));
        var sut = BuildSut(handler);

        var ex = await Assert.ThrowsAsync<FetcherException>(
            () => sut.FetchRawAsync("cape"));

        Assert.Contains(((int)statusCode).ToString(), ex.Message);
    }

    // ── Catalogue cross-check ─────────────────────────────────────────────

    /// <summary>
    /// Verifies that the MetricCatalogueService no longer maps "cape" to MetricSource.Fred —
    /// a regression guard ensuring the catalogue fix holds.
    /// </summary>
    [Fact]
    public void MetricCatalogue_Cape_HasShillerSource()
    {
        var catalogue = new MacroMetrics.Services.Metrics.MetricCatalogueService();
        var cape = catalogue.GetAll()
            .FirstOrDefault(m => m.Id == MacroMetrics.Abstractions.Enums.MetricId.Cape);

        Assert.NotNull(cape);
        Assert.Equal(MacroMetrics.Abstractions.Enums.MetricSource.Shiller, cape!.Source);
    }

    /// <summary>
    /// Regression guard: verifies FredFetcherService no longer contains "cape" in its
    /// supported metric IDs (i.e., it raises ArgumentException for "cape").
    /// </summary>
    [Fact]
    public async Task FredFetcherService_Cape_ThrowsArgumentException()
    {
        var fredHandler = new FakeHttpMessageHandler(_ => OkJson("{}"));
        var fredClient  = new HttpClient(fredHandler) { BaseAddress = new Uri("https://api.stlouisfed.org") };
        var config      = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Fred:ApiKey"] = "test" })
            .Build();
        var fredSut = new MacroMetrics.Services.Fetchers.FredFetcherService(fredClient, config);

        await Assert.ThrowsAsync<ArgumentException>(
            () => fredSut.FetchRawAsync("cape"));
    }
}
