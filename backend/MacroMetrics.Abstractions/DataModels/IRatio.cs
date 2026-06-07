namespace MacroMetrics.Abstractions.DataModels;

public interface IRatio
{
    string                    Numerator      { get; }
    string                    Denominator    { get; }
    double                    LongRunAverage { get; }
    IReadOnlyList<IDataPoint> Series         { get; }
}
