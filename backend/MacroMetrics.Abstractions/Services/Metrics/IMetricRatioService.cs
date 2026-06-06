using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services.Metrics;

public interface IMetricRatioService
{
    IMetricRatioSeries? GetRatio(string numeratorId, string denominatorId,
        string? from = null, string? to = null);
}
