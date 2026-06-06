namespace MacroMetrics.Abstractions.DataModels;

public interface IMetricSeries
{
    string Id    { get; set; }
    string Label { get; set; }
    string Unit  { get; set; }
    IReadOnlyList<IMetricPoint> Points { get; set; }
}
