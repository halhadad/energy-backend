using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories;

public class SettingRepository(EnergyDbContext context) : ISettingRepository
{
    public async Task<Setting?> GetByUserIdAsync(Guid userId)
        => await context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task<Setting> GetOrCreateAsync(Guid userId)
    {
        var setting = await context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (setting is not null) return setting;

        setting = new Setting { UserId = userId };
        context.Settings.Add(setting);
        await context.SaveChangesAsync();
        return setting;
    }

    public async Task AddAsync(Setting setting)
        => await context.Settings.AddAsync(setting);

    public async Task SaveChangesAsync()
        => await context.SaveChangesAsync();
}
