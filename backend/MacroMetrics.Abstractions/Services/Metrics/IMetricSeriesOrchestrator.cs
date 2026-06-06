using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services.Metrics;

/// <summary>
/// Orchestrates fetching a metric series end-to-end:
/// routes to the correct fetcher (ONS, FRED, or YFinance) based on metric source,
/// normalises raw data to monthly cadence, and returns the resulting series.
/// </summary>
public interface IMetricSeriesOrchestrator
{
    Task<IMetricSeries?> GetSeriesAsync(string id);
}
