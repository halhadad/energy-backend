using energy_backend.Data;
using energy_backend.Entities;
using energy_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Services
{
    public class DeviceService(EnergyDbContext context) : IDeviceService
    {
        public async Task<IEnumerable<DeviceResponseDto>> GetDevicesAsync(Guid userId)
        {
            return await context.Devices
                .Where(d => d.Organisation!.UserId == userId)
                .Select(d => new DeviceResponseDto
                {
                    DeviceId = d.DeviceId,
                    OrganisationId = d.OrganisationId,
                    Name = d.Name,
                    Type = d.Type,
                    EnergyConsumption = d.EnergyConsumption
                }).ToListAsync();
        }

        public async Task<DeviceResponseDto?> GetDeviceByIdAsync(Guid userId, Guid deviceId)
        {
            var device = await context.Devices
                .Include(d => d.Organisation)
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.Organisation!.UserId == userId);

            return device is null ? null : MapDeviceToDto(device);
        }

        public async Task<DeviceResponseDto?> CreateDeviceAsync(Guid userId, DeviceRequestDto request)
        {
            var organisation = await context.Organisations.FirstOrDefaultAsync(o =>
                o.OrganisationId == request.OrganisationId && o.UserId == userId);

            if (organisation is null) return null;

            var device = new Device
            {
                DeviceId = Guid.NewGuid(),
                OrganisationId = request.OrganisationId,
                Name = request.Name,
                Type = request.Type,
                EnergyConsumption = request.EnergyConsumption
            };

            context.Devices.AddAsync(device);
            await context.SaveChangesAsync();

            return MapDeviceToDto(device);
        }

        public async Task<DeviceResponseDto?> UpdateDeviceAsync(Guid userId, Guid deviceId, DeviceRequestDto request)
        {
            var device = await context.Devices
                .Include(d => d.Organisation)
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.Organisation!.UserId == userId);

            if (device is null) return null;

            device.Name = request.Name;
            device.Type = request.Type;
            device.EnergyConsumption = request.EnergyConsumption;

            await context.SaveChangesAsync();

            return MapDeviceToDto(device);
        }

        public async Task<bool> DeleteDeviceAsync(Guid userId, Guid deviceId)
        {
            var device = await context.Devices
                .Include(d => d.Organisation)
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.Organisation!.UserId == userId);

            if (device is null) return false;

            context.Devices.Remove(device);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DeviceResponseDto>> GetDevicesByOrganisationIdAsync(Guid userId, Guid organisationId)
        {
            var organisation = await context.Organisations
                .Include(o => o.Devices)
                .FirstOrDefaultAsync(o => o.OrganisationId == organisationId && o.UserId == userId);

            if (organisation is null) return Enumerable.Empty<DeviceResponseDto>();

            return organisation.Devices.Select(d => new DeviceResponseDto
            {
                DeviceId = d.DeviceId,
                OrganisationId = d.OrganisationId,
                Name = d.Name,
                Type = d.Type,
                EnergyConsumption = d.EnergyConsumption
            });
        }
        private static DeviceResponseDto MapDeviceToDto(Device d) => new()
        {
            DeviceId = d.DeviceId,
            OrganisationId = d.OrganisationId,
            Name = d.Name,
            Type = d.Type,
            EnergyConsumption = d.EnergyConsumption
        };
    }
}