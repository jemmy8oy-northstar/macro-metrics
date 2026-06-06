using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services.Fetchers;

/// <summary>
/// Fetches raw time-series data from the FRED (Federal Reserve Economic Data) API.
/// Used for US macroeconomic metrics: us-house-prices, us-wages, us-cpi, us-10yr-treasury.
/// </summary>
/// <remarks>
/// The Shiller CAPE ratio (<c>cape</c>) is <em>not</em> a FRED-hosted series and is
/// therefore <em>not</em> handled by this interface. Use <see cref="IShillerFetcherService"/>
/// for CAPE data instead.
/// </remarks>
public interface IFredFetcherService
{
    Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId);
}
