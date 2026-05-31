// energy-backend.Core/Interfaces/ISettingRepository.cs
using energy_backend.Core.Entities;

namespace energy_backend.Core.Interfaces;

public interface ISettingRepository
{
    Task<Setting?> GetByUserIdAsync(Guid userId);
    Task<Setting> GetOrCreateAsync(Guid userId);
    Task AddAsync(Setting setting);
    Task SaveChangesAsync();
}
