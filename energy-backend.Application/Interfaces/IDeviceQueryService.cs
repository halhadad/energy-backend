using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using energy_backend.Core.Entities;

namespace energy_backend.Application.Services
{
    // Abstraction for querying device-related data from infrastructure
    public interface IDeviceQueryService
    {
        Task<List<Device>> GetDevicesForOrganisationAsync(Guid orgId);
        Task<Device?> GetDeviceByIdAsync(Guid deviceId);
    }
}
