using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services.Metrics;

public interface IMetricSeriesService
{
    IMetricSeries? GetSeries(string id);
}
