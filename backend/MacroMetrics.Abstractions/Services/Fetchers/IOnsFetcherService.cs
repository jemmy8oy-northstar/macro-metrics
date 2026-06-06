using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services.Fetchers;

/// <summary>
/// Fetches raw time-series data from the ONS (Office for National Statistics) API.
/// Used exclusively for UK macroeconomic metrics: uk-house-prices, uk-wages, uk-cpi.
/// </summary>
public interface IOnsFetcherService
{
    Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId);
}
