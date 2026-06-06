using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Services.Metrics;
using Microsoft.Extensions.Caching.Memory;

namespace MacroMetrics.Services.Tests.Orchestrator;


/// <summary>
/// BDD-aligned tests derived from Issue #53 (US-B12) and Issue #56 (US-B15).
///
/// Scenario: UK metric routes to ONS fetcher
///   Given real fetcher implementations are wired via DI
///   When MetricSeriesOrchestrator.GetSeriesAsync("uk-house-prices") is called
///   Then IOnsFetcherService.FetchRawAsync is invoked
///   And IFredFetcherService.FetchRawAsync is not invoked
///   And IYFinanceFetcherService.FetchRawAsync is not invoked
///
/// Scenario: Cache hit on second request (US-B15)
///   Given GET /api/metrics/gold has already been called once and the result is cached
///   When GET /api/metrics/gold is called a second time within the same hour
///   Then YFinanceFetcherService.FetchRawAsync is not called again
///   And the response is served from the in-memory cache
///
/// Scenario: Cache miss triggers a fresh fetch (US-B15)
///   Given no cached entry exists for metric "oil"
///   When GET /api/metrics/oil is called
///   Then YFinanceFetcherService.FetchRawAsync is called exactly once
///   And the result is stored in the cache with a 1-hour TTL
/// </summary>
public class MetricSeriesOrchestratorTests
{
    // ---------------------------------------------------------------------------
    // Spy helpers
    // ---------------------------------------------------------------------------

    private sealed class SpyOnsFetcher : IOnsFetcherService
    {
        public bool WasCalled  => CallCount > 0;
        public int  CallCount  { get; private set; }

        public Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyList<IMetricPoint>>(Array.Empty<IMetricPoint>());
        }
    }

    private sealed class SpyFredFetcher : IFredFetcherService
    {
        public bool WasCalled  => CallCount > 0;
        public int  CallCount  { get; private set; }

        public Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyList<IMetricPoint>>(Array.Empty<IMetricPoint>());
        }
    }

    private sealed class SpyYFinanceFetcher : IYFinanceFetcherService
    {
        public bool WasCalled  => CallCount > 0;
        public int  CallCount  { get; private set; }

        public Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyList<IMetricPoint>>(Array.Empty<IMetricPoint>());
        }
    }

    private sealed class SpyShillerFetcher : IShillerFetcherService
    {
        public bool WasCalled  => CallCount > 0;
        public int  CallCount  { get; private set; }

        public Task<IReadOnlyList<IMetricPoint>> FetchRawAsync(string metricId)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyList<IMetricPoint>>(Array.Empty<IMetricPoint>());
        }
    }

    // ---------------------------------------------------------------------------
    // Factory helpers
    // ---------------------------------------------------------------------------

    private static (MetricSeriesOrchestrator sut, SpyOnsFetcher ons, SpyFredFetcher fred, SpyYFinanceFetcher yf, SpyShillerFetcher shiller)
        BuildSut(IMemoryCache? cache = null)
    {
        var catalogue = new MetricCatalogueService();
        var ons       = new SpyOnsFetcher();
        var fred      = new SpyFredFetcher();
        var yf        = new SpyYFinanceFetcher();
        var shiller   = new SpyShillerFetcher();
        var memCache  = cache ?? new MemoryCache(new MemoryCacheOptions());
        var sut       = new MetricSeriesOrchestrator(catalogue, ons, fred, yf, shiller, memCache);
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

    // ---------------------------------------------------------------------------
    // Caching behaviour (US-B15)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Scenario: Cache hit on second request
    ///   Given GET /api/metrics/gold has already been called once and the result is cached
    ///   When GET /api/metrics/gold is called a second time within the same hour
    ///   Then YFinanceFetcherService.FetchRawAsync is not called again
    ///   And the response is served from the in-memory cache
    ///   And the response status is 200
    /// </summary>
    [Fact]
    public async Task GetSeriesAsync_SecondCallForSameMetric_ServedFromCache_FetcherNotCalledAgain()
    {
        var (sut, _, _, yf, _) = BuildSut();

        // First call — populates the cache
        var firstResult  = await sut.GetSeriesAsync("gold");

        // Second call — should be served from cache
        var secondResult = await sut.GetSeriesAsync("gold");

        Assert.True(yf.CallCount == 1,
            "YFinanceFetcherService.FetchRawAsync should be called exactly once; the second response must come from cache.");
        Assert.NotNull(secondResult);
        Assert.Same(firstResult, secondResult);
    }

    /// <summary>
    /// Scenario: Cache miss triggers a fresh fetch
    ///   Given no cached entry exists for metric "oil"
    ///   When GET /api/metrics/oil is called
    ///   Then YFinanceFetcherService.FetchRawAsync is called exactly once
    ///   And the result is stored in the cache with a 1-hour TTL
    /// </summary>
    [Fact]
    public async Task GetSeriesAsync_CacheMiss_FetcherCalledOnce_ResultStoredInCache()
    {
        var realCache          = new MemoryCache(new MemoryCacheOptions());
        var (sut, _, _, yf, _) = BuildSut(realCache);

        // Act — first (and only) request; no prior cache entry
        var result = await sut.GetSeriesAsync("oil");

        // Fetcher was called exactly once
        Assert.True(yf.CallCount == 1,
            "YFinanceFetcherService.FetchRawAsync should be called exactly once on a cache miss.");

        // Result is in the cache with the expected 1-hour TTL key
        var found = realCache.TryGetValue("metric-series:oil", out _);
        Assert.True(found, "The result should be stored in IMemoryCache after a cache miss.");
        Assert.NotNull(result);
    }

    /// <summary>
    /// Verifies that the TTL constant on the orchestrator is exactly 1 hour,
    /// matching the requirement in US-B15.
    /// </summary>
    [Fact]
    public void CacheTtl_IsOneHour()
    {
        Assert.Equal(TimeSpan.FromHours(1), MetricSeriesOrchestrator.CacheTtl);
    }
}
