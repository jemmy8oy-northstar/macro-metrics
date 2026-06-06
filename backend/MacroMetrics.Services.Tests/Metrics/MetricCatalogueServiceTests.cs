using MacroMetrics.Abstractions.Enums;
using MacroMetrics.Abstractions.Extensions;
using MacroMetrics.Services.Metrics;

namespace MacroMetrics.Services.Tests.Metrics;

public class MetricCatalogueServiceTests
{
    private readonly MetricCatalogueService _sut = new();

    [Fact]
    public void GetAll_ReturnsExactly14Metrics()
    {
        var result = _sut.GetAll();

        Assert.Equal(14, result.Count);
    }

    [Fact]
    public void GetAll_AllEntriesHaveDefinedId()
    {
        var result = _sut.GetAll();

        Assert.All(result, m => Assert.True(Enum.IsDefined(typeof(MetricId), m.Id)));
    }

    [Fact]
    public void GetAll_AllIdsSerialiseToKebabCase()
    {
        var result = _sut.GetAll();

        // kebab-case: lowercase letters, digits, and hyphens only
        Assert.All(result, m =>
            Assert.Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$", m.Id.ToDisplayString()));
    }

    [Fact]
    public void GetAll_AllEntriesHaveNonEmptyLabel()
    {
        var result = _sut.GetAll();

        Assert.All(result, m => Assert.False(string.IsNullOrWhiteSpace(m.Label)));
    }

    [Fact]
    public void GetAll_AllEntriesHaveDefinedUnit()
    {
        var result = _sut.GetAll();

        Assert.All(result, m => Assert.True(Enum.IsDefined(typeof(MetricUnit), m.Unit)));
    }

    [Fact]
    public void GetAll_AllEntriesHaveDefinedSource()
    {
        var result = _sut.GetAll();

        Assert.All(result, m => Assert.True(Enum.IsDefined(typeof(MetricSource), m.Source)));
    }

    [Theory]
    [InlineData(MetricId.Cape)]
    [InlineData(MetricId.Uk10YrGilt)]
    [InlineData(MetricId.Us10YrTreasury)]
    public void GetAll_IndicatorOnlyMetricsAreCorrectlyFlagged(MetricId id)
    {
        var result = _sut.GetAll();

        var metric = Assert.Single(result, m => m.Id == id);
        Assert.True(metric.IsIndicatorOnly, $"Metric '{id}' should have IsIndicatorOnly = true");
    }

    [Fact]
    public void GetAll_ExactlyThreeMetricsAreIndicatorOnly()
    {
        var result = _sut.GetAll();

        var indicatorOnly = result.Where(m => m.IsIndicatorOnly).ToList();
        Assert.Equal(3, indicatorOnly.Count);
    }

    [Fact]
    public void GetAll_NonIndicatorMetricsHaveIsIndicatorOnlyFalse()
    {
        var result = _sut.GetAll();

        var nonIndicator = result.Where(m => !m.IsIndicatorOnly);
        Assert.All(nonIndicator, m =>
            Assert.False(m.IsIndicatorOnly, $"Metric '{m.Id}' should have IsIndicatorOnly = false"));
    }

    [Fact]
    public void GetAll_AllEarliestDatesAreValid()
    {
        var result = _sut.GetAll();

        // Earliest dates should be in the past and not the default DateOnly value
        Assert.All(result, m =>
            Assert.NotEqual(default, m.EarliestDate));
    }

    // US-B3: Each metric exposes its earliest reliable date
    // Scenario: Catalogue entries include earliestDate
    //   Then each entry includes an earliestDate value in ISO date format (YYYY-MM-DD)

    [Fact]
    public void GetAll_AllEarliestDatesSerialiseToIsoFormat()
    {
        var result = _sut.GetAll();

        Assert.All(result, m =>
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", m.EarliestDate.ToString("yyyy-MM-dd")));
    }

    //   And the earliestDate for "bitcoin" is no earlier than 2014-09-30

    [Fact]
    public void GetAll_BitcoinEarliestDateIsNoEarlierThan_2014_09_30()
    {
        var result = _sut.GetAll();

        var bitcoin = Assert.Single(result, m => m.Id == MetricId.Bitcoin);
        Assert.True(
            bitcoin.EarliestDate >= new DateOnly(2014, 9, 30),
            $"Bitcoin earliestDate {bitcoin.EarliestDate:yyyy-MM-dd} should not be earlier than 2014-09-30");
    }

    //   And the earliestDate for "cape" is no later than 1881-01-31

    [Fact]
    public void GetAll_CapeEarliestDateIsNoLaterThan_1881_01_31()
    {
        var result = _sut.GetAll();

        var cape = Assert.Single(result, m => m.Id == MetricId.Cape);
        Assert.True(
            cape.EarliestDate <= new DateOnly(1881, 1, 31),
            $"CAPE earliestDate {cape.EarliestDate:yyyy-MM-dd} should not be later than 1881-01-31");
    }

    [Fact]
    public void GetAll_AllIdsAreUnique()
    {
        var result = _sut.GetAll();

        var distinctIds = result.Select(m => m.Id).Distinct().ToList();
        Assert.Equal(result.Count, distinctIds.Count);
    }

    [Fact]
    public void GetAll_ReturnsSameInstanceOnMultipleCalls()
    {
        var first = _sut.GetAll();
        var second = _sut.GetAll();

        // The static catalogue should return the same reference each time
        Assert.Same(first, second);
    }
}
