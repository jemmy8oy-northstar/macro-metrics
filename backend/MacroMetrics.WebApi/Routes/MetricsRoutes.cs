using MacroMetrics.Abstractions.Extensions;
using MacroMetrics.Abstractions.Services.Metrics;

namespace MacroMetrics.WebApi.Routes;

public static class MetricsRoutes
{
    public static RouteGroupBuilder MapMetricsRoutes(this RouteGroupBuilder parentGroup)
    {
        var group = parentGroup.MapGroup("/metrics");

        group.MapGet("", (IMetricCatalogueService catalogueService) =>
        {
            var metrics = catalogueService.GetAll().Select(m => new
            {
                id = m.Id.ToDisplayString(),
                label = m.Label,
                unit = m.Unit.ToDisplayString(),
                source = m.Source.ToDisplayString(),
                isIndicatorOnly = m.IsIndicatorOnly,
                earliestDate = m.EarliestDate.ToString("yyyy-MM-dd")
            });

            return Results.Ok(metrics);
        })
        .WithName("GetMetrics")
        .WithSummary("Full metric catalogue with metadata.");

        group.MapGet("ratio", (string numerator, string denominator,
            string? from, string? to,
            IMetricRatioService ratioService) =>
        {
            var ratio = ratioService.GetRatio(numerator, denominator, from, to);

            if (ratio is null) return Results.NotFound();

            var response = new
            {
                numerator      = ratio.NumeratorId,
                denominator    = ratio.DenominatorId,
                series         = ratio.Points.Select(p => new { date = p.Date, value = p.Value }),
                longRunAverage = ratio.LongRunAverage
            };

            return Results.Ok(response);
        })
        .WithName("GetMetricRatio")
        .WithSummary("Ratio series for two metrics (numerator / denominator). Optional 'from' and 'to' (yyyy-MM-dd) parameters filter the returned data points while longRunAverage always reflects the full historical record.");

        group.MapGet("indicator/{id}", (string id, IMetricSeriesService seriesService) =>
        {
            var series = seriesService.GetSeries(id);

            if (series is null) return Results.NotFound();

            var points = series.Points.Select(p => new { date = p.Date, value = p.Value }).ToList();
            var response = new
            {
                id             = series.Id,
                label          = series.Label,
                unit           = series.Unit,
                longRunAverage = points.Count > 0 ? Math.Round(points.Average(p => p.value), 2) : 0.0,
                series         = points
            };

            return Results.Ok(response);
        })
        .WithName("GetMetricIndicator")
        .WithSummary("Indicator series with long-run average for a single metric.");

        group.MapGet("{id}", (string id, IMetricSeriesService seriesService) =>
        {
            var series = seriesService.GetSeries(id);

            if (series is null) return Results.NotFound();

            var response = new
            {
                id     = series.Id,
                label  = series.Label,
                unit   = series.Unit,
                points = series.Points.Select(p => new { date = p.Date, value = p.Value })
            };

            return Results.Ok(response);
        })
        .WithName("GetMetricSeries")
        .WithSummary("Full time series for a single metric.");

        return parentGroup;
    }
}
