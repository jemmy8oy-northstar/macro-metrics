using Bogus;
using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Services;
using MacroMetrics.DomainModels.Models;

namespace MacroMetrics.Services;

public class MetricsService : IMetricsService
{
    private static readonly Dictionary<string, (string Label, string Unit)> IndicatorMeta = new()
    {
        ["cape"]             = ("CAPE Ratio",              "×"),
        ["uk-10yr-gilt"]     = ("UK 10yr Gilt Yield",      "%"),
        ["us-10yr-treasury"] = ("US 10yr Treasury Yield",  "%"),
    };

    public IRatio GetRatio(string numerator, string denominator)
    {
        var rng    = new Randomizer(Math.Abs((numerator + denominator).GetHashCode()));
        var series = GenerateSeries(rng, startValue: rng.Double(3, 12), volatility: 0.03);
        return new DomainRatio
        {
            Numerator      = numerator,
            Denominator    = denominator,
            LongRunAverage = Math.Round(series.Average(p => p.Value), 2),
            Series         = series
        };
    }

    public IIndicator? GetIndicator(string id)
    {
        if (!IndicatorMeta.TryGetValue(id, out var meta)) return null;
        var rng      = new Randomizer(Math.Abs(id.GetHashCode()));
        double start = id == "cape" ? rng.Double(14, 22) : rng.Double(2, 5);
        var series   = GenerateSeries(rng, start, volatility: id == "cape" ? 0.04 : 0.05);
        return new DomainIndicator
        {
            Id             = id,
            Label          = meta.Label,
            Unit           = meta.Unit,
            LongRunAverage = Math.Round(series.Average(p => p.Value), 2),
            Series         = series
        };
    }

    private static List<DomainDataPoint> GenerateSeries(Randomizer rng, double startValue, double volatility)
    {
        var origin = new DateTime(1995, 1, 1);
        var points = new List<DomainDataPoint>(360);
        var value  = startValue;
        for (int i = 0; i < 360; i++)
        {
            value = Math.Max(0.1, value * (1 + rng.Double(-volatility, volatility)));
            points.Add(new DomainDataPoint
            {
                Date  = origin.AddMonths(i).ToString("yyyy-MM-dd"),
                Value = Math.Round(value, 2)
            });
        }
        return points;
    }
}
