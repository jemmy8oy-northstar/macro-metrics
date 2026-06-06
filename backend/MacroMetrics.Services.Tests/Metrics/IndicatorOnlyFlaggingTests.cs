using MacroMetrics.Abstractions.Enums;
using MacroMetrics.Abstractions.Extensions;
using MacroMetrics.Services.Metrics;

namespace MacroMetrics.Services.Tests.Metrics;

/// <summary>
/// US-B2: Indicator-only metrics are flagged in the catalogue.
///
/// BDD Scenario: Indicator-only metrics are correctly flagged
///   Given the application has started
///   When GET /api/metrics is called
///   Then the entries with id "cape", "uk-10yr-gilt", and "us-10yr-treasury" have isIndicatorOnly = true
///   And all other 11 entries have isIndicatorOnly = false
/// </summary>
public class IndicatorOnlyFlaggingTests
{
    private readonly MetricCatalogueService _sut = new();

    [Theory]
    [InlineData("cape", MetricId.Cape)]
    [InlineData("uk-10yr-gilt", MetricId.Uk10YrGilt)]
    [InlineData("us-10yr-treasury", MetricId.Us10YrTreasury)]
    public void WhenGetMetricsIsCalled_IndicatorMetricHasIsIndicatorOnlyTrue(string expectedId, MetricId metricId)
    {
        var metrics = _sut.GetAll();

        var metric = Assert.Single(metrics, m => m.Id == metricId);
        Assert.Equal(expectedId, metric.Id.ToDisplayString());
        Assert.True(metric.IsIndicatorOnly,
            $"Expected metric '{expectedId}' to have isIndicatorOnly = true");
    }

    [Fact]
    public void WhenGetMetricsIsCalled_ExactlyThreeMetricsHaveIsIndicatorOnlyTrue()
    {
        var metrics = _sut.GetAll();

        var indicatorOnly = metrics.Where(m => m.IsIndicatorOnly).ToList();
        Assert.Equal(3, indicatorOnly.Count);
    }

    [Fact]
    public void WhenGetMetricsIsCalled_AllOther11EntriesHaveIsIndicatorOnlyFalse()
    {
        var metrics = _sut.GetAll();

        var nonIndicator = metrics.Where(m => !m.IsIndicatorOnly).ToList();
        Assert.Equal(11, nonIndicator.Count);
        Assert.All(nonIndicator, m =>
            Assert.False(m.IsIndicatorOnly,
                $"Expected metric '{m.Id.ToDisplayString()}' to have isIndicatorOnly = false"));
    }
}
