using energy_backend.Entities;
using energy_backend.Models;

namespace energy_backend.Services
{
    public interface IOrganisationService
    {
        Task<IEnumerable<OrganisationResponseDto>?> GetAllOrganisationsAsync(Guid userId);
        Task<OrganisationResponseDto?> CreateOrganisationAsync(Guid userId, OrganisationRequestDto request);
        Task<OrganisationResponseDto?> UpdateOrganisationAsync(Guid userId, Guid organisationId, OrganisationRequestDto request);
        Task<bool> DeleteOrganisationAsync(Guid userId, Guid organisationId);
        Task<bool> HasOrganisationAsync(Guid userId);
        
    }

}
