using energy_backend.Core.Entities;

namespace energy_backend.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(Guid userId);
    Task<bool> ExistsAsync(string username);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
