using System.ComponentModel;

namespace MacroMetrics.Abstractions.Enums;

public enum MetricId
{
    [Description("gold")]
    Gold,

    [Description("oil")]
    Oil,

    [Description("ftse100")]
    Ftse100,

    [Description("sp500")]
    Sp500,

    [Description("bitcoin")]
    Bitcoin,

    [Description("uk-10yr-gilt")]
    Uk10YrGilt,

    [Description("us-house-prices")]
    UsHousePrices,

    [Description("us-wages")]
    UsWages,

    [Description("us-cpi")]
    UsCpi,

    [Description("cape")]
    Cape,

    [Description("us-10yr-treasury")]
    Us10YrTreasury,

    [Description("uk-house-prices")]
    UkHousePrices,

    [Description("uk-wages")]
    UkWages,

    [Description("uk-cpi")]
    UkCpi
}
