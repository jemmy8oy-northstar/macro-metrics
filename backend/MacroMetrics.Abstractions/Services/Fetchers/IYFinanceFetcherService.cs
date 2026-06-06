using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services.Fetchers;

/// <summary>
/// Fetches raw time-series data from Yahoo Finance.
/// Used for market/equity metrics: gold, oil, ftse100, sp500, bitcoin, uk-10yr-gilt.
/// </summary>
public interface IYFinanceFetcherService
{
    Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId);
}
