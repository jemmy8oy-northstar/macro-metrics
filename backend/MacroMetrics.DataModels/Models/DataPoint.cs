using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.DataModels.Models;

public class DataPoint : IDataPoint
{
    public string Date  { get; set; } = "";
    public double Value { get; set; }
}
