using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.DataModels.Models;

public class MetricRatioSeries : IMetricRatioSeries
{
    public string                      NumeratorId    { get; set; } = "";
    public string                      DenominatorId  { get; set; } = "";
    public IReadOnlyList<IMetricPoint> Points         { get; set; } = [];
    public double                      LongRunAverage { get; set; }
}
