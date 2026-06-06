using MacroMetrics.Abstractions.DataModels;

namespace MacroMetrics.Abstractions.Services.Metrics;

public interface IMetricCatalogueService
{
    IReadOnlyList<IMetricMetadata> GetAll();
}
