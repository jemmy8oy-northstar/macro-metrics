using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Enums;
using MacroMetrics.Abstractions.Services.Metrics;
using MacroMetrics.DataModels.Models;

namespace MacroMetrics.Services.Metrics;

public class MetricCatalogueService : IMetricCatalogueService
{
    private static readonly IReadOnlyList<IMetricMetadata> Catalogue = new List<IMetricMetadata>
    {
        // yfinance metrics
        new MetricMetadata
        {
            Id = MetricId.Gold,
            Label = "Gold Price",
            Unit = MetricUnit.UsdPerOz,
            Source = MetricSource.YFinance,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1979, 1, 31)
        },
        new MetricMetadata
        {
            Id = MetricId.Oil,
            Label = "Oil Price (WTI)",
            Unit = MetricUnit.UsdPerBarrel,
            Source = MetricSource.YFinance,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1983, 3, 31)
        },
        new MetricMetadata
        {
            Id = MetricId.Ftse100,
            Label = "FTSE 100",
            Unit = MetricUnit.IndexGbp,
            Source = MetricSource.YFinance,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1984, 1, 31)
        },
        new MetricMetadata
        {
            Id = MetricId.Sp500,
            Label = "S&P 500",
            Unit = MetricUnit.IndexUsd,
            Source = MetricSource.YFinance,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1950, 1, 31)
        },
        new MetricMetadata
        {
            Id = MetricId.Bitcoin,
            Label = "Bitcoin",
            Unit = MetricUnit.Usd,
            Source = MetricSource.YFinance,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(2014, 9, 30)
        },
        new MetricMetadata
        {
            Id = MetricId.Uk10YrGilt,
            Label = "UK 10-Year Gilt Yield",
            Unit = MetricUnit.PercentYield,
            Source = MetricSource.YFinance,
            IsIndicatorOnly = true,
            EarliestDate = new DateOnly(1990, 1, 31)
        },

        // FRED metrics
        new MetricMetadata
        {
            Id = MetricId.UsHousePrices,
            Label = "US House Prices",
            Unit = MetricUnit.Index,
            Source = MetricSource.Fred,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1987, 1, 31)
        },
        new MetricMetadata
        {
            Id = MetricId.UsWages,
            Label = "US Wages",
            Unit = MetricUnit.UsdPerHour,
            Source = MetricSource.Fred,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1964, 3, 31)
        },
        new MetricMetadata
        {
            Id = MetricId.UsCpi,
            Label = "US CPI",
            Unit = MetricUnit.Index,
            Source = MetricSource.Fred,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1947, 1, 31)
        },
        // Shiller metrics
        new MetricMetadata
        {
            Id = MetricId.Cape,
            Label = "Shiller CAPE Ratio",
            Unit = MetricUnit.Ratio,
            Source = MetricSource.Shiller,
            IsIndicatorOnly = true,
            EarliestDate = new DateOnly(1881, 1, 31)
        },
        new MetricMetadata
        {
            Id = MetricId.Us10YrTreasury,
            Label = "US 10-Year Treasury Yield",
            Unit = MetricUnit.PercentYield,
            Source = MetricSource.Fred,
            IsIndicatorOnly = true,
            EarliestDate = new DateOnly(1962, 1, 31)
        },

        // ONS metrics
        new MetricMetadata
        {
            Id = MetricId.UkHousePrices,
            Label = "UK House Prices",
            Unit = MetricUnit.IndexPounds,
            Source = MetricSource.OnsHpi,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1968, 1, 31)
        },
        new MetricMetadata
        {
            Id = MetricId.UkWages,
            Label = "UK Wages",
            Unit = MetricUnit.PoundsPerMonth,
            Source = MetricSource.OnsAwe,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1963, 1, 31)
        },
        new MetricMetadata
        {
            Id = MetricId.UkCpi,
            Label = "UK CPI",
            Unit = MetricUnit.Index,
            Source = MetricSource.Ons,
            IsIndicatorOnly = false,
            EarliestDate = new DateOnly(1988, 1, 31)
        }
    };

    public IReadOnlyList<IMetricMetadata> GetAll() => Catalogue;
}
