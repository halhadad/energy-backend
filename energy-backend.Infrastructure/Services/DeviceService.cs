using energy_backend.Core.Interfaces;
using energy_backend.Data;
using energy_backend.Entities;
using energy_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Services
{
    public class DeviceService(IDeviceRepository repository, IOrganisationRepository orgRepo) : IDeviceService
    {
        public async Task<IEnumerable<DeviceResponseDto>> GetDevicesAsync(Guid userId)
        {
            var devices = await repository.GetAllByUserIdAsync(userId);

            return devices.Select(MapDeviceToDto);
        }

        public async Task<DeviceResponseDto?> GetDeviceByIdAsync(Guid userId, Guid deviceId)
        {
            var device = await repository.GetByIdAsync(userId, deviceId);
            return device is null ? null : MapDeviceToDto(device);
        }

        public async Task<DeviceResponseDto?> CreateDeviceAsync(Guid userId, DeviceRequestDto request)
        {
            var organisation = await orgRepo.GetByIdAsync(userId, request.OrganisationId);
            if (organisation is null) return null;

            var device = new Device
            {
                DeviceId = Guid.NewGuid(),
                OrganisationId = request.OrganisationId,
                Name = request.Name,
                Type = request.Type,
                EnergyConsumption = request.EnergyConsumption
            };

            await repository.AddAsync(device);
            await repository.SaveChangesAsync();

            return MapDeviceToDto(device);
        }

        public async Task<DeviceResponseDto?> UpdateDeviceAsync(Guid userId, Guid deviceId, DeviceRequestDto request)
        {
            var device = await repository.GetByIdAsync(userId, deviceId);
            if (device is null) return null;

            device.Name = request.Name;
            device.Type = request.Type;
            device.EnergyConsumption = request.EnergyConsumption;

            await repository.SaveChangesAsync();

            return MapDeviceToDto(device);
        }

        public async Task<bool> DeleteDeviceAsync(Guid userId, Guid deviceId)
        {
            var device = await repository.GetByIdAsync(userId, deviceId);
            if (device is null) return false;

            await repository.DeleteAsync(device);
            await repository.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<DeviceResponseDto>> GetDevicesByOrganisationIdAsync(Guid userId, Guid organisationId)
        {
            var devices = await repository.GetByOrganisationIdAsync(userId, organisationId);
            return devices.Select(MapDeviceToDto);
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