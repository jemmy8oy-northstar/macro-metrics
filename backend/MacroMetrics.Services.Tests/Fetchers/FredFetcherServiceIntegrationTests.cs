using System.Net;
using System.Text;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Services.Fetchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MacroMetrics.Services.Tests.Fetchers;

/// <summary>
/// In-process integration tests for <see cref="FredFetcherService"/>.
/// <para>
/// These tests wire the real <see cref="FredFetcherService"/> through
/// <see cref="IServiceCollection.AddHttpClient{TClient,TImplementation}"/> — the same DI
/// registration used in production — and stub only the HTTP boundary via a
/// <see cref="FakeHttpMessageHandler"/>. This verifies that:
/// <list type="bullet">
///   <item>the DI registration resolves correctly,</item>
///   <item>the API key is read from <c>IConfiguration["Fred:ApiKey"]</c>,</item>
///   <item>the full HTTP → deserialise → map pipeline produces the expected domain objects, and</item>
///   <item>error paths propagate as typed exceptions through the real implementation.</item>
/// </list>
/// </para>
/// </summary>
public sealed class FredFetcherServiceIntegrationTests
{
    private const string FredBaseUrl = "https://api.stlouisfed.org";
    private const string TestApiKey  = "test-fred-key-from-env";

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="ServiceProvider"/> with the real
    /// <see cref="FredFetcherService"/> wired via <c>AddHttpClient</c>, using
    /// <paramref name="handler"/> as the primary HTTP handler so no real network
    /// calls are made.
    /// </summary>
    private static IFredFetcherService BuildSut(FakeHttpMessageHandler handler)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Fred:ApiKey"] = TestApiKey })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);

        services
            .AddHttpClient<IFredFetcherService, FredFetcherService>(client =>
            {
                client.BaseAddress = new Uri(FredBaseUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IFredFetcherService>();
    }

    private static string BuildFredJson(params (string Date, string Value)[] observations)
    {
        var items = string.Join(",", observations.Select(o =>
            $$$"""{"date":"{{{o.Date}}}","value":"{{{o.Value}}}"}"""));
        return $$"""{"observations":[{{items}}]}""";
    }

    private static HttpResponseMessage OkJson(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    // ── DI resolution ─────────────────────────────────────────────────────

    [Fact]
    public void ServiceProvider_ResolvesFredFetcherService()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson("{}"));
        var sut = BuildSut(handler);
        Assert.NotNull(sut);
        Assert.IsType<FredFetcherService>(sut);
    }

    // ── API key is read from IConfiguration ──────────────────────────────

    /// <summary>
    /// BDD: FRED API key is read from environment, not appsettings.
    /// Verifies that FredFetcherService reads the key via IConfiguration["Fred:ApiKey"]
    /// (bound from the environment variable FRED__ApiKey in production).
    /// </summary>
    [Fact]
    public async Task FetchRawAsync_DiWired_AppendsApiKeyFromConfiguration()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildFredJson()));
        var sut = BuildSut(handler);

        await sut.FetchRawAsync("us-cpi");

        Assert.NotNull(handler.LastRequest);
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains($"api_key={TestApiKey}", query);
    }

    // ── Series ID routing through DI wiring ──────────────────────────────

    [Theory]
    [InlineData("us-house-prices",  "CSUSHPINSA")]
    [InlineData("us-wages",         "CES0500000003")]
    [InlineData("us-cpi",           "CPIAUCSL")]
    [InlineData("cape",             "CAPE")]
    [InlineData("us-10yr-treasury", "DGS10")]
    public async Task FetchRawAsync_DiWired_UsesCorrectFredSeriesId(
        string metricId, string expectedSeriesId)
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildFredJson()));
        var sut = BuildSut(handler);

        await sut.FetchRawAsync(metricId);

        Assert.NotNull(handler.LastRequest);
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains($"series_id={expectedSeriesId}", query);
    }

    // ── Happy-path parsing through real DI wiring ─────────────────────────

    [Fact]
    public async Task FetchRawAsync_DiWired_ParsesObservationsAndReturnsCorrectPoints()
    {
        var json = BuildFredJson(
            ("2024-01-01", "310.5"),
            ("2024-02-01", "312.0"));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("us-cpi");

        Assert.Equal(2, result.Count);
        Assert.Equal("2024-01-01", result[0].Date);
        Assert.Equal(310.5, result[0].Value, precision: 5);
        Assert.Equal("2024-02-01", result[1].Date);
        Assert.Equal(312.0, result[1].Value, precision: 5);
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
            () => sut.FetchRawAsync("us-wages"));

        Assert.Contains(((int)statusCode).ToString(), ex.Message);
    }
}
