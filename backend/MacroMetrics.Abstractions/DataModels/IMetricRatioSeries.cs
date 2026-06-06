namespace MacroMetrics.Abstractions.DataModels;

public interface IMetricRatioSeries
{
    string                      NumeratorId     { get; set; }
    string                      DenominatorId   { get; set; }
    IReadOnlyList<IMetricPoint> Points          { get; set; }
    double                      LongRunAverage  { get; set; }
}
