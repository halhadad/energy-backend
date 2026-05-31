using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories;

public class UserRepository(EnergyDbContext context) : IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username)
        => await context.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User?> GetByIdAsync(Guid userId)
        => await context.Users.FindAsync(userId);

    public async Task<bool> ExistsAsync(string username)
        => await context.Users.AnyAsync(u => u.Username == username);

    public async Task AddAsync(User user)
        => await context.Users.AddAsync(user);

    public async Task SaveChangesAsync()
        => await context.SaveChangesAsync();
}
