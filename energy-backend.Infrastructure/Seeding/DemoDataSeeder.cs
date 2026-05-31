// IDemoDataSeeder was moved from Core.Interfaces → Application.Interfaces
using energy_backend.Application.Interfaces;
using energy_backend.Infrastructure.Data;
namespace energy_backend.Infrastructure.Seeding;

public class DemoDataSeeder(EnergyDbContext context) : IDemoDataSeeder
{
    public async Task SeedHistoryForDeviceAsync(Guid deviceId)
    {
        await SeedData.SeedDeviceHistoryAsync(context, deviceId);
    }
}