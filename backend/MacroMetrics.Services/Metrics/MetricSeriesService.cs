using Bogus;
using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Extensions;
using MacroMetrics.Abstractions.Services.Metrics;
using MacroMetrics.DomainModels.Models;

namespace MacroMetrics.Services.Metrics;

public class MetricSeriesService(IMetricCatalogueService catalogueService) : IMetricSeriesService
{
    public IMetricSeries? GetSeries(string id)
    {
        var metadata = catalogueService.GetAll()
            .FirstOrDefault(m => m.Id.ToDisplayString() == id);

        if (metadata is null) return null;

        var rng    = new Randomizer(Math.Abs(id.GetHashCode()));
        var origin = metadata.EarliestDate.ToDateTime(TimeOnly.MinValue);
        var series = GenerateSeries(rng, origin, startValue: rng.Double(10, 100), volatility: 0.03);

        return new DomainMetricSeries
        {
            Id     = id,
            Label  = metadata.Label,
            Unit   = metadata.Unit.ToDisplayString(),
            Points = series
        };
    }

    private static List<DomainMetricPoint> GenerateSeries(
        Randomizer rng, DateTime origin, double startValue, double volatility)
    {
        var end    = DateTime.UtcNow;
        var months = ((end.Year - origin.Year) * 12) + end.Month - origin.Month;
        var points = new List<DomainMetricPoint>(months);
        var value  = startValue;

        for (int i = 0; i < months; i++)
        {
            value = Math.Max(0.01, value * (1 + rng.Double(-volatility, volatility)));
            points.Add(new DomainMetricPoint
            {
                Date  = origin.AddMonths(i).ToString("yyyy-MM-dd"),
                Value = Math.Round(value, 2)
            });
        }

        return points;
    }
}
