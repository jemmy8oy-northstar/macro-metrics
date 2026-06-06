using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Services.Normalisation;
using MacroMetrics.DomainModels.Models;

namespace MacroMetrics.Services.Normalisation;

/// <summary>
/// Normalises raw metric data points — regardless of their original cadence
/// (daily, monthly mid-month, etc.) — to a single end-of-month point per
/// calendar month, suitable for aligned ratio computation.
/// </summary>
public class DataNormalisationService : IDataNormalisationService
{
    /// <inheritdoc />
    public IReadOnlyList<IMetricPoint> Normalise(IEnumerable<IMetricPoint> rawPoints)
    {
        // Group by (year, month), take the last value within each group,
        // and output a single point dated to the last calendar day of that month.
        return rawPoints
            .Where(p => DateOnly.TryParseExact(p.Date, "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out _))
            .Select(p => (
                Point: p,
                Parsed: DateOnly.ParseExact(p.Date, "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture)))
            .OrderBy(x => x.Parsed)
            .GroupBy(x => new { x.Parsed.Year, x.Parsed.Month })
            .Select(g =>
            {
                // Last value within the month (closing / latest price)
                var last       = g.Last();
                var endOfMonth = new DateOnly(g.Key.Year, g.Key.Month,
                                     DateTime.DaysInMonth(g.Key.Year, g.Key.Month));

                return (IMetricPoint)new DomainMetricPoint
                {
                    Date  = endOfMonth.ToString("yyyy-MM-dd"),
                    Value = last.Point.Value
                };
            })
            .ToList()
            .AsReadOnly();
    }
}
