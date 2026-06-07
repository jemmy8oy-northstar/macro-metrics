using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services;

public interface IMetricsService
{
    IRatio    GetRatio(string numerator, string denominator);
    IIndicator? GetIndicator(string id);
}
