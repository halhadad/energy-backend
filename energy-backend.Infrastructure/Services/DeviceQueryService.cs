using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class DeviceQueryService : IDeviceQueryService
    {
        private readonly EnergyDbContext _context;
        private readonly ILogger<DeviceQueryService> _logger;

        public DeviceQueryService(EnergyDbContext context, ILogger<DeviceQueryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Device>> GetDevicesForOrganisationAsync(Guid orgId)
        {
            return await _context.Devices
                .Where(d => d.OrganisationId == orgId)
                .ToListAsync();
        }

        public async Task<Device?> GetDeviceByIdAsync(Guid deviceId)
        {
            return await _context.Devices.FindAsync(deviceId);
        }
    }
}
