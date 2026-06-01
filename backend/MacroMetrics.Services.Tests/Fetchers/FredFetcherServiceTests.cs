using System.Net;
using System.Text;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Services.Fetchers;
using Microsoft.Extensions.Configuration;

namespace MacroMetrics.Services.Tests.Fetchers;

/// <summary>
/// Unit tests for <see cref="FredFetcherService"/>.
/// HTTP calls are intercepted by <see cref="FakeHttpMessageHandler"/>.
/// </summary>
public sealed class FredFetcherServiceTests
{
    private const string FredBaseUrl = "https://api.stlouisfed.org";
    private const string TestApiKey  = "test-fred-api-key";

    // ── Helpers ───────────────────────────────────────────────────────────

    private static FredFetcherService BuildSut(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri(FredBaseUrl) };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Fred:ApiKey"] = TestApiKey })
            .Build();
        return new FredFetcherService(client, config);
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

    // ── Series ID routing ─────────────────────────────────────────────────

    [Theory]
    [InlineData("us-house-prices",  "CSUSHPINSA")]
    [InlineData("us-wages",         "CES0500000003")]
    [InlineData("us-cpi",           "CPIAUCSL")]
    [InlineData("cape",             "CAPE")]
    [InlineData("us-10yr-treasury", "DGS10")]
    public async Task FetchRawAsync_KnownMetric_UsesCorrectFredSeriesId(
        string metricId, string expectedSeriesId)
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildFredJson()));
        var sut = BuildSut(handler);

        await sut.FetchRawAsync(metricId);

        Assert.NotNull(handler.LastRequest);
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains($"series_id={expectedSeriesId}", query);
    }

    // ── API key is appended to the request ────────────────────────────────

    [Fact]
    public async Task FetchRawAsync_AppendsApiKeyToRequest()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildFredJson()));
        var sut = BuildSut(handler);

        await sut.FetchRawAsync("us-cpi");

        Assert.NotNull(handler.LastRequest);
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains($"api_key={TestApiKey}", query);
    }

    // ── Response parsing ──────────────────────────────────────────────────

    [Fact]
    public async Task FetchRawAsync_ParsesObservationsIntoMetricPoints()
    {
        var json = BuildFredJson(
            ("2024-01-01", "123.4"),
            ("2024-02-01", "456.78"));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("us-cpi");

        Assert.Equal(2, result.Count);

        Assert.Equal("2024-01-01", result[0].Date);
        Assert.Equal(123.4,  result[0].Value, precision: 5);

        Assert.Equal("2024-02-01", result[1].Date);
        Assert.Equal(456.78, result[1].Value, precision: 5);
    }

    [Fact]
    public async Task FetchRawAsync_DatePassthrough_PreservesFredIsoFormat()
    {
        var json = BuildFredJson(
            ("1962-01-01", "4.08"),
            ("2023-12-01", "5.00"));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("us-10yr-treasury");

        Assert.Equal("1962-01-01", result[0].Date);
        Assert.Equal("2023-12-01", result[1].Date);
    }

    // ── Missing-value filtering ───────────────────────────────────────────

    [Fact]
    public async Task FetchRawAsync_FiltersDotValues_FredMissingPlaceholder()
    {
        // FRED uses "." to indicate an unreleased or unavailable observation.
        var json = BuildFredJson(
            ("2024-01-01", "100.0"),
            ("2024-02-01", "."),
            ("2024-03-01", "102.5"));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("us-cpi");

        Assert.Equal(2, result.Count);
        Assert.Equal("2024-01-01", result[0].Date);
        Assert.Equal("2024-03-01", result[1].Date);
    }

    [Fact]
    public async Task FetchRawAsync_EmptyObservations_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildFredJson()));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("us-wages");

        Assert.Empty(result);
    }

    // ── Error handling ────────────────────────────────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task FetchRawAsync_NonSuccessStatusCode_ThrowsFetcherException(
        HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(
            _ => new HttpResponseMessage(statusCode));
        var sut = BuildSut(handler);

        var ex = await Assert.ThrowsAsync<FetcherException>(
            () => sut.FetchRawAsync("us-cpi"));

        Assert.Contains("FRED source", ex.Message);
        Assert.Contains(((int)statusCode).ToString(), ex.Message);
    }

    [Fact]
    public async Task FetchRawAsync_UnknownMetricId_ThrowsArgumentException()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson("{}"));
        var sut = BuildSut(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.FetchRawAsync("unknown-metric"));
    }

    // ── Missing API key ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_MissingApiKey_ThrowsInvalidOperationException()
    {
        var client = new HttpClient { BaseAddress = new Uri(FredBaseUrl) };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())  // no Fred:ApiKey
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => new FredFetcherService(client, config));
    }
}
