using energy_backend.Application.Models;

namespace energy_backend.Application.Interfaces;

public interface IHistoricalDataOrchestratorService
{
    Task<OrganisationAnalyticsDto?> GetHistoricalSnapshotAsync(
        Guid organisationId,
        string preset = "7d");
}