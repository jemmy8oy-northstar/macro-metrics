using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Services.Metrics;

namespace MacroMetrics.Services.Tests.Orchestrator;


/// <summary>
/// BDD-aligned tests derived from Issue #53 (US-B12).
///
/// Scenario: UK metric routes to ONS fetcher
///   Given real fetcher implementations are wired via DI
///   When MetricSeriesOrchestrator.GetSeriesAsync("uk-house-prices") is called
///   Then IOnsFetcherService.FetchRawAsync is invoked
///   And IFredFetcherService.FetchRawAsync is not invoked
///   And IYFinanceFetcherService.FetchRawAsync is not invoked
/// </summary>
public class MetricSeriesOrchestratorTests
{
    // ---------------------------------------------------------------------------
    // Spy helpers
    // ---------------------------------------------------------------------------

    private sealed class SpyOnsFetcher : IOnsFetcherService
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
        {
            WasCalled = true;
            return Task.FromResult<IReadOnlyList<IMetricPoint>>(Array.Empty<IMetricPoint>());
        }
    }

    private sealed class SpyFredFetcher : IFredFetcherService
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
        {
            WasCalled = true;
            return Task.FromResult<IReadOnlyList<IMetricPoint>>(Array.Empty<IMetricPoint>());
        }
    }

    private sealed class SpyYFinanceFetcher : IYFinanceFetcherService
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
        {
            WasCalled = true;
            return Task.FromResult<IReadOnlyList<IMetricPoint>>(Array.Empty<IMetricPoint>());
        }
    }

    private sealed class SpyShillerFetcher : IShillerFetcherService
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
        {
            WasCalled = true;
            return Task.FromResult<IReadOnlyList<IMetricPoint>>(Array.Empty<IMetricPoint>());
        }
    }

    // ---------------------------------------------------------------------------
    // Factory helpers
    // ---------------------------------------------------------------------------

    private static (MetricSeriesOrchestrator sut, SpyOnsFetcher ons, SpyFredFetcher fred, SpyYFinanceFetcher yf, SpyShillerFetcher shiller)
        BuildSut()
    {
        var catalogue = new MetricCatalogueService();
        var ons       = new SpyOnsFetcher();
        var fred      = new SpyFredFetcher();
        var yf        = new SpyYFinanceFetcher();
        var shiller   = new SpyShillerFetcher();
        var sut       = new MetricSeriesOrchestrator(catalogue, ons, fred, yf, shiller);
        return (sut, ons, fred, yf, shiller);
    }

    // ---------------------------------------------------------------------------
    // UK metrics → ONS fetcher
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("uk-house-prices")]
    [InlineData("uk-wages")]
    [InlineData("uk-cpi")]
    public async Task GetSeriesAsync_UkMetric_InvokesOnsFetcher(string metricId)
    {
        var (sut, ons, _, _, _) = BuildSut();

        await sut.GetSeriesAsync(metricId);

        Assert.True(ons.WasCalled,
            $"Expected IOnsFetcherService.FetchRawAsync to be called for '{metricId}'");
    }

    [Theory]
    [InlineData("uk-house-prices")]
    [InlineData("uk-wages")]
    [InlineData("uk-cpi")]
    public async Task GetSeriesAsync_UkMetric_DoesNotInvokeFredFetcher(string metricId)
    {
        var (sut, _, fred, _, _) = BuildSut();

        await sut.GetSeriesAsync(metricId);

        Assert.False(fred.WasCalled,
            $"Expected IFredFetcherService.FetchRawAsync NOT to be called for '{metricId}'");
    }

    [Theory]
    [InlineData("uk-house-prices")]
    [InlineData("uk-wages")]
    [InlineData("uk-cpi")]
    public async Task GetSeriesAsync_UkMetric_DoesNotInvokeYFinanceFetcher(string metricId)
    {
        var (sut, _, _, yf, _) = BuildSut();

        await sut.GetSeriesAsync(metricId);

        Assert.False(yf.WasCalled,
            $"Expected IYFinanceFetcherService.FetchRawAsync NOT to be called for '{metricId}'");
    }

    // ---------------------------------------------------------------------------
    // FRED metrics → FRED fetcher (not ONS)
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("us-house-prices")]
    [InlineData("us-wages")]
    [InlineData("us-cpi")]
    [InlineData("us-10yr-treasury")]
    public async Task GetSeriesAsync_FredMetric_InvokesFredFetcher(string metricId)
    {
        var (sut, _, fred, _, _) = BuildSut();

        await sut.GetSeriesAsync(metricId);

        Assert.True(fred.WasCalled,
            $"Expected IFredFetcherService.FetchRawAsync to be called for '{metricId}'");
    }

    [Theory]
    [InlineData("us-house-prices")]
    [InlineData("us-wages")]
    [InlineData("us-cpi")]
    public async Task GetSeriesAsync_FredMetric_DoesNotInvokeOnsFetcher(string metricId)
    {
        var (sut, ons, _, _, _) = BuildSut();

        await sut.GetSeriesAsync(metricId);

        Assert.False(ons.WasCalled,
            $"Expected IOnsFetcherService.FetchRawAsync NOT to be called for '{metricId}'");
    }

    // ---------------------------------------------------------------------------
    // YFinance metrics → YFinance fetcher (not ONS)
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("gold")]
    [InlineData("oil")]
    [InlineData("ftse100")]
    [InlineData("sp500")]
    [InlineData("bitcoin")]
    [InlineData("uk-10yr-gilt")]
    public async Task GetSeriesAsync_YFinanceMetric_InvokesYFinanceFetcher(string metricId)
    {
        var (sut, _, _, yf, _) = BuildSut();

        await sut.GetSeriesAsync(metricId);

        Assert.True(yf.WasCalled,
            $"Expected IYFinanceFetcherService.FetchRawAsync to be called for '{metricId}'");
    }

    [Theory]
    [InlineData("gold")]
    [InlineData("oil")]
    [InlineData("ftse100")]
    public async Task GetSeriesAsync_YFinanceMetric_DoesNotInvokeOnsFetcher(string metricId)
    {
        var (sut, ons, _, _, _) = BuildSut();

        await sut.GetSeriesAsync(metricId);

        Assert.False(ons.WasCalled,
            $"Expected IOnsFetcherService.FetchRawAsync NOT to be called for '{metricId}'");
    }

    // ---------------------------------------------------------------------------
    // Shiller metric (cape) → Shiller fetcher
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSeriesAsync_CapeMetric_InvokesShillerFetcher()
    {
        var (sut, _, _, _, shiller) = BuildSut();

        await sut.GetSeriesAsync("cape");

        Assert.True(shiller.WasCalled,
            "Expected IShillerFetcherService.FetchRawAsync to be called for 'cape'");
    }

    [Fact]
    public async Task GetSeriesAsync_CapeMetric_DoesNotInvokeFredFetcher()
    {
        var (sut, _, fred, _, _) = BuildSut();

        await sut.GetSeriesAsync("cape");

        Assert.False(fred.WasCalled,
            "Expected IFredFetcherService.FetchRawAsync NOT to be called for 'cape'");
    }

    [Fact]
    public async Task GetSeriesAsync_CapeMetric_DoesNotInvokeOnsFetcher()
    {
        var (sut, ons, _, _, _) = BuildSut();

        await sut.GetSeriesAsync("cape");

        Assert.False(ons.WasCalled,
            "Expected IOnsFetcherService.FetchRawAsync NOT to be called for 'cape'");
    }

    [Fact]
    public async Task GetSeriesAsync_CapeMetric_DoesNotInvokeYFinanceFetcher()
    {
        var (sut, _, _, yf, _) = BuildSut();

        await sut.GetSeriesAsync("cape");

        Assert.False(yf.WasCalled,
            "Expected IYFinanceFetcherService.FetchRawAsync NOT to be called for 'cape'");
    }

    // ---------------------------------------------------------------------------
    // Unknown metric returns null (maps to 404)
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSeriesAsync_UnknownId_ReturnsNull()
    {
        var (sut, _, _, _, _) = BuildSut();

        var result = await sut.GetSeriesAsync("not-a-real-metric");

        Assert.Null(result);
    }
}
