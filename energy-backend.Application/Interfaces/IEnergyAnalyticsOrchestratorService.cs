// energy-backend.Application/Interfaces/IEnergyAnalyticsOrchestratorService.cs
using energy_backend.Application.Models;

namespace energy_backend.Application.Interfaces;

public interface IEnergyAnalyticsOrchestratorService
{
    Task<OrganisationAnalyticsDto?> GetOrganisationAnalyticsAsync(Guid organisationId);
}
