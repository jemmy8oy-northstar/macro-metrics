using MacroMetrics.Services.Metrics;

namespace MacroMetrics.Services.Tests.Metrics;

/// <summary>
/// BDD-aligned tests derived from Issue #49 (US-B7).
///
/// Scenario: Ratio series computed correctly for two valid metrics
///   Given aligned monthly series exist for "us-house-prices" and "us-wages"
///   When GET /api/metrics/ratio?numerator=us-house-prices&amp;denominator=us-wages is called
///   Then the response status is 200
///   And the response body contains: numeratorId, denominatorId, points, and longRunAverage
///   And each DataPoint.value equals the numerator value divided by the denominator value for that date
///   And DataPoints only cover dates present in both series (the intersection of their date ranges)
///   And dates are in chronological ascending order
/// </summary>
public class MetricRatioServiceTests
{
    private readonly MetricSeriesService _seriesService = new(new MetricCatalogueService());

    private MetricRatioService CreateSut() => new(_seriesService);

    // --- Happy path: two valid metrics ---

    [Fact]
    public void GetRatio_TwoValidMetrics_ReturnsNonNull()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages");

        Assert.NotNull(result);
    }

    [Fact]
    public void GetRatio_TwoValidMetrics_NumeratorIdMatchesRequest()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages");

        Assert.Equal("us-house-prices", result!.NumeratorId);
    }

    [Fact]
    public void GetRatio_TwoValidMetrics_DenominatorIdMatchesRequest()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages");

        Assert.Equal("us-wages", result!.DenominatorId);
    }

    [Fact]
    public void GetRatio_TwoValidMetrics_PointsArrayIsNotEmpty()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages");

        Assert.NotEmpty(result!.Points);
    }

    // And the response body contains: longRunAverage

    [Fact]
    public void GetRatio_TwoValidMetrics_LongRunAverageIsPositive()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages");

        Assert.True(result!.LongRunAverage > 0,
            $"Expected positive longRunAverage but got {result.LongRunAverage}");
    }

    // And each DataPoint.value equals numerator / denominator for that date

    [Fact]
    public void GetRatio_TwoValidMetrics_EachPointValueEqualsNumeratorDividedByDenominator()
    {
        var sut         = CreateSut();
        var numerator   = _seriesService.GetSeries("us-house-prices")!;
        var denominator = _seriesService.GetSeries("us-wages")!;
        var result      = sut.GetRatio("us-house-prices", "us-wages")!;

        var numByDate   = numerator.Points.ToDictionary(p => p.Date, p => p.Value);
        var denByDate   = denominator.Points.ToDictionary(p => p.Date, p => p.Value);

        Assert.All(result.Points, p =>
        {
            Assert.True(numByDate.ContainsKey(p.Date),  $"Date {p.Date} not found in numerator");
            Assert.True(denByDate.ContainsKey(p.Date),  $"Date {p.Date} not found in denominator");
            var expected = Math.Round(numByDate[p.Date] / denByDate[p.Date], 4);
            Assert.Equal(expected, p.Value);
        });
    }

    // And DataPoints only cover dates present in both series (intersection)

    [Fact]
    public void GetRatio_TwoValidMetrics_PointDatesAreIntersectionOfBothSeries()
    {
        var sut         = CreateSut();
        var numerator   = _seriesService.GetSeries("us-house-prices")!;
        var denominator = _seriesService.GetSeries("us-wages")!;
        var result      = sut.GetRatio("us-house-prices", "us-wages")!;

        var numDates    = numerator.Points.Select(p => p.Date).ToHashSet();
        var denDates    = denominator.Points.Select(p => p.Date).ToHashSet();
        var intersection = numDates.Intersect(denDates).ToHashSet();

        Assert.All(result.Points, p => Assert.Contains(p.Date, intersection));
    }

    // And dates are in chronological ascending order

    [Fact]
    public void GetRatio_TwoValidMetrics_DatesAreInAscendingOrder()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages")!;
        var dates  = result.Points.Select(p => p.Date).ToList();
        var sorted = dates.OrderBy(d => d).ToList();

        Assert.Equal(sorted, dates);
    }

    // And all point dates are valid ISO format

    [Fact]
    public void GetRatio_TwoValidMetrics_AllPointDatesAreIsoFormat()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages")!;

        Assert.All(result.Points, p =>
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", p.Date));
    }

    // And all point values are positive

    [Fact]
    public void GetRatio_TwoValidMetrics_AllPointValuesArePositive()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages")!;

        Assert.All(result.Points, p =>
            Assert.True(p.Value > 0, $"Expected positive value but got {p.Value}"));
    }

    // And longRunAverage equals the mean of all ratio point values

    [Fact]
    public void GetRatio_TwoValidMetrics_LongRunAverageEqualsMeanOfPoints()
    {
        var sut      = CreateSut();
        var result   = sut.GetRatio("us-house-prices", "us-wages")!;
        var expected = Math.Round(result.Points.Average(p => p.Value), 4);

        Assert.Equal(expected, result.LongRunAverage);
    }

    // --- Unknown numerator returns null (maps to 404) ---

    [Fact]
    public void GetRatio_UnknownNumerator_ReturnsNull()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("not-a-real-metric", "us-wages");

        Assert.Null(result);
    }

    // --- Unknown denominator returns null (maps to 404) ---

    [Fact]
    public void GetRatio_UnknownDenominator_ReturnsNull()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "not-a-real-metric");

        Assert.Null(result);
    }

    // --- Both unknown returns null ---

    [Fact]
    public void GetRatio_BothUnknown_ReturnsNull()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("foo", "bar");

        Assert.Null(result);
    }

    // --- Determinism: same pair always produces same ratio series ---

    [Fact]
    public void GetRatio_SamePair_IsDeterministic()
    {
        var sut    = CreateSut();
        var first  = sut.GetRatio("us-house-prices", "us-wages")!.Points.ToList();
        var second = sut.GetRatio("us-house-prices", "us-wages")!.Points.ToList();

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].Date,  second[i].Date);
            Assert.Equal(first[i].Value, second[i].Value);
        }
    }

    // --- Various valid metric pairs all return non-null ---

    [Theory]
    [InlineData("us-house-prices", "us-wages")]
    [InlineData("gold", "oil")]
    [InlineData("sp500", "us-cpi")]
    [InlineData("uk-house-prices", "uk-wages")]
    public void GetRatio_ValidMetricPairs_ReturnNonNull(string numeratorId, string denominatorId)
    {
        var sut    = CreateSut();
        var result = sut.GetRatio(numeratorId, denominatorId);

        Assert.NotNull(result);
    }

    // =========================================================================
    // US-B8 / US-B9 — Date range filter with stable long-run average
    // =========================================================================

    /// <summary>
    /// Scenario: Ratio series is filtered to the requested date range
    ///   Given ratio history exists for "us-house-prices" and "us-wages"
    ///   When GetRatio is called with from=2000-01-01 and to=2010-12-31
    ///   Then all returned DataPoints have dates between 2000-01-01 and 2010-12-31
    ///   And longRunAverage is still computed from the full history
    /// </summary>

    [Fact]
    public void GetRatio_WithFromAndTo_ReturnsOnlyPointsInRange()
    {
        var sut    = CreateSut();
        const string from = "2000-01-01";
        const string to   = "2010-12-31";

        var result = sut.GetRatio("us-house-prices", "us-wages", from, to);

        Assert.NotNull(result);
        Assert.All(result!.Points, p =>
        {
            Assert.True(string.Compare(p.Date, from, StringComparison.Ordinal) >= 0,
                $"Point date {p.Date} is before 'from' bound {from}");
            Assert.True(string.Compare(p.Date, to, StringComparison.Ordinal) <= 0,
                $"Point date {p.Date} is after 'to' bound {to}");
        });
    }

    [Fact]
    public void GetRatio_WithFromAndTo_DoesNotReturnEmptyPoints()
    {
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages", "2000-01-01", "2010-12-31");

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Points);
    }

    [Fact]
    public void GetRatio_WithFromOnly_ReturnsOnlyPointsOnOrAfterFrom()
    {
        var sut    = CreateSut();
        const string from = "2010-01-01";

        var result = sut.GetRatio("us-house-prices", "us-wages", from: from);

        Assert.NotNull(result);
        Assert.All(result!.Points, p =>
            Assert.True(string.Compare(p.Date, from, StringComparison.Ordinal) >= 0,
                $"Point date {p.Date} is before 'from' bound {from}"));
    }

    [Fact]
    public void GetRatio_WithToOnly_ReturnsOnlyPointsOnOrBeforeTo()
    {
        var sut    = CreateSut();
        const string to = "2010-12-31";

        var result = sut.GetRatio("us-house-prices", "us-wages", to: to);

        Assert.NotNull(result);
        Assert.All(result!.Points, p =>
            Assert.True(string.Compare(p.Date, to, StringComparison.Ordinal) <= 0,
                $"Point date {p.Date} is after 'to' bound {to}"));
    }

    /// <summary>
    /// Scenario: Date filter does not affect longRunAverage
    ///   Given ratio history with 20+ years exists for "us-house-prices" and "us-wages"
    ///   When GetRatio is called with from=2020-01-01
    ///   Then longRunAverage equals the mean of ALL ratio points across the full historical record
    ///   And longRunAverage is NOT recalculated from only the filtered DataPoints
    /// </summary>

    [Fact]
    public void GetRatio_WithDateFilter_LongRunAverageMatchesUnfilteredMean()
    {
        var sut = CreateSut();

        // Compute the baseline long-run average with no filter
        var unfiltered     = sut.GetRatio("us-house-prices", "us-wages")!;
        var expectedLra    = unfiltered.LongRunAverage;

        // Now apply a date filter that restricts to a narrow recent window
        var filtered = sut.GetRatio("us-house-prices", "us-wages", from: "2020-01-01");

        Assert.NotNull(filtered);
        Assert.Equal(expectedLra, filtered!.LongRunAverage);
    }

    [Fact]
    public void GetRatio_WithDateFilter_FilteredPointCountIsLessThanUnfiltered()
    {
        var sut        = CreateSut();
        var unfiltered = sut.GetRatio("us-house-prices", "us-wages")!;
        var filtered   = sut.GetRatio("us-house-prices", "us-wages", from: "2020-01-01");

        Assert.NotNull(filtered);
        Assert.True(filtered!.Points.Count < unfiltered.Points.Count,
            "Filtered result should contain fewer points than the unfiltered series");
    }

    [Fact]
    public void GetRatio_WithDateFilter_LongRunAverageNotRecalculatedFromFilteredPoints()
    {
        var sut = CreateSut();

        var filtered = sut.GetRatio("us-house-prices", "us-wages", from: "2020-01-01");

        Assert.NotNull(filtered);

        // If the longRunAverage were wrongly recalculated from the filtered slice only,
        // it would equal the mean of the filtered points — it must NOT.
        var filteredMean = filtered!.Points.Count > 0
            ? Math.Round(filtered.Points.Average(p => p.Value), 4)
            : 0;

        var unfiltered = sut.GetRatio("us-house-prices", "us-wages")!;

        // The long-run average should match the unfiltered mean, not the filtered mean
        Assert.Equal(unfiltered.LongRunAverage, filtered.LongRunAverage);
        // Sanity: filtered mean differs (series is long enough for a meaningful difference)
        // We simply confirm the LRA equals full-history mean, not the narrow slice mean.
        Assert.NotEqual(filteredMean, filtered.LongRunAverage);
    }

    [Fact]
    public void GetRatio_NoDateFilter_LongRunAverageEqualsMeanOfAllPoints()
    {
        // Regression guard: when no filter is applied the behaviour is unchanged
        var sut    = CreateSut();
        var result = sut.GetRatio("us-house-prices", "us-wages")!;
        var expected = Math.Round(result.Points.Average(p => p.Value), 4);

        Assert.Equal(expected, result.LongRunAverage);
    }
}
