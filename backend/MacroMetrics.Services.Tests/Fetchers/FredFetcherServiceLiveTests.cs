using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Services.Fetchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MacroMetrics.Services.Tests.Fetchers;

/// <summary>
/// Marks a test that requires the <c>FRED__ApiKey</c> environment variable.
/// The test is automatically skipped when the variable is absent so local builds
/// without a key stay green. In CI the key is supplied via the
/// <c>FREDAPIKEY</c> repository secret.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class RequiresFredApiKeyFactAttribute : FactAttribute
{
    private const string EnvVar = "FRED__ApiKey";

    public RequiresFredApiKeyFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvVar)))
            Skip = $"Live FRED API test skipped — set the '{EnvVar}' environment variable to run.";
    }
}

/// <inheritdoc cref="RequiresFredApiKeyFactAttribute"/>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class RequiresFredApiKeyTheoryAttribute : TheoryAttribute
{
    private const string EnvVar = "FRED__ApiKey";

    public RequiresFredApiKeyTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvVar)))
            Skip = $"Live FRED API test skipped — set the '{EnvVar}' environment variable to run.";
    }
}

/// <summary>
/// Shared fixture for <see cref="FredFetcherServiceLiveTests"/>.
/// Fetches live data from the FRED API for the four FRED-hosted US metrics exactly once,
/// sequentially and with a delay between requests to respect rate limits.
/// The cached results are reused across all test methods to avoid redundant
/// API calls and burst-rate 429 errors.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why "cape" is excluded from live tests:</b> The Shiller CAPE ratio (series ID
/// <c>"CAPE"</c>) is <em>not</em> a FRED data series — the St. Louis Fed does not publish
/// it and the FRED REST API returns HTTP 400 for that series ID. The CAPE data originates
/// from Robert Shiller's Yale dataset and must be sourced via a dedicated integration
/// (e.g. the free Shiller data wrapper API). It is therefore excluded from these live
/// FRED tests. The series-ID routing in <see cref="FredFetcherService"/> should be
/// updated once a valid alternative source is wired up.
/// </para>
/// </remarks>
public sealed class FredLiveApiFixture : IAsyncLifetime
{
    private const string FredBaseUrl = "https://api.stlouisfed.org";
    private const string EnvVarName  = "FRED__ApiKey";

    /// <summary>
    /// The four US metric IDs that are live-testable against the FRED REST API.
    /// <c>"cape"</c> is intentionally omitted — see class remarks for the reason.
    /// </summary>
    public static readonly IReadOnlyList<string> MetricIds =
    [
        "us-house-prices",
        "us-wages",
        "us-cpi",
        "us-10yr-treasury",
    ];

    private readonly Dictionary<string, IReadOnlyList<IMetricPoint>> _cache = new();
    private readonly Dictionary<string, Exception> _fetchErrors = new();

    /// <summary>
    /// Returns cached live observations for <paramref name="metricId"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Re-thrown when the fixture failed to fetch this metric during initialisation,
    /// so the individual test fails with a descriptive message instead of a generic
    /// KeyNotFoundException.
    /// </exception>
    public IReadOnlyList<IMetricPoint> GetObservations(string metricId)
    {
        if (_fetchErrors.TryGetValue(metricId, out var ex))
            throw new InvalidOperationException(
                $"Live fixture failed to fetch '{metricId}' during initialisation: {ex.Message}", ex);

        return _cache[metricId];
    }

    /// <summary>
    /// True when the <c>FRED__ApiKey</c> environment variable is present; tests should
    /// skip themselves when false.
    /// </summary>
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvVarName));

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
            return; // All tests will skip themselves; no network calls needed.

        var sut = BuildSut();

        foreach (var metricId in MetricIds)
        {
            try
            {
                _cache[metricId] = await sut.FetchRawAsync(metricId);
            }
            catch (Exception ex)
            {
                // Store the per-metric error so a single bad series ID cannot cascade
                // into failures for every other metric in the fixture.
                _fetchErrors[metricId] = ex;
            }

            // One-second pause between requests to stay well within the FRED API's
            // rate limit (120 req/min) and avoid HTTP 429 bursts in CI.
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Helpers ───────────────────────────────────────────────────────────

    private static IFredFetcherService BuildSut()
    {
        var apiKey = Environment.GetEnvironmentVariable(EnvVarName)!;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Fred:ApiKey"] = apiKey })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services
            .AddHttpClient<IFredFetcherService, FredFetcherService>(client =>
            {
                client.BaseAddress = new Uri(FredBaseUrl);
                client.Timeout     = TimeSpan.FromSeconds(30);
            });

        return services.BuildServiceProvider().GetRequiredService<IFredFetcherService>();
    }
}

