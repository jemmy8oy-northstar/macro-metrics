using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.DataModels.Models;

public class Indicator : IIndicator
{
    public string                    Id             { get; set; } = "";
    public string                    Label          { get; set; } = "";
    public string                    Unit           { get; set; } = "";
    public double                    LongRunAverage { get; set; }
    public IReadOnlyList<IDataPoint> Series         { get; set; } = [];
}
