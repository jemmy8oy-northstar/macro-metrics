using System.Net;
using System.Text;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Services.Fetchers;
using Microsoft.Extensions.DependencyInjection;

namespace MacroMetrics.Services.Tests.Fetchers;

/// <summary>
/// In-process integration tests for <see cref="YFinanceFetcherService"/>.
/// <para>
/// These tests wire the real <see cref="YFinanceFetcherService"/> through
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
public sealed class YFinanceFetcherServiceIntegrationTests
{
    private const string SidecarBaseUrl = "http://yfinance-sidecar:8000";

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="ServiceProvider"/> with the real
    /// <see cref="YFinanceFetcherService"/> wired via <c>AddHttpClient</c>, using
    /// <paramref name="handler"/> as the primary HTTP handler so no real network
    /// calls are made.
    /// </summary>
    private static IYFinanceFetcherService BuildSut(FakeHttpMessageHandler handler)
    {
        var services = new ServiceCollection();

        services
            .AddHttpClient<IYFinanceFetcherService, YFinanceFetcherService>(client =>
            {
                client.BaseAddress = new Uri(SidecarBaseUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IYFinanceFetcherService>();
    }

    private static string BuildSidecarJson(string ticker, params (string Date, double Close)[] points)
    {
        var items = string.Join(",", points.Select(p =>
            $$$"""{"date":"{{{p.Date}}}","close":{{{p.Close.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}]""".Replace("]", "}")));
        return $$"""{"ticker":"{{ticker}}","points":[{{items}}]}""";
    }

    private static HttpResponseMessage OkJson(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    // ── DI resolution ─────────────────────────────────────────────────────

    [Fact]
    public void ServiceProvider_ResolvesYFinanceFetcherService()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson("{}"));
        var sut = BuildSut(handler);
        Assert.NotNull(sut);
        Assert.IsType<YFinanceFetcherService>(sut);
    }

    // ── Ticker routing through DI wiring ─────────────────────────────────

    [Theory]
    [InlineData("gold",         "GC%3DF")]         // GC=F URL-encoded
    [InlineData("oil",          "CL%3DF")]         // CL=F URL-encoded
    [InlineData("ftse100",      "%5EFTSE")]         // ^FTSE URL-encoded
    [InlineData("sp500",        "%5EGSPC")]         // ^GSPC URL-encoded
    [InlineData("bitcoin",      "BTC-USD")]
    [InlineData("uk-10yr-gilt", "%5ETMBMKGB-10Y")] // ^TMBMKGB-10Y URL-encoded
    public async Task FetchRawAsync_DiWired_CallsSidecarWithCorrectTicker(
        string metricId, string expectedEncodedTicker)
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildSidecarJson("GC=F")));
        var sut = BuildSut(handler);

        await sut.FetchRawAsync(metricId);

        Assert.NotNull(handler.LastRequest);
        var path = handler.LastRequest!.RequestUri!.AbsolutePath;
        Assert.EndsWith(expectedEncodedTicker, path, StringComparison.OrdinalIgnoreCase);
    }

    // ── Happy-path parsing through real DI wiring ─────────────────────────

    [Fact]
    public async Task FetchRawAsync_DiWired_ParsesPointsAndReturnsCorrectMetricPoints()
    {
        var json = BuildSidecarJson("^GSPC",
            ("2024-01-02", 4742.83),
            ("2024-01-03", 4704.81));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("sp500");

        Assert.Equal(2, result.Count);
        Assert.Equal("2024-01-02", result[0].Date);
        Assert.Equal(4742.83, result[0].Value, precision: 5);
        Assert.Equal("2024-01-03", result[1].Date);
        Assert.Equal(4704.81, result[1].Value, precision: 5);
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
            () => sut.FetchRawAsync("bitcoin"));

        Assert.Contains(((int)statusCode).ToString(), ex.Message);
    }
}
