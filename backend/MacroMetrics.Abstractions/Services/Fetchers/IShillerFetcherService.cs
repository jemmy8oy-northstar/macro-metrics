using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services.Fetchers;

/// <summary>
/// Fetches raw time-series data from Robert Shiller's Cyclically Adjusted
/// Price-to-Earnings (CAPE) dataset.
/// Used exclusively for the <c>cape</c> metric.
/// </summary>
/// <remarks>
/// The Shiller CAPE ratio is <em>not</em> published by the St. Louis Fed (FRED).
/// The authoritative source is Robert Shiller's Yale dataset; this service
/// retrieves the data from the multpl.com JSON endpoint which mirrors that dataset.
/// </remarks>
public interface IShillerFetcherService
{
    Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId);
}
