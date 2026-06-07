using MacroMetrics.Abstractions.Services;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Abstractions.Services.Metrics;
using MacroMetrics.Abstractions.Services.Normalisation;
using MacroMetrics.Services;
using MacroMetrics.Services.Fetchers;
using MacroMetrics.Services.Metrics;
using MacroMetrics.Services.Normalisation;
namespace MacroMetrics.WebApi;

public static class ServiceRegistration
{
    public static void AddBackendServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

        // In-memory cache — used by MetricSeriesOrchestrator to cache normalised series for 1 hour
        services.AddMemoryCache();

        services.AddScoped<IStatusService, StatusService>();
        services.AddSingleton<IMetricCatalogueService, MetricCatalogueService>();
        services.AddScoped<IMetricSeriesService, MetricSeriesService>();
        services.AddScoped<IMetricRatioService, MetricRatioService>();
        services.AddScoped<IDataNormalisationService, DataNormalisationService>();

        // ONS fetcher — typed HTTP client targeting the ONS REST API
        services.AddHttpClient<IOnsFetcherService, OnsFetcherService>(client =>
        {
            client.BaseAddress = new Uri("https://api.ons.gov.uk");
        });

        // Shiller fetcher — typed HTTP client targeting the multpl.com JSON API (no API key required)
        // Source: https://www.multpl.com/shiller-pe/table/by-month.json
        // This is the correct authoritative source for the Shiller CAPE ratio; FRED does not host this series.
        services.AddHttpClient<IShillerFetcherService, ShillerFetcherService>(client =>
        {
            client.BaseAddress = new Uri("https://www.multpl.com");
        });

        // Validate that the FRED API key is present before the app starts serving requests.
        // The key must be supplied via the environment variable FRED__ApiKey (which .NET maps
        // to the configuration key "Fred:ApiKey"). Failing here prevents silent data gaps.
        var fredApiKey = configuration["Fred:ApiKey"];
        if (string.IsNullOrWhiteSpace(fredApiKey))
            throw new InvalidOperationException(
                "FRED API key is missing. Supply the 'Fred:ApiKey' configuration key " +
                "(environment variable FRED__ApiKey).");

        // FRED fetcher — typed HTTP client targeting the FRED REST API
        services.AddHttpClient<IFredFetcherService, FredFetcherService>(client =>
        {
            client.BaseAddress = new Uri("https://api.stlouisfed.org");
        });

        // Validate that the yfinance sidecar base URL is present before the app starts serving
        // requests. The URL must be supplied via the environment variable
        // YFINANCE__SidecarBaseUrl (which .NET maps to the configuration key
        // "YFinance:SidecarBaseUrl"). Failing here prevents silent data gaps at runtime.
        var yFinanceSidecarBaseUrl = configuration["YFinance:SidecarBaseUrl"];
        if (string.IsNullOrWhiteSpace(yFinanceSidecarBaseUrl))
            throw new InvalidOperationException(
                "yfinance sidecar base URL is missing. Supply the 'YFinance:SidecarBaseUrl' " +
                "configuration key (environment variable YFINANCE__SidecarBaseUrl).");

        // YFinance fetcher — typed HTTP client targeting the Python yfinance sidecar
        services.AddHttpClient<IYFinanceFetcherService, YFinanceFetcherService>(client =>
        {
            client.BaseAddress = new Uri(yFinanceSidecarBaseUrl);
        });

        // Orchestrator — routes each metric to its correct fetcher by source
        services.AddScoped<IMetricSeriesOrchestrator, MetricSeriesOrchestrator>();
    }
}
