using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Entities;

namespace energy_backend.Core.Interfaces
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<Device>> GetAllByUserIdAsync(Guid userId);
        Task<Device?> GetByIdAsync(Guid userId, Guid deviceId);
        Task<IEnumerable<Device>> GetByOrganisationIdAsync(Guid userId, Guid organisationId);
        Task<Device?> AddAsync(Device device);
        Task<bool> DeleteAsync(Device device);
        Task SaveChangesAsync();
    }
}
