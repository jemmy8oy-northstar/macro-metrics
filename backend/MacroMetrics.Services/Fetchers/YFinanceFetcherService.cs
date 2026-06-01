using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.DomainModels.Models;

namespace MacroMetrics.Services.Fetchers;

/// <summary>
/// Fetches raw time-series data from the Python yfinance sidecar over HTTP.
/// Supports the six market/equity metrics: gold, oil, ftse100, sp500, bitcoin,
/// and uk-10yr-gilt.
/// </summary>
/// <remarks>
/// The sidecar exposes a single endpoint: <c>GET /series/{ticker}</c> which returns
/// a JSON body of the form <c>{ "ticker": "...", "points": [{ "date": "yyyy-MM-dd", "close": 0.0 }] }</c>.
/// The sidecar base URL is injected via the typed <see cref="HttpClient"/> registered in
/// <see cref="MacroMetrics.WebApi.ServiceRegistration"/>. It is configured from the
/// <c>YFinance:SidecarBaseUrl</c> configuration key (environment variable
/// <c>YFINANCE__SidecarBaseUrl</c>).
/// </remarks>
public sealed class YFinanceFetcherService : IYFinanceFetcherService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Maps each supported metric ID to its yfinance ticker symbol.
    /// </summary>
    private static readonly Dictionary<string, string> Tickers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["gold"]          = "GC=F",
            ["oil"]           = "CL=F",
            ["ftse100"]       = "^FTSE",
            ["sp500"]         = "^GSPC",
            ["bitcoin"]       = "BTC-USD",
            ["uk-10yr-gilt"]  = "^TMBMKGB-10Y",
        };

    public YFinanceFetcherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
    {
        if (!Tickers.TryGetValue(metricId, out var ticker))
            throw new ArgumentException($"Unknown yfinance metric ID: '{metricId}'.", nameof(metricId));

        var path = $"/series/{Uri.EscapeDataString(ticker)}";

        using var response = await _httpClient.GetAsync(path);

        if (!response.IsSuccessStatusCode)
            throw new FetcherException(
                $"yfinance sidecar returned HTTP {(int)response.StatusCode} for metric '{metricId}' (ticker '{ticker}').");

        var dto = await response.Content.ReadFromJsonAsync<SidecarResponse>()
                  ?? throw new FetcherException(
                      $"yfinance sidecar returned an empty body for metric '{metricId}'.");

        return dto.Points
            .Select(p => (IMetricPoint)new DomainMetricPoint
            {
                Date  = p.Date,   // sidecar already returns ISO-8601 dates: "yyyy-MM-dd"
                Value = p.Close,
            })
            .ToList();
    }

    // ── Private DTOs for sidecar JSON deserialization ──────────────────────

    private sealed class SidecarResponse
    {
        [JsonPropertyName("ticker")]
        public string Ticker { get; init; } = "";

        [JsonPropertyName("points")]
        public List<SidecarPoint> Points { get; init; } = [];
    }

    private sealed class SidecarPoint
    {
        [JsonPropertyName("date")]
        public string Date { get; init; } = "";

        [JsonPropertyName("close")]
        public double Close { get; init; }
    }
}
