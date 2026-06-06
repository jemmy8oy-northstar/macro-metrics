using System.Net;
using System.Text;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Services.Fetchers;

namespace MacroMetrics.Services.Tests.Fetchers;

/// <summary>
/// Unit tests for <see cref="OnsFetcherService"/>.
/// HTTP calls are intercepted by <see cref="FakeHttpMessageHandler"/>.
/// </summary>
public sealed class OnsFetcherServiceTests
{
    private const string OnsBaseUrl = "https://api.ons.gov.uk";

    // ── Helpers ───────────────────────────────────────────────────────────

    private static OnsFetcherService BuildSut(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri(OnsBaseUrl) };
        return new OnsFetcherService(client);
    }

    /// <summary>Builds a minimal ONS JSON payload with a months array.</summary>
    private static string BuildOnsJson(params (string Date, string Value)[] months)
    {
        var items = string.Join(",", months.Select(m =>
            $$$"""{"date":"{{{m.Date}}}","value":"{{{m.Value}}}"}"""));
        return $$"""{"months":[{{items}}]}""";
    }

    // ── URL mapping ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("uk-house-prices", "/v1/datasets/housepriceindex/timeseries/AVHP/data")]
    [InlineData("uk-wages",        "/v1/datasets/averageweeklyearnings/timeseries/KAC3/data")]
    [InlineData("uk-cpi",          "/v1/datasets/cpih01/timeseries/L55O/data")]
    public async Task FetchRawAsync_KnownMetric_CallsCorrectOnsPath(
        string metricId, string expectedPath)
    {
        var handler = new FakeHttpMessageHandler(
            _ => OkJson(BuildOnsJson()));
        var sut = BuildSut(handler);

        await sut.FetchRawAsync(metricId);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(expectedPath, handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    // ── Response parsing ──────────────────────────────────────────────────

    [Fact]
    public async Task FetchRawAsync_ParsesMonthsArrayIntoMetricPoints()
    {
        var json = BuildOnsJson(
            ("2024 JAN", "123.4"),
            ("2024 FEB", "456.78"));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("uk-cpi");

        Assert.Equal(2, result.Count);

        // First point
        Assert.Equal("2024-01-01", result[0].Date);
        Assert.Equal(123.4, result[0].Value, precision: 5);

        // Second point
        Assert.Equal("2024-02-01", result[1].Date);
        Assert.Equal(456.78, result[1].Value, precision: 5);
    }

    [Fact]
    public async Task FetchRawAsync_DateParsing_MapsOnsDateToFirstDayOfMonth()
    {
        // Covers a range of months including multi-char abbreviations
        var json = BuildOnsJson(
            ("2023 MAR", "100.0"),
            ("2023 DEC", "200.0"));
        var handler = new FakeHttpMessageHandler(_ => OkJson(json));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("uk-wages");

        Assert.Equal("2023-03-01", result[0].Date);
        Assert.Equal("2023-12-01", result[1].Date);
    }

    [Fact]
    public async Task FetchRawAsync_EmptyMonthsArray_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(_ => OkJson(BuildOnsJson()));
        var sut = BuildSut(handler);

        var result = await sut.FetchRawAsync("uk-house-prices");

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
            () => sut.FetchRawAsync("uk-cpi"));

        Assert.Contains("ONS source", ex.Message);
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

    // ── Shared factory ────────────────────────────────────────────────────

    private static HttpResponseMessage OkJson(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
}

/// <summary>
/// A minimal <see cref="HttpMessageHandler"/> that returns a preset response and
/// captures the last request for assertion in tests.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public HttpRequestMessage? LastRequest { get; private set; }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        => _respond = respond;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_respond(request));
    }
}
