using energy_backend.Application.Models;

namespace energy_backend.Application.Interfaces;

public interface ILiveDataOrchestratorService
{
    Task<LiveSnapshotDto> GetLiveSnapshotAsync(Guid organisationId);
}