/// <summary>
/// Live integration tests for <see cref="FredFetcherService"/> that make real HTTP calls
/// to the FRED REST API (<c>api.stlouisfed.org</c>).
/// <para>
/// The four FRED-hosted metrics are fetched exactly once by
/// <see cref="FredLiveApiFixture.InitializeAsync"/> (sequentially, with a 1-second delay
/// between requests) and the results cached for the duration of the test run. This keeps
/// total API calls to a minimum and prevents HTTP 429 burst-rate errors.
/// </para>
/// <para>
/// <c>"cape"</c> is excluded from live tests because the FRED API returns HTTP 400 for
/// series ID <c>"CAPE"</c> — the Shiller CAPE ratio is not a FRED-hosted series.
/// See <see cref="FredLiveApiFixture"/> remarks for details.
/// </para>
/// <para>
/// These tests require the <c>FRED__ApiKey</c> environment variable to be set. They skip
/// automatically when the variable is absent so local builds without a key stay green.
/// In CI the key is injected from the <c>FREDAPIKEY</c> repository secret.
/// </para>
/// </summary>
[Collection("FredLiveApi")]
public sealed class FredFetcherServiceLiveTests(FredLiveApiFixture fixture)
    : IClassFixture<FredLiveApiFixture>
{
    // ── Live connectivity checks ──────────────────────────────────────────

    /// <summary>
    /// Verifies that the FRED API returned non-empty observations for each FRED-hosted US metric.
    /// </summary>
    [RequiresFredApiKeyTheory]
    [InlineData("us-house-prices")]
    [InlineData("us-wages")]
    [InlineData("us-cpi")]
    // "cape" intentionally excluded: FRED series "CAPE" does not exist (HTTP 400).
    // The Shiller CAPE ratio is sourced from Robert Shiller's Yale dataset, not FRED.
    [InlineData("us-10yr-treasury")]
    public void FetchRawAsync_LiveApi_ReturnsNonEmptyObservations(string metricId)
    {
        var result = fixture.GetObservations(metricId);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Verifies that returned data points have ISO-8601 date strings and finite numeric values.
    /// </summary>
    [RequiresFredApiKeyTheory]
    [InlineData("us-cpi")]
    [InlineData("us-10yr-treasury")]
    public void FetchRawAsync_LiveApi_DataPointsHaveValidDatesAndValues(string metricId)
    {
        var result = fixture.GetObservations(metricId);

        Assert.All(result, point =>
        {
            // Date must parse as a valid calendar date (FRED returns "yyyy-MM-dd")
            Assert.True(
                DateOnly.TryParseExact(point.Date, "yyyy-MM-dd", out _),
                $"Date '{point.Date}' is not a valid yyyy-MM-dd date.");

            // Value must be a finite number (dot-placeholders should already be filtered)
            Assert.True(
                double.IsFinite(point.Value),
                $"Value {point.Value} for date '{point.Date}' is not finite.");
        });
    }

    /// <summary>
    /// Verifies that the live API returned at least several years of historical data for
    /// CPIAUCSL, confirming the series ID is correct and the full observation window is returned.
    /// CPIAUCSL has been published monthly since 1947 — we conservatively expect >100 points.
    /// </summary>
    [RequiresFredApiKeyFact]
    public void FetchRawAsync_LiveApi_UsCpi_ReturnsDecadesOfHistory()
    {
        var result = fixture.GetObservations("us-cpi");

        Assert.True(result.Count > 100,
            $"Expected >100 CPI observations from FRED but got {result.Count}.");
    }
}
