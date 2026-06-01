using MacroMetrics.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MacroMetrics.Services.Tests.Startup;

/// <summary>
/// BDD-aligned tests for FRED API key startup validation.
///
/// Scenario: Missing FRED API key causes fail-fast on startup
///   Given neither appsettings.json nor the environment supplies a FRED API key
///   When the application starts
///   Then a startup validation exception is thrown
///   And the application does not serve requests
///
/// Scenario: FRED API key is read from environment, not appsettings
///   Given the FRED API key is absent from appsettings.json
///   And the environment variable FRED__ApiKey is set to a valid key
///   When the application starts
///   Then FredFetcherService reads the key via IConfiguration["Fred:ApiKey"]
///   And no startup exception is thrown
/// </summary>
public sealed class FredStartupValidationTests
{
    // ── Missing key causes fail-fast ──────────────────────────────────────

    [Fact]
    public void AddBackendServices_MissingFredApiKey_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // No Fred:ApiKey supplied — simulates neither appsettings nor env having it
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddBackendServices(config));

        Assert.Contains("FRED API key is missing", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddBackendServices_BlankFredApiKey_ThrowsInvalidOperationException(
        string blankKey)
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Fred:ApiKey"]                         = blankKey,
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
            })
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => services.AddBackendServices(config));
    }

    // ── Present key (via env var simulation) does not throw ──────────────

    /// <summary>
    /// BDD: FRED API key is read from environment, not appsettings.
    /// In production the env var FRED__ApiKey is mapped by .NET to Fred:ApiKey.
    /// Here we simulate that by adding it directly to the in-memory collection.
    /// </summary>
    [Fact]
    public void AddBackendServices_FredApiKeyPresentViaConfiguration_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Simulates FRED__ApiKey environment variable (double-underscore maps to Fred:ApiKey)
                ["Fred:ApiKey"]                         = "a-valid-fred-api-key",
                // Also supply the yfinance sidecar URL so startup validation doesn't throw for that
                ["YFinance:SidecarBaseUrl"]             = "http://localhost:8000",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
            })
            .Build();

        // Should not throw
        var ex = Record.Exception(() => services.AddBackendServices(config));
        Assert.Null(ex);
    }
}
