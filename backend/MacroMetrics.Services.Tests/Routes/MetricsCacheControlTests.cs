using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MacroMetrics.Services.Tests.Routes;

/// <summary>
/// BDD-aligned integration tests derived from Issue #57 (US-B16).
///
/// Scenario: Metric catalogue response includes correct Cache-Control header
///   When GET /api/metrics is called
///   Then the response includes the header: Cache-Control: public, max-age=3600
///
/// Scenario: Single metric series response includes correct Cache-Control header
///   When GET /api/metrics/gold is called
///   Then the response includes the header: Cache-Control: public, max-age=3600
///
/// Scenario: Ratio series response includes correct Cache-Control header
///   When GET /api/metrics/ratio?numerator=gold&amp;denominator=us-wages is called
///   Then the response includes the header: Cache-Control: public, max-age=3600
/// </summary>
public class MetricsCacheControlTests : IClassFixture<MacroMetricsWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MetricsCacheControlTests(MacroMetricsWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Scenario: Metric catalogue response includes correct Cache-Control header
    ///   When GET /api/metrics is called
    ///   Then the response status is 200
    ///   And the response includes Cache-Control: public, max-age=3600
    /// </summary>
    [Fact]
    public async Task GetMetrics_CatalogueEndpoint_IncludesCacheControlHeader()
    {
        var response = await _client.GetAsync("/api/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.True(cacheControl.Public,
            "Expected Cache-Control 'public' directive on GET /api/metrics.");
        Assert.Equal(TimeSpan.FromHours(1), cacheControl.MaxAge);
    }

    /// <summary>
    /// Scenario: Single metric series response includes correct Cache-Control header
    ///   When GET /api/metrics/gold is called
    ///   Then the response status is 200
    ///   And the response includes Cache-Control: public, max-age=3600
    /// </summary>
    [Fact]
    public async Task GetMetricSeries_SingleMetricEndpoint_IncludesCacheControlHeader()
    {
        var response = await _client.GetAsync("/api/metrics/gold");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.True(cacheControl.Public,
            "Expected Cache-Control 'public' directive on GET /api/metrics/gold.");
        Assert.Equal(TimeSpan.FromHours(1), cacheControl.MaxAge);
    }

    /// <summary>
    /// Scenario: Ratio series response includes correct Cache-Control header
    ///   When GET /api/metrics/ratio?numerator=gold&amp;denominator=us-wages is called
    ///   Then the response status is 200
    ///   And the response includes Cache-Control: public, max-age=3600
    /// </summary>
    [Fact]
    public async Task GetMetricRatio_RatioEndpoint_IncludesCacheControlHeader()
    {
        var response = await _client.GetAsync("/api/metrics/ratio?numerator=gold&denominator=us-wages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.True(cacheControl.Public,
            "Expected Cache-Control 'public' directive on GET /api/metrics/ratio.");
        Assert.Equal(TimeSpan.FromHours(1), cacheControl.MaxAge);
    }
}

/// <summary>
/// Custom <see cref="WebApplicationFactory{TProgram}"/> that configures the test host
/// with stub configuration values so the app can start without real external dependencies
/// (database, FRED API key, yfinance sidecar).
/// </summary>
public sealed class MacroMetricsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // ConfigureHostConfiguration runs at the host layer, before WebApplicationBuilder
        // processes its service-registration delegates.  This guarantees that the FRED API
        // key guard and the yfinance sidecar URL guard in ServiceRegistration.cs see the
        // stub values rather than throwing on startup.
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Satisfy the startup-time FRED API key guard in ServiceRegistration
                ["Fred:ApiKey"]                         = "test-key",
                // Satisfy the startup-time yfinance sidecar URL guard in ServiceRegistration
                ["YFinance:SidecarBaseUrl"]             = "http://localhost:9999",
                // Provide a connection string so UseNpgsql does not throw on DI setup;
                // the database is never actually opened because migrations are skipped
                // in the "Testing" environment (see Program.cs).
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            });
        });

        return base.CreateHost(builder);
    }
}
