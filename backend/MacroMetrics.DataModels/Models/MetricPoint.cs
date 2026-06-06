using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.DataModels.Models;

public class MetricPoint : IMetricPoint
{
    public string Date  { get; init; } = "";
    public double Value { get; init; }
}
