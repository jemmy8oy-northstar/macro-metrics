using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.DomainModels.Models;

namespace MacroMetrics.Services.Fetchers;

/// <summary>
/// Fetches the Shiller Cyclically Adjusted Price-to-Earnings (CAPE) ratio from the
/// multpl.com JSON API, which mirrors Robert Shiller's authoritative Yale dataset.
/// Supports the single metric: <c>cape</c>.
/// </summary>
/// <remarks>
/// <para>
/// The FRED (St. Louis Fed) REST API does <em>not</em> host the Shiller CAPE series —
/// it returns HTTP 400 for series ID <c>"CAPE"</c>. This service provides the correct,
/// dedicated source for that metric.
/// </para>
/// <para>
/// The multpl.com endpoint (<c>GET /shiller-pe/table/by-month.json</c>) requires no API
/// key and is publicly accessible. Dates are returned in <c>"MMM d, yyyy"</c> format
/// (e.g. <c>"Jun 1, 2026"</c>) and are normalised to ISO-8601 <c>"yyyy-MM-dd"</c>.
/// </para>
/// </remarks>
public sealed class ShillerFetcherService : IShillerFetcherService
{
    private readonly HttpClient _httpClient;

    private const string CapePath = "/shiller-pe/table/by-month.json";

    private static readonly HashSet<string> SupportedMetrics =
        new(StringComparer.OrdinalIgnoreCase) { "cape" };

    public ShillerFetcherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
    {
        if (!SupportedMetrics.Contains(metricId))
            throw new ArgumentException(
                $"Unknown Shiller metric ID: '{metricId}'.", nameof(metricId));

        using var response = await _httpClient.GetAsync(CapePath);

        if (!response.IsSuccessStatusCode)
            throw new FetcherException(
                $"Shiller source returned HTTP {(int)response.StatusCode} for metric '{metricId}'.");

        var dto = await response.Content.ReadFromJsonAsync<ShillerResponse>()
                  ?? throw new FetcherException(
                      $"Shiller source returned an empty body for metric '{metricId}'.");

        return dto.Data
            .Select(d => (IMetricPoint)new DomainMetricPoint
            {
                Date  = ParseShillerDate(d.Date),
                Value = d.Value,
            })
            .ToList();
    }

    /// <summary>
    /// Parses the multpl.com date format <c>"MMM d, yyyy"</c>
    /// (e.g. <c>"Jun 1, 2026"</c>) and returns an ISO-8601 <c>"yyyy-MM-dd"</c> string.
    /// </summary>
    private static string ParseShillerDate(string raw)
    {
        // multpl.com returns dates like "Jun 1, 2026" — parse with invariant culture
        // to avoid locale-sensitive month-name parsing.
        if (DateOnly.TryParseExact(
                raw.Trim(),
                ["MMM d, yyyy", "MMM  d, yyyy"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date.ToString("yyyy-MM-dd");
        }

        throw new FetcherException(
            $"Shiller source returned an unrecognised date format: '{raw}'.");
    }

    // ── Private DTOs for multpl.com JSON deserialization ─────────────────

    private sealed class ShillerResponse
    {
        [JsonPropertyName("data")]
        public List<ShillerDataPoint> Data { get; init; } = [];
    }

    private sealed class ShillerDataPoint
    {
        [JsonPropertyName("date")]
        public string Date { get; init; } = "";

        [JsonPropertyName("value")]
        public double Value { get; init; }
    }
}
