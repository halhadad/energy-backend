using energy_backend.Application.Models;

namespace energy_backend.Application.Interfaces;

public interface IDeviceService
{
    Task<IEnumerable<DeviceResponseDto>> GetDevicesAsync(Guid userId);
    Task<DeviceResponseDto?> GetDeviceByIdAsync(Guid userId, Guid deviceId);
    Task<DeviceResponseDto?> CreateDeviceAsync(Guid userId, DeviceRequestDto request);
    Task<DeviceResponseDto?> UpdateDeviceAsync(Guid userId, Guid deviceId, DeviceRequestDto request);
    Task<bool> DeleteDeviceAsync(Guid userId, Guid deviceId);
    Task<IEnumerable<DeviceResponseDto>> GetDevicesByOrganisationIdAsync(Guid userId, Guid organisationId);
}
