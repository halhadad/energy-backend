using energy_backend.Application.Models;

namespace energy_backend.Application.Interfaces;

public interface ISettingService
{
    Task<SettingResponseDto> GetOrCreateAsync(Guid userId);
    Task<SettingResponseDto> UpdateAsync(Guid userId, UpdateSettingDto dto);
}
