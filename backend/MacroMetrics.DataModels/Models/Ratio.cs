using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.DataModels.Models;

public class Ratio : IRatio
{
    public string                    Numerator      { get; set; } = "";
    public string                    Denominator    { get; set; } = "";
    public double                    LongRunAverage { get; set; }
    public IReadOnlyList<IDataPoint> Series         { get; set; } = [];
}
