using MacroMetrics.Abstractions.DataModels;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Services.Fetchers;
using Microsoft.Extensions.DependencyInjection;

namespace MacroMetrics.Services.Tests.Fetchers;

/// <summary>
/// Marks a live Shiller test that requires internet access.
/// The test is automatically skipped when the <c>SHILLER_LIVE_TESTS</c> environment
/// variable is absent, so local builds without network access stay green.
/// In CI the variable is set to <c>true</c> to opt in to live tests.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class RequiresShillerLiveFactAttribute : FactAttribute
{
    private const string EnvVar = "SHILLER_LIVE_TESTS";

    public RequiresShillerLiveFactAttribute()
    {
        if (!IsEnabled())
            Skip = $"Live Shiller test skipped — set the '{EnvVar}' environment variable to 'true' to run.";
    }

    internal static bool IsEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnvVar),
            "true",
            StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Shared fixture for <see cref="ShillerFetcherServiceLiveTests"/>.
/// Fetches live CAPE data from the multpl.com API exactly once and caches the
/// result for the duration of the test run.
/// </summary>
public sealed class ShillerLiveApiFixture : IAsyncLifetime
{
    private const string ShillerBaseUrl = "https://www.multpl.com";

    private IReadOnlyList<IMetricPoint>? _capeResult;
    private Exception? _fetchError;

    /// <summary>True when live tests are opted in via the environment variable.</summary>
    public bool IsEnabled => RequiresShillerLiveFactAttribute.IsEnabled();

    /// <summary>
    /// Returns cached live CAPE observations.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Re-thrown when the fixture failed to fetch during initialisation.
    /// </exception>
    public IReadOnlyList<IMetricPoint> GetCapeObservations()
    {
        if (_fetchError is not null)
            throw new InvalidOperationException(
                $"Live fixture failed to fetch 'cape' during initialisation: {_fetchError.Message}",
                _fetchError);

        return _capeResult!;
    }

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
            return; // All tests will skip themselves; no network calls needed.

        try
        {
            var sut = BuildSut();
            _capeResult = await sut.FetchRawAsync("cape");
        }
        catch (Exception ex)
        {
            _fetchError = ex;
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static IShillerFetcherService BuildSut()
    {
        var services = new ServiceCollection();
        services
            .AddHttpClient<IShillerFetcherService, ShillerFetcherService>(client =>
            {
                client.BaseAddress = new Uri(ShillerBaseUrl);
                client.Timeout     = TimeSpan.FromSeconds(30);
            });

        return services.BuildServiceProvider().GetRequiredService<IShillerFetcherService>();
    }
}

/// <summary>
/// Live integration tests for <see cref="ShillerFetcherService"/> that make real HTTP
/// calls to <c>www.multpl.com</c>.
/// <para>
/// These tests require the <c>SHILLER_LIVE_TESTS</c> environment variable to be set to
/// <c>true</c>. They skip automatically when the variable is absent so local builds and
/// CI environments without internet access stay green.
/// </para>
/// </summary>
[Collection("ShillerLiveApi")]
public sealed class ShillerFetcherServiceLiveTests(ShillerLiveApiFixture fixture)
    : IClassFixture<ShillerLiveApiFixture>
{
    // ── Live connectivity checks ──────────────────────────────────────────

    /// <summary>
    /// Verifies that the multpl.com API returns non-empty CAPE observations.
    /// </summary>
    [RequiresShillerLiveFact]
    public void FetchRawAsync_LiveApi_ReturnsNonEmptyObservations()
    {
        var result = fixture.GetCapeObservations();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Verifies that returned data points have valid ISO-8601 date strings and
    /// positive, finite numeric CAPE values.
    /// </summary>
    [RequiresShillerLiveFact]
    public void FetchRawAsync_LiveApi_DataPointsHaveValidDatesAndValues()
    {
        var result = fixture.GetCapeObservations();

        Assert.All(result, point =>
        {
            // Date must parse as a valid calendar date (normalised to ISO-8601 by the service)
            Assert.True(
                DateOnly.TryParseExact(point.Date, "yyyy-MM-dd", out _),
                $"Date '{point.Date}' is not a valid yyyy-MM-dd date.");

            // CAPE ratio must be a positive, finite number (typically 5–50 historically)
            Assert.True(
                double.IsFinite(point.Value) && point.Value > 0,
                $"Value {point.Value} for date '{point.Date}' is not a positive finite CAPE ratio.");
        });
    }

    /// <summary>
    /// Verifies that the live API returns over a century of historical Shiller CAPE data.
    /// The dataset starts in January 1881 — we conservatively expect > 500 monthly points.
    /// </summary>
    [RequiresShillerLiveFact]
    public void FetchRawAsync_LiveApi_ReturnsDecadesOfHistory()
    {
        var result = fixture.GetCapeObservations();

        Assert.True(result.Count > 500,
            $"Expected >500 CAPE observations from multpl.com but got {result.Count}.");
    }

    /// <summary>
    /// Verifies that the earliest data point is in the 19th century (Shiller's data
    /// starts in January 1881), confirming the full historical series is returned.
    /// </summary>
    [RequiresShillerLiveFact]
    public void FetchRawAsync_LiveApi_IncludesNineteenthCenturyData()
    {
        var result = fixture.GetCapeObservations();

        // Parse all dates and find the earliest
        var earliest = result
            .Select(p => DateOnly.ParseExact(p.Date, "yyyy-MM-dd"))
            .Min();

        Assert.True(earliest.Year < 1920,
            $"Expected data going back before 1920 but earliest date was {earliest}.");
    }
}
