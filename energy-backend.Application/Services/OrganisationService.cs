using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
namespace energy_backend.Application.Services;

public class OrganisationService(
    IOrganisationRepository repository) : IOrganisationService
{
    // FIX: was also injecting IEnergyAnalyticsOrchestratorService and calling it
    // from GetOrganisationAnalyticsAsync — application services should not call each
    // other. Analytics is now fetched directly by the controller (OrganisationController)
    // which already has IEnergyAnalyticsOrchestratorService injected.
    // GetOrganisationAnalyticsAsync is removed from this service; the controller
    // calls orgService.GetByIdAsync for the existence check, then calls
    // analyticsOrchestrator.GetOrganisationAnalyticsAsync separately.
    public async Task<IEnumerable<OrganisationResponseDto>?> GetAllOrganisationsAsync(Guid userId)
    {
        var organisations = await repository.GetAllByUserIdAsync(userId);
        return organisations.Select(MapToDto);
    }
    // ADDED: needed by the controller to check org ownership before fetching analytics
    public async Task<OrganisationResponseDto?> GetByIdAsync(Guid userId, Guid organisationId)
    {
        var org = await repository.GetByIdAsync(userId, organisationId);
        return org is null ? null : MapToDto(org);
    }
    public async Task<OrganisationResponseDto?> CreateOrganisationAsync(Guid userId, OrganisationRequestDto request)
    {
        var organisation = new Organisation
        {
            OrganisationId = Guid.NewGuid(),
            Name = request.Name,
            Type = request.Type,
            UserId = userId,
            // RENAMED: was request.EnergyBudget → organisation.Budget (inconsistent names)
            EnergyBudgetKwh = request.EnergyBudgetKwh
        };
        await repository.AddAsync(organisation);
        await repository.SaveChangesAsync();
        return MapToDto(organisation);
    }
    public async Task<OrganisationResponseDto?> UpdateOrganisationAsync(
        Guid userId, Guid organisationId, OrganisationRequestDto request)
    {
        var organisation = await repository.GetByIdAsync(userId, organisationId);
        if (organisation is null) return null;
        organisation.Name = request.Name;
        organisation.Type = request.Type;
        organisation.EnergyBudgetKwh = request.EnergyBudgetKwh;
        await repository.SaveChangesAsync();
        return MapToDto(organisation);
    }
    public async Task<bool> DeleteOrganisationAsync(Guid userId, Guid organisationId)
    {
        var organisation = await repository.GetByIdAsync(userId, organisationId);
        if (organisation is null) return false;
        await repository.DeleteAsync(organisation);
        await repository.SaveChangesAsync();
        return true;
    }
    public async Task<bool> HasOrganisationAsync(Guid userId)
        => await repository.ExistsAsync(userId);
    private static OrganisationResponseDto MapToDto(Organisation o) => new()
    {
        OrganisationId = o.OrganisationId,
        Name = o.Name,
        Type = o.Type,
        EnergyBudgetKwh = o.EnergyBudgetKwh,
        DeviceCount = o.Devices?.Count ?? 0
    };
}