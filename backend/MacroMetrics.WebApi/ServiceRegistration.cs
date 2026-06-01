using MacroMetrics.Abstractions.Services;
using MacroMetrics.Abstractions.Services.Fetchers;
using MacroMetrics.Abstractions.Services.Metrics;
using MacroMetrics.Abstractions.Services.Normalisation;
using MacroMetrics.Services;
using MacroMetrics.Services.Fetchers;
using MacroMetrics.Services.Metrics;
using MacroMetrics.Services.Normalisation;
using MacroMetrics.Database;
using Microsoft.EntityFrameworkCore;

namespace MacroMetrics.WebApi;

public static class ServiceRegistration
{
    public static void AddBackendServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("MacroMetrics.Database")));

        services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));
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

        // FRED fetcher — stub implementation; real HTTP calls wired in a future story
        services.AddScoped<IFredFetcherService, FredFetcherService>();

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
