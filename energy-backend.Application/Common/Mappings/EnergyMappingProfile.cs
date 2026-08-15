using AutoMapper;
using energy_backend.Application.Models;
using energy_backend.Core.Projections;

namespace energy_backend.Application.Common.Mapping;

public class EnergyMappingProfile : Profile
{
    public EnergyMappingProfile()
    {
        // Label and EstimatedCost are filled in by the orchestrator after mapping
        CreateMap<AggregateMetricRow, EnergyMetricRollupDto>()
            .ForMember(d => d.TotalEnergyKwh, o => o.MapFrom(s => (decimal)s.TotalEnergyKwh))
            .ForMember(d => d.Label, o => o.Ignore())
            .ForMember(d => d.EstimatedCost, o => o.Ignore());
    }
}
