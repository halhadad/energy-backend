using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using energy_backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories
{
    public class DeviceRepository(EnergyDbContext context): IDeviceRepository
    {
        public async Task<IEnumerable<Device>> GetAllByUserIdAsync(Guid userId)
        {
            return await context.Devices
                .Where(d => d.Organisation!.UserId == userId)
                .ToListAsync();
        }

        public async Task<Device?> GetByIdAsync(Guid userId, Guid deviceId)
        {
            return await context.Devices
                .Include(d => d.Organisation)
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.Organisation!.UserId == userId);
        }

        public async Task<IEnumerable<Device>> GetByOrganisationIdAsync(Guid userId, Guid organisationId)
        {
            var organisation = await context.Organisations
                .Include(o => o.Devices)
                .FirstOrDefaultAsync(o => o.OrganisationId == organisationId && o.UserId == userId);

            return organisation?.Devices ?? Enumerable.Empty<Device>();
        }

        public async Task<Device?> AddAsync(Device device)
        {
            await context.Devices.AddAsync(device);
            return device;
        }

        public async Task<bool> DeleteAsync(Device device)
        {
            context.Devices.Remove(device);
            return true;
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}
