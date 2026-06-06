using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.DataModels.Models;

public class MetricSeries : IMetricSeries
{
    public string                    Id     { get; set; } = "";
    public string                    Label  { get; set; } = "";
    public string                    Unit   { get; set; } = "";
    public IReadOnlyList<IMetricPoint> Points { get; set; } = [];
}
