using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Entities;

namespace energy_backend.Core.Interfaces
{
    public interface IOrganisationRepository
    {
        Task<IEnumerable<Organisation>> GetAllByUserIdAsync(Guid userId);
        Task<Organisation?> GetByIdAsync(Guid userId, Guid organisationId);
        Task<Organisation?> AddAsync(Organisation organisation);
        Task<bool> DeleteAsync(Organisation organisation);
        Task<bool> ExistsAsync(Guid userId);
        Task SaveChangesAsync();
    }
}
