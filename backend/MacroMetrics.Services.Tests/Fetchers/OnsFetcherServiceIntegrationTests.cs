using System.Net;
using System.Text;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Services.Fetchers;
using Microsoft.Extensions.DependencyInjection;

namespace MacroMetrics.Services.Tests.Fetchers;

/// <summary>
/// In-process integration tests for <see cref="OnsFetcherService"/>.
/// <para>
/// These tests wire the real <see cref="OnsFetcherService"/> through
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
public sealed class OnsFetcherServiceIntegrationTests
{
    private const string OnsBaseUrl = "https://api.ons.gov.uk";

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="ServiceProvider"/> with the real
    /// <see cref="OnsFetcherService"/> wired via <c>AddHttpClient</c>, using
    /// <paramref name="handler"/> as the primary HTTP handler so no real network
    /// calls are made.
    /// </summary>
    private static IOnsFetcherService BuildSut(FakeHttpMessageHandler handler)
    {
        var services = new ServiceCollection();

        services
            .AddHttpClient<IOnsFetcherService, OnsFetcherService>(client =>
            {
                client.BaseAddress = new Uri(OnsBaseUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOnsFetcherService>();
    }

    private static string BuildOnsJson(params (string Date, string Value)[] months)
    {
        var items = string.Join(",", months.Select(m =>
            $$$"""{"date":"{{{m.Date}}}","value":"{{{m.Value}}}"}"""));
        return $$"""{"months":[{{items}}]}""";
    }

    private static HttpResponseMessage OkJson(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    // ── DI resolution ─────────────────────────────────────────────────────

    [Fact]
    public void ServiceProvider_ResolvesOnsFetcherService()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson("{}"));
        var sut = BuildSut(handler);
        Assert.NotNull(sut);
        Assert.IsType<OnsFetcherService>(sut);
    }

    // ── Happy-path parsing through real DI wiring ─────────────────────────

    [Fact]
    public async Task FetchRawAsync_DiWired_ParsesMonthsAndReturnsCorrectPoints()
    {
        var json = BuildOnsJson(
            ("2024 JAN", "123.4"),
            ("2024 FEB", "456.78"));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("uk-cpi");

        Assert.Equal(2, result.Count);
        Assert.Equal("2024-01-01", result[0].Date);
        Assert.Equal(123.4,  result[0].Value, precision: 5);
        Assert.Equal("2024-02-01", result[1].Date);
        Assert.Equal(456.78, result[1].Value, precision: 5);
    }

    [Theory]
    [InlineData("uk-house-prices", "/v1/datasets/housepriceindex/timeseries/AVHP/data")]
    [InlineData("uk-wages",        "/v1/datasets/averageweeklyearnings/timeseries/KAC3/data")]
    [InlineData("uk-cpi",          "/v1/datasets/cpih01/timeseries/L55O/data")]
    public async Task FetchRawAsync_DiWired_CallsCorrectOnsPath(
        string metricId, string expectedPath)
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildOnsJson()));
        var sut = BuildSut(handler);

        await sut.FetchRawAsync(metricId);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(expectedPath, handler.LastRequest!.RequestUri!.AbsolutePath);
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
            () => sut.FetchRawAsync("uk-wages"));

        Assert.Contains(((int)statusCode).ToString(), ex.Message);
    }
}
