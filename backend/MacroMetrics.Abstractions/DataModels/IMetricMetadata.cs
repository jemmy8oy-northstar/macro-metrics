using MacroMetrics.Abstractions.Enums;

namespace MacroMetrics.Abstractions.DataModels;

public interface IMetricMetadata
{
    MetricId Id { get; set; }
    string Label { get; set; }
    MetricUnit Unit { get; set; }
    MetricSource Source { get; set; }
    bool IsIndicatorOnly { get; set; }
    DateOnly EarliestDate { get; set; }
}
