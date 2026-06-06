namespace MacroMetrics.Abstractions.DataModels;

public interface IMetricPoint
{
    string Date  { get; }
    double Value { get; }
}
