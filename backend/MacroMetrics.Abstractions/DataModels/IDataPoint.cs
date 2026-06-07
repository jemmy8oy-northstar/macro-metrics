namespace MacroMetrics.Abstractions.DataModels;

public interface IDataPoint
{
    string Date  { get; }
    double Value { get; }
}
