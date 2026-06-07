using AutoMapper;
using MacroMetrics.DataModels.Models;
using MacroMetrics.DomainModels.Models;

namespace MacroMetrics.WebApi;

public class MetricsMappingProfile : Profile
{
    public MetricsMappingProfile()
    {
        CreateMap<DomainDataPoint, DataPoint>();
        CreateMap<DomainRatio,     Ratio>();
        CreateMap<DomainIndicator, Indicator>();
    }
}
