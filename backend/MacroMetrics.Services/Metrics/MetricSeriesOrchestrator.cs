using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Enums;
using MacroMetrics.Abstractions.Extensions;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Abstractions.Services.Metrics;
using MacroMetrics.DomainModels.Models;

namespace MacroMetrics.Services.Metrics;

/// <summary>
/// Routes a metric series request to the correct fetcher (ONS, FRED, YFinance, or Shiller)
/// based on the metric's declared <see cref="MetricSource"/>, then returns the series.
///
/// UK metrics (OnsHpi / OnsAwe / Ons) are exclusively served by <see cref="IOnsFetcherService"/>.
/// US macro metrics (Fred) are exclusively served by <see cref="IFredFetcherService"/>.
/// Market metrics (YFinance) are exclusively served by <see cref="IYFinanceFetcherService"/>.
/// Shiller CAPE ratio (Shiller) is exclusively served by <see cref="IShillerFetcherService"/>.
/// </summary>
public class MetricSeriesOrchestrator(
    IMetricCatalogueService catalogue,
    IOnsFetcherService onsFetcher,
    IFredFetcherService fredFetcher,
    IYFinanceFetcherService yFinanceFetcher,
    IShillerFetcherService shillerFetcher) : IMetricSeriesOrchestrator
{
    public async Task<IMetricSeries?> GetSeriesAsync(string id)
    {
        var metadata = catalogue.GetAll()
            .FirstOrDefault(m => m.Id.ToDisplayString() == id);

        if (metadata is null)
            return null;

        var raw = await FetchRawForSourceAsync(id, metadata.Source);

        return new DomainMetricSeries
        {
            Id     = id,
            Label  = metadata.Label,
            Unit   = metadata.Unit.ToDisplayString(),
            Points = raw
                .Select(p => new DomainMetricPoint { Date = p.Date, Value = p.Value })
                .ToList()
        };
    }

    private Task<IReadOnlyList<IMetricPoint>> FetchRawForSourceAsync(string id, MetricSource source)
        => source switch
        {
            MetricSource.OnsHpi or MetricSource.OnsAwe or MetricSource.Ons
                => onsFetcher.FetchRawAsync(id),
            MetricSource.Fred
                => fredFetcher.FetchRawAsync(id),
            MetricSource.YFinance
                => yFinanceFetcher.FetchRawAsync(id),
            MetricSource.Shiller
                => shillerFetcher.FetchRawAsync(id),
            _ => throw new InvalidOperationException($"Unsupported MetricSource: {source}")
        };
}
