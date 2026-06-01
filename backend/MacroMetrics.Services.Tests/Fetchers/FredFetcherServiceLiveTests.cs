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
/// Live integration tests for <see cref="FredFetcherService"/> that make real HTTP calls
/// to the FRED REST API (<c>api.stlouisfed.org</c>).
/// <para>
/// These tests require the <c>FRED__ApiKey</c> environment variable to be set to a valid
/// FRED API key. They are skipped automatically when the variable is absent so they do not
/// break local builds where a key is unavailable. In CI the key is supplied via the
/// <c>FREDAPIKEY</c> repository secret.
/// </para>
/// </summary>
public sealed class FredFetcherServiceLiveTests
{
    private const string FredBaseUrl = "https://api.stlouisfed.org";
    private const string EnvVarName  = "FRED__ApiKey";

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a real <see cref="FredFetcherService"/> wired through DI with a real
    /// <see cref="HttpClient"/> — no fake handler.
    /// </summary>
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

    // ── Live connectivity checks ──────────────────────────────────────────

    /// <summary>
    /// Verifies that the FRED API is reachable and returns non-empty observations for
    /// each supported US metric.
    /// </summary>
    [RequiresFredApiKeyTheory]
    [InlineData("us-house-prices")]
    [InlineData("us-wages")]
    [InlineData("us-cpi")]
    [InlineData("cape")]
    [InlineData("us-10yr-treasury")]
    public async Task FetchRawAsync_LiveApi_ReturnsNonEmptyObservations(string metricId)
    {
        var result = await BuildSut().FetchRawAsync(metricId);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Verifies that returned data points have ISO-8601 date strings and finite numeric values.
    /// </summary>
    [RequiresFredApiKeyTheory]
    [InlineData("us-cpi")]
    [InlineData("us-10yr-treasury")]
    public async Task FetchRawAsync_LiveApi_DataPointsHaveValidDatesAndValues(string metricId)
    {
        var result = await BuildSut().FetchRawAsync(metricId);

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
    /// Verifies that the live API returns at least several years of historical data for
    /// CPIAUCSL, confirming the series ID is correct and the full observation window is returned.
    /// CPIAUCSL has been published monthly since 1947 — we conservatively expect >100 points.
    /// </summary>
    [RequiresFredApiKeyFact]
    public async Task FetchRawAsync_LiveApi_UsCpi_ReturnsDecadesOfHistory()
    {
        var result = await BuildSut().FetchRawAsync("us-cpi");

        Assert.True(result.Count > 100,
            $"Expected >100 CPI observations from FRED but got {result.Count}.");
    }
}
