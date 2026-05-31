using energy_backend.Application.Models;
namespace energy_backend.Application.Interfaces;

public interface IOrganisationService
{
    Task<IEnumerable<OrganisationResponseDto>?> GetAllOrganisationsAsync(Guid userId);
    // ADDED: needed by OrganisationController for ownership check before analytics
    Task<OrganisationResponseDto?> GetByIdAsync(Guid userId, Guid organisationId);
    Task<OrganisationResponseDto?> CreateOrganisationAsync(Guid userId, OrganisationRequestDto request);
    Task<OrganisationResponseDto?> UpdateOrganisationAsync(Guid userId, Guid organisationId, OrganisationRequestDto request);
    Task<bool> DeleteOrganisationAsync(Guid userId, Guid organisationId);
    Task<bool> HasOrganisationAsync(Guid userId);
    // REMOVED: GetOrganisationAnalyticsAsync — was causing OrganisationService to call
    // IEnergyAnalyticsOrchestratorService, creating a cross-service dependency.
    // The controller now calls the two services independently.
}