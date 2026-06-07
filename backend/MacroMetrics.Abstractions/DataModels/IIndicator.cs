namespace MacroMetrics.Abstractions.DataModels;

public interface IIndicator
{
    string                    Id             { get; }
    string                    Label          { get; }
    string                    Unit           { get; }
    double                    LongRunAverage { get; }
    IReadOnlyList<IDataPoint> Series         { get; }
}
