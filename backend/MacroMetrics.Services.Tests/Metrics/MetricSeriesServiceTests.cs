using MacroMetrics.Services.Metrics;

namespace MacroMetrics.Services.Tests.Metrics;

/// <summary>
/// BDD-aligned tests derived from Issue #46 (US-B4).
///
/// Scenario: Full series returned for a valid metric
///   Given a valid metric id "gold" exists in the catalogue
///   When GET /api/metrics/gold is called
///   Then the response status is 200
///   And the response body contains: id, label, unit, and a points array
///   And all points have a date in ISO format (YYYY-MM-DD) and a numeric value
///   And dates are in chronological ascending order
/// </summary>
public class MetricSeriesServiceTests
{
    private readonly MetricSeriesService _sut = new(new MetricCatalogueService());

    // --- Happy path: known metric "gold" ---

    [Fact]
    public void GetSeries_Gold_ReturnsNonNull()
    {
        var result = _sut.GetSeries("gold");

        Assert.NotNull(result);
    }

    [Fact]
    public void GetSeries_Gold_IdMatchesRequest()
    {
        var result = _sut.GetSeries("gold");

        Assert.Equal("gold", result!.Id);
    }

    [Fact]
    public void GetSeries_Gold_HasNonEmptyLabel()
    {
        var result = _sut.GetSeries("gold");

        Assert.False(string.IsNullOrWhiteSpace(result!.Label));
    }

    [Fact]
    public void GetSeries_Gold_HasNonEmptyUnit()
    {
        var result = _sut.GetSeries("gold");

        Assert.False(string.IsNullOrWhiteSpace(result!.Unit));
    }

    [Fact]
    public void GetSeries_Gold_PointsArrayIsNotEmpty()
    {
        var result = _sut.GetSeries("gold");

        Assert.NotEmpty(result!.Points);
    }

    // And all points have a date in ISO format (YYYY-MM-DD)

    [Fact]
    public void GetSeries_Gold_AllPointDatesAreIsoFormat()
    {
        var result = _sut.GetSeries("gold");

        Assert.All(result!.Points, p =>
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", p.Date));
    }

    // And all points have a numeric value

    [Fact]
    public void GetSeries_Gold_AllPointValuesArePositive()
    {
        var result = _sut.GetSeries("gold");

        Assert.All(result!.Points, p => Assert.True(p.Value > 0, $"Expected positive value but got {p.Value}"));
    }

    // And dates are in chronological ascending order

    [Fact]
    public void GetSeries_Gold_DatesAreInAscendingOrder()
    {
        var result  = _sut.GetSeries("gold");
        var dates   = result!.Points.Select(p => p.Date).ToList();
        var sorted  = dates.OrderBy(d => d).ToList();

        Assert.Equal(sorted, dates);
    }

    // --- Unknown metric returns null (maps to 404) ---

    [Fact]
    public void GetSeries_UnknownId_ReturnsNull()
    {
        var result = _sut.GetSeries("not-a-real-metric");

        Assert.Null(result);
    }

    // --- All catalogue metrics resolve to a non-null series ---

    [Theory]
    [InlineData("gold")]
    [InlineData("oil")]
    [InlineData("ftse100")]
    [InlineData("sp500")]
    [InlineData("bitcoin")]
    [InlineData("uk-10yr-gilt")]
    [InlineData("us-house-prices")]
    [InlineData("us-wages")]
    [InlineData("us-cpi")]
    [InlineData("cape")]
    [InlineData("us-10yr-treasury")]
    [InlineData("uk-house-prices")]
    [InlineData("uk-wages")]
    [InlineData("uk-cpi")]
    public void GetSeries_AllCatalogueMetrics_ReturnNonNull(string id)
    {
        var result = _sut.GetSeries(id);

        Assert.NotNull(result);
    }

    // --- Determinism: same id always produces same series ---

    [Fact]
    public void GetSeries_Gold_IsDeterministic()
    {
        var first  = _sut.GetSeries("gold")!.Points.ToList();
        var second = _sut.GetSeries("gold")!.Points.ToList();

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].Date,  second[i].Date);
            Assert.Equal(first[i].Value, second[i].Value);
        }
    }
}
