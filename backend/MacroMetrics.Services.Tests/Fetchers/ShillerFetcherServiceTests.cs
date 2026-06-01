using System.Net;
using System.Text;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Services.Fetchers;

namespace MacroMetrics.Services.Tests.Fetchers;

/// <summary>
/// Unit tests for <see cref="ShillerFetcherService"/>.
/// HTTP calls are intercepted by <see cref="FakeHttpMessageHandler"/>.
/// </summary>
public sealed class ShillerFetcherServiceTests
{
    private const string ShillerBaseUrl = "https://www.multpl.com";

    // ── Helpers ───────────────────────────────────────────────────────────

    private static ShillerFetcherService BuildSut(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri(ShillerBaseUrl) };
        return new ShillerFetcherService(client);
    }

    /// <summary>
    /// Builds a minimal multpl.com-style JSON payload for the CAPE series.
    /// Each data point has a "date" string in "MMM d, yyyy" format and a numeric "value".
    /// </summary>
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

    // ── Request path ──────────────────────────────────────────────────────

    [Fact]
    public async Task FetchRawAsync_Cape_CallsCorrectMultplPath()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildShillerJson()));
        var sut = BuildSut(handler);

        await sut.FetchRawAsync("cape");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("/shiller-pe/table/by-month.json",
            handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    // ── Metric ID routing ─────────────────────────────────────────────────

    [Fact]
    public async Task FetchRawAsync_CapeMetricId_Succeeds()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildShillerJson()));
        var sut = BuildSut(handler);

        // Should not throw
        var result = await sut.FetchRawAsync("cape");

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchRawAsync_CapeMetricId_IsCaseInsensitive()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildShillerJson()));
        var sut = BuildSut(handler);

        // All three casings should succeed
        await sut.FetchRawAsync("cape");
        await sut.FetchRawAsync("CAPE");
        await sut.FetchRawAsync("Cape");
    }

    // ── Response parsing ──────────────────────────────────────────────────

    [Fact]
    public async Task FetchRawAsync_ParsesDataArrayIntoMetricPoints()
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
    public async Task FetchRawAsync_DateParsing_ConvertsMultplFormatToIso8601()
    {
        // Covers several months and a historical date
        var json = BuildShillerJson(
            ("Jan 1, 1881", 18.54),
            ("Dec 1, 2023",  29.0));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("cape");

        Assert.Equal("1881-01-01", result[0].Date);
        Assert.Equal("2023-12-01", result[1].Date);
    }

    [Fact]
    public async Task FetchRawAsync_EmptyDataArray_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildShillerJson()));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("cape");

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
            () => sut.FetchRawAsync("cape"));

        Assert.Contains("Shiller source", ex.Message);
        Assert.Contains(((int)statusCode).ToString(), ex.Message);
    }

    [Fact]
    public async Task FetchRawAsync_UnknownMetricId_ThrowsArgumentException()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson("{}"));
        var sut = BuildSut(handler);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.FetchRawAsync("unknown-metric"));

        Assert.Contains("unknown-metric", ex.Message);
    }

    /// <summary>
    /// Verifies that "cape" (the only Shiller metric) is handled correctly and
    /// that other metric IDs such as "us-cpi" (FRED) are rejected.
    /// This ensures no accidental routing from other sources.
    /// </summary>
    [Theory]
    [InlineData("us-cpi")]
    [InlineData("us-house-prices")]
    [InlineData("gold")]
    [InlineData("uk-cpi")]
    public async Task FetchRawAsync_NonShillerMetricIds_ThrowsArgumentException(string metricId)
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson("{}"));
        var sut = BuildSut(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.FetchRawAsync(metricId));
    }
}
