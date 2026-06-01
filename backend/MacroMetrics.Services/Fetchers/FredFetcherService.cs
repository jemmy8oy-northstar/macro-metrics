using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Exceptions;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.DomainModels.Models;
using Microsoft.Extensions.Configuration;

namespace MacroMetrics.Services.Fetchers;

/// <summary>
/// Fetches raw time-series data from the FRED (Federal Reserve Economic Data) REST API.
/// Supports the five US macroeconomic metrics: us-house-prices, us-wages, us-cpi, cape,
/// and us-10yr-treasury.
/// </summary>
/// <remarks>
/// The FRED API key is read from <c>IConfiguration["Fred:ApiKey"]</c>. In production this
/// is supplied via the environment variable <c>FRED__ApiKey</c> (double-underscore is the
/// .NET hierarchical separator). A missing key causes a startup validation failure in
/// <see cref="MacroMetrics.WebApi.ServiceRegistration"/>, so the key is guaranteed
/// to be present when this service is constructed.
/// </remarks>
public sealed class FredFetcherService : IFredFetcherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    /// <summary>
    /// Maps each supported metric ID to its FRED series identifier.
    /// </summary>
    /// <remarks>
    /// <b>Known issue — "cape" series:</b> The Shiller CAPE ratio is <em>not</em> published
    /// by the St. Louis Fed; the FRED REST API returns HTTP 400 for series ID <c>"CAPE"</c>.
    /// The authoritative source is Robert Shiller's Yale dataset. Until a dedicated
    /// non-FRED fetcher is implemented for this metric, any live call for <c>"cape"</c>
    /// via this service will result in a <see cref="MacroMetrics.Abstractions.Exceptions.FetcherException"/>.
    /// See: https://github.com/jemmy8oy/macro-metrics/issues — track as a follow-up story.
    /// </remarks>
    private static readonly Dictionary<string, string> SeriesIds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["us-house-prices"]  = "CSUSHPINSA",
            ["us-wages"]         = "CES0500000003",
            ["us-cpi"]           = "CPIAUCSL",
            ["cape"]             = "CAPE",       // TODO: FRED returns HTTP 400 — series does not exist on FRED
            ["us-10yr-treasury"] = "DGS10",
        };

    public FredFetcherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Fred:ApiKey"]
            ?? throw new InvalidOperationException(
                "FRED API key is not configured. Set 'Fred:ApiKey' (environment variable FRED__ApiKey).");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
    {
        if (!SeriesIds.TryGetValue(metricId, out var seriesId))
            throw new ArgumentException($"Unknown FRED metric ID: '{metricId}'.", nameof(metricId));

        var path = $"/fred/series/observations?series_id={seriesId}&api_key={_apiKey}&file_type=json";

        using var response = await _httpClient.GetAsync(path);

        if (!response.IsSuccessStatusCode)
            throw new FetcherException(
                $"FRED source returned HTTP {(int)response.StatusCode} for metric '{metricId}'.");

        var dto = await response.Content.ReadFromJsonAsync<FredResponse>()
                  ?? throw new FetcherException(
                      $"FRED source returned an empty body for metric '{metricId}'.");

        // FRED uses "." as a placeholder for unreleased or missing observations — filter them out.
        return dto.Observations
            .Where(o => o.Value != ".")
            .Select(o => (IMetricPoint)new DomainMetricPoint
            {
                Date  = o.Date,   // FRED already returns ISO-8601 dates: "yyyy-MM-dd"
                Value = double.Parse(o.Value, CultureInfo.InvariantCulture),
            })
            .ToList();
    }

    // ── Private DTOs for FRED JSON deserialization ────────────────────────

    private sealed class FredResponse
    {
        [JsonPropertyName("observations")]
        public List<FredObservation> Observations { get; init; } = [];
    }

    private sealed class FredObservation
    {
        [JsonPropertyName("date")]
        public string Date { get; init; } = "";

        [JsonPropertyName("value")]
        public string Value { get; init; } = "";
    }
}
