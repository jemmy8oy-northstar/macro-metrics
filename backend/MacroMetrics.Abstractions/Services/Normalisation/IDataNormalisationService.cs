using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services.Normalisation;

/// <summary>
/// Maps any input cadence (daily, monthly mid-month, etc.) to a sequence of
/// monthly end-of-month data points.
/// </summary>
public interface IDataNormalisationService
{
    /// <summary>
    /// Normalises a raw list of data points to monthly end-of-month alignment.
    ///
    /// Rules:
    ///   - Daily series: exactly one point per calendar month is retained,
    ///     using the last available value within that month (closing price).
    ///   - Monthly series (mid-month dates): the date is shifted to the last
    ///     calendar day of the same month; the value is unchanged.
    ///   - Output is sorted chronologically.
    /// </summary>
    /// <param name="rawPoints">Input data points in any cadence.</param>
    /// <returns>
    ///   A new read-only list with exactly one <see cref="IMetricPoint"/> per
    ///   calendar month, each dated to the end of that month.
    /// </returns>
    IReadOnlyList<IMetricPoint> Normalise(IEnumerable<IMetricPoint> rawPoints);
}
