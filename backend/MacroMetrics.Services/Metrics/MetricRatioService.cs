using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Services.Metrics;
using MacroMetrics.DomainModels.Models;

namespace MacroMetrics.Services.Metrics;

public class MetricRatioService(IMetricSeriesService seriesService) : IMetricRatioService
{
    public IMetricRatioSeries? GetRatio(string numeratorId, string denominatorId,
        string? from = null, string? to = null)
    {
        var numerator   = seriesService.GetSeries(numeratorId);
        var denominator = seriesService.GetSeries(denominatorId);

        if (numerator is null || denominator is null) return null;

        // Build lookup of denominator points by date for O(1) access
        var denominatorByDate = denominator.Points
            .ToDictionary(p => p.Date, p => p.Value);

        // Compute ratio for ALL dates present in both series (full historical intersection).
        // This full set is used to calculate the stable longRunAverage regardless of any
        // date-range filter supplied by the caller.
        var allRatioPoints = numerator.Points
            .Where(p => denominatorByDate.ContainsKey(p.Date) && denominatorByDate[p.Date] != 0)
            .Select(p => (IMetricPoint) new DomainMetricPoint
            {
                Date  = p.Date,
                Value = Math.Round(p.Value / denominatorByDate[p.Date], 4)
            })
            .OrderBy(p => p.Date)
            .ToList();

        // longRunAverage is always derived from the complete history (US-B9)
        var longRunAverage = allRatioPoints.Count > 0
            ? Math.Round(allRatioPoints.Average(p => p.Value), 4)
            : 0;

        // Apply optional date window filter to the returned points (US-B8)
        var filteredPoints = allRatioPoints
            .Where(p => (from is null || string.Compare(p.Date, from, StringComparison.Ordinal) >= 0)
                     && (to   is null || string.Compare(p.Date, to,   StringComparison.Ordinal) <= 0))
            .ToList();

        return new DomainMetricRatioSeries
        {
            NumeratorId    = numeratorId,
            DenominatorId  = denominatorId,
            Points         = filteredPoints,
            LongRunAverage = longRunAverage
        };
    }
}
