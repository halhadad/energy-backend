namespace energy_backend.Application.Interfaces;

public interface IDemoDataSeeder
{
    Task SeedHistoryForDeviceAsync(Guid deviceId);
}