using System.ComponentModel;

namespace MacroMetrics.Abstractions.Enums;

public enum MetricSource
{
    [Description("yfinance")]
    YFinance,

    [Description("FRED")]
    Fred,

    [Description("ONS HPI")]
    OnsHpi,

    [Description("ONS AWE")]
    OnsAwe,

    [Description("ONS")]
    Ons,

    [Description("Shiller")]
    Shiller
}
