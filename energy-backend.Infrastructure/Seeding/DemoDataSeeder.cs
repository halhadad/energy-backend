using energy_backend.Application.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Seeding;

// seeds a new device's history in the background so the create request can return right away
public class DemoDataSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoDataSeeder> logger) : IDemoDataSeeder
{
    public Task SeedHistoryForDeviceAsync(Guid deviceId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
                await SeedData.SeedDeviceHistoryAsync(context, deviceId);
                logger.LogInformation("Background seeding completed for DeviceId={DeviceId}", deviceId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background seeding failed for DeviceId={DeviceId}", deviceId);
            }
        });

        return Task.CompletedTask;
    }
}
