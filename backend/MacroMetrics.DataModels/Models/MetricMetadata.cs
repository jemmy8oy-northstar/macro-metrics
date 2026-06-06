using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Enums;

namespace MacroMetrics.DataModels.Models;

public class MetricMetadata : IMetricMetadata
{
    public MetricId Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public MetricUnit Unit { get; set; }
    public MetricSource Source { get; set; }
    public bool IsIndicatorOnly { get; set; }
    public DateOnly EarliestDate { get; set; }
}
