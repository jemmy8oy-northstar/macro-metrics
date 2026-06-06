using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.DomainModels.Models;

namespace MacroMetrics.Services.Fetchers;

/// <summary>
/// Fetches raw time-series data from the ONS (Office for National Statistics) REST API.
/// Supports the three UK macroeconomic metrics: uk-house-prices, uk-wages, uk-cpi.
/// </summary>
public sealed class OnsFetcherService : IOnsFetcherService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Maps each supported metric ID to its ONS dataset + series path.
    /// </summary>
    private static readonly Dictionary<string, string> MetricPaths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["uk-house-prices"] = "/v1/datasets/housepriceindex/timeseries/AVHP/data",
            ["uk-wages"]        = "/v1/datasets/averageweeklyearnings/timeseries/KAC3/data",
            ["uk-cpi"]          = "/v1/datasets/cpih01/timeseries/L55O/data",
        };

    public OnsFetcherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
    {
        if (!MetricPaths.TryGetValue(metricId, out var path))
            throw new ArgumentException($"Unknown ONS metric ID: '{metricId}'.", nameof(metricId));

        using var response = await _httpClient.GetAsync(path);

        if (!response.IsSuccessStatusCode)
            throw new FetcherException(
                $"ONS source returned HTTP {(int)response.StatusCode} for metric '{metricId}'.");

        var dto = await response.Content.ReadFromJsonAsync<OnsResponse>()
                  ?? throw new FetcherException(
                      $"ONS source returned an empty body for metric '{metricId}'.");

        return dto.Months
            .Select(m => (IMetricPoint)new DomainMetricPoint
            {
                Date  = ParseOnsDate(m.Date).ToString("yyyy-MM-dd"),
                Value = (double)decimal.Parse(m.Value, CultureInfo.InvariantCulture),
            })
            .ToList();
    }

    /// <summary>
    /// Parses the ONS date format "yyyy MMM" (e.g. "2024 JAN") to the first day of that month.
    /// </summary>
    private static DateOnly ParseOnsDate(string date)
        => DateOnly.ParseExact(date, "yyyy MMM", CultureInfo.InvariantCulture);

    // ── Private DTOs for ONS JSON deserialization ────────────────────────

    private sealed class OnsResponse
    {
        [JsonPropertyName("months")]
        public List<OnsMonth> Months { get; init; } = [];
    }

    private sealed class OnsMonth
    {
        [JsonPropertyName("date")]
        public string Date { get; init; } = "";

        [JsonPropertyName("value")]
        public string Value { get; init; } = "";
    }
}
