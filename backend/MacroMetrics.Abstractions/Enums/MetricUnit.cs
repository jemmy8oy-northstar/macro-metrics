using System.ComponentModel;

namespace MacroMetrics.Abstractions.Enums;

public enum MetricUnit
{
    [Description("$/oz")]
    UsdPerOz,

    [Description("$/barrel")]
    UsdPerBarrel,

    [Description("Index (GBP)")]
    IndexGbp,

    [Description("Index (USD)")]
    IndexUsd,

    [Description("USD")]
    Usd,

    [Description("% Yield")]
    PercentYield,

    [Description("Index")]
    Index,

    [Description("$/hour")]
    UsdPerHour,

    [Description("Ratio")]
    Ratio,

    [Description("Index (£)")]
    IndexPounds,

    [Description("£/month")]
    PoundsPerMonth
}
