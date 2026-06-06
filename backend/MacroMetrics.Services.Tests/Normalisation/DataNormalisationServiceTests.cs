using MacroMetrics.DataModels.Models;
using MacroMetrics.Services.Normalisation;

namespace MacroMetrics.Services.Tests.Normalisation;

/// <summary>
/// BDD-aligned tests derived from Issue #51 (US-B10).
///
/// Scenario A: Daily series is downsampled to monthly end-of-month
///   Given a raw daily series with multiple data points per calendar month
///   When DataNormalisationService normalises the series
///   Then the output contains exactly one DataPoint per calendar month
///   And each date is the last calendar day of that month
///   And the retained value is the last available value within the month (closing price)
///
/// Scenario B: Monthly series is aligned to end-of-month dates
///   Given a raw monthly series with mid-month dates (e.g. 2024-01-15)
///   When DataNormalisationService normalises the series
///   Then each date is shifted to the end of its calendar month (e.g. 2024-01-31)
///   And values are unchanged
/// </summary>
public class DataNormalisationServiceTests
{
    private readonly DataNormalisationService _sut = new();

    // =========================================================================
    // Scenario A — Daily series downsampled to monthly end-of-month
    // =========================================================================

    [Fact]
    public void Normalise_DailySeries_ReturnsOnePointPerCalendarMonth()
    {
        // Given: daily data spanning Jan 2024 (31 days) and Feb 2024 (29 days)
        var raw = BuildDailyPoints("2024-01-01", "2024-02-29");

        var result = _sut.Normalise(raw);

        // Then: exactly two output points (one per month)
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Normalise_DailySeries_EachOutputDateIsLastDayOfMonth()
    {
        var raw = BuildDailyPoints("2024-01-01", "2024-03-31");

        var result = _sut.Normalise(raw);

        Assert.Collection(result,
            p => Assert.Equal("2024-01-31", p.Date),
            p => Assert.Equal("2024-02-29", p.Date),   // 2024 is a leap year
            p => Assert.Equal("2024-03-31", p.Date));
    }

    [Fact]
    public void Normalise_DailySeries_RetainedValueIsLastWithinMonth()
    {
        // Given: two daily points in January — the last should win
        var raw = new[]
        {
            new MetricPoint { Date = "2024-01-10", Value = 100.0 },
            new MetricPoint { Date = "2024-01-25", Value = 200.0 }
        };

        var result = _sut.Normalise(raw);

        Assert.Single(result);
        Assert.Equal(200.0, result[0].Value);
    }

    [Fact]
    public void Normalise_DailySeries_ClosingValueWinsOverEarlierInSameMonth()
    {
        // Given: three points in February — the closing (last) value is 999
        var raw = new[]
        {
            new MetricPoint { Date = "2024-02-01", Value = 10.0 },
            new MetricPoint { Date = "2024-02-14", Value = 50.0 },
            new MetricPoint { Date = "2024-02-28", Value = 999.0 }
        };

        var result = _sut.Normalise(raw);

        Assert.Single(result);
        Assert.Equal(999.0, result[0].Value);
        Assert.Equal("2024-02-29", result[0].Date); // Feb 2024 ends on 29th (leap)
    }

    [Fact]
    public void Normalise_DailySeries_OutputIsInChronologicalOrder()
    {
        var raw = BuildDailyPoints("2023-11-01", "2024-02-28");

        var result = _sut.Normalise(raw);

        var dates  = result.Select(p => p.Date).ToList();
        var sorted = dates.OrderBy(d => d).ToList();

        Assert.Equal(sorted, dates);
    }

    [Fact]
    public void Normalise_DailySeries_AllOutputDatesAreIsoFormat()
    {
        var raw = BuildDailyPoints("2024-01-01", "2024-06-30");

        var result = _sut.Normalise(raw);

        Assert.All(result, p =>
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", p.Date));
    }

    // =========================================================================
    // Scenario B — Monthly mid-month series aligned to end-of-month
    // =========================================================================

    [Fact]
    public void Normalise_MidMonthDates_ShiftedToEndOfMonth()
    {
        // Given: mid-month monthly series
        var raw = new[]
        {
            new MetricPoint { Date = "2024-01-15", Value = 1.0 },
            new MetricPoint { Date = "2024-02-15", Value = 2.0 },
            new MetricPoint { Date = "2024-03-15", Value = 3.0 }
        };

        var result = _sut.Normalise(raw);

        Assert.Equal(3, result.Count);
        Assert.Equal("2024-01-31", result[0].Date);
        Assert.Equal("2024-02-29", result[1].Date);   // leap year
        Assert.Equal("2024-03-31", result[2].Date);
    }

    [Fact]
    public void Normalise_MidMonthDates_ValuesAreUnchanged()
    {
        var raw = new[]
        {
            new MetricPoint { Date = "2024-01-15", Value = 42.5  },
            new MetricPoint { Date = "2024-02-15", Value = 100.0 }
        };

        var result = _sut.Normalise(raw);

        Assert.Equal(42.5,  result[0].Value);
        Assert.Equal(100.0, result[1].Value);
    }

    [Fact]
    public void Normalise_SingleMidMonthPoint_DateShiftedToEndOfMonth()
    {
        var raw = new[] { new MetricPoint { Date = "2024-01-01", Value = 77.0 } };

        var result = _sut.Normalise(raw);

        Assert.Single(result);
        Assert.Equal("2024-01-31", result[0].Date);
    }

    // =========================================================================
    // Edge cases
    // =========================================================================

    [Fact]
    public void Normalise_EmptyInput_ReturnsEmptyList()
    {
        var result = _sut.Normalise(Array.Empty<MetricPoint>());

        Assert.Empty(result);
    }

    [Fact]
    public void Normalise_SinglePoint_ReturnsOneEndOfMonthPoint()
    {
        var raw = new[] { new MetricPoint { Date = "2024-06-15", Value = 55.5 } };

        var result = _sut.Normalise(raw);

        Assert.Single(result);
        Assert.Equal("2024-06-30", result[0].Date);
        Assert.Equal(55.5, result[0].Value);
    }

    [Fact]
    public void Normalise_AlreadyEndOfMonthDate_DateUnchanged()
    {
        var raw = new[] { new MetricPoint { Date = "2024-03-31", Value = 88.0 } };

        var result = _sut.Normalise(raw);

        Assert.Single(result);
        Assert.Equal("2024-03-31", result[0].Date);
    }

    [Fact]
    public void Normalise_LeapYearFebruary_EndDateIs29th()
    {
        var raw = new[] { new MetricPoint { Date = "2024-02-10", Value = 1.0 } };

        var result = _sut.Normalise(raw);

        Assert.Single(result);
        Assert.Equal("2024-02-29", result[0].Date);
    }

    [Fact]
    public void Normalise_NonLeapYearFebruary_EndDateIs28th()
    {
        var raw = new[] { new MetricPoint { Date = "2023-02-10", Value = 1.0 } };

        var result = _sut.Normalise(raw);

        Assert.Single(result);
        Assert.Equal("2023-02-28", result[0].Date);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Builds a daily series from <paramref name="from"/> to <paramref name="to"/> inclusive,
    /// assigning incrementing values (1, 2, 3, …).
    /// </summary>
    private static IEnumerable<MetricPoint> BuildDailyPoints(string from, string to)
    {
        var start   = DateOnly.ParseExact(from, "yyyy-MM-dd");
        var end     = DateOnly.ParseExact(to,   "yyyy-MM-dd");
        var current = start;
        var value   = 1.0;

        while (current <= end)
        {
            yield return new MetricPoint
            {
                Date  = current.ToString("yyyy-MM-dd"),
                Value = value++
            };
            current = current.AddDays(1);
        }
    }
}
