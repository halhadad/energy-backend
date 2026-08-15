using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Interfaces;

namespace energy_backend.Application.Services;

public class SettingService(ISettingRepository settingRepo, IUserRepository userRepo) : ISettingService
{
    public async Task<SettingResponseDto> GetOrCreateAsync(Guid userId)
    {
        var setting = await settingRepo.GetOrCreateAsync(userId);
        var user = await userRepo.GetByIdAsync(userId);
        return new SettingResponseDto
        {
            Email = user?.Email ?? string.Empty,
            RequireEmail = setting.RequireEmail
        };
    }

    public async Task<SettingResponseDto> UpdateAsync(Guid userId, UpdateSettingDto dto)
    {
        var setting = await settingRepo.GetOrCreateAsync(userId);
        var user = await userRepo.GetByIdAsync(userId);

        if (dto.RequireEmail.HasValue)
            setting.RequireEmail = dto.RequireEmail.Value;

        if (dto.Email is not null && user is not null)
        {
            var trimmed = dto.Email.Trim();
            if (trimmed.Length > 0 && !trimmed.Contains('@'))
                throw new ArgumentException("Please enter a valid email address.");
            user.Email = trimmed;
        }

        // settingRepo and userRepo share the same scoped DbContext, so one save persists both.
        await settingRepo.SaveChangesAsync();

        return new SettingResponseDto
        {
            Email = user?.Email ?? string.Empty,
            RequireEmail = setting.RequireEmail
        };
    }
}
