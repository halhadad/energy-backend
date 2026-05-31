using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.BackgroundWorkers;

public class RecurrentDataRollupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurrentDataRollupWorker> _logger;
    private static readonly TimeSpan LoopInterval = TimeSpan.FromMinutes(1);

    public RecurrentDataRollupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurrentDataRollupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recurrent Data Rollup Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var rollupRepo = scope.ServiceProvider.GetRequiredService<IEnergyRollupRepository>();
                var aggregateRepo = scope.ServiceProvider.GetRequiredService<IAggregateRepository>();

                var since = DateTime.UtcNow.AddMinutes(-5);
                var projections = await rollupRepo.GetRawMinuteRollupsAsync(since, stoppingToken);

                foreach (var item in projections)
                {
                    await aggregateRepo.UpsertMinuteAsync(new AggregateMinuteEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId = item.OrgId,
                        DeviceId = item.DeviceId,
                        Timestamp = item.MinuteSlot,
                        TotalActiveEnergyKwh = item.TotalEnergyKwh,
                        AverageActivePowerWatts = item.AvgWatts,
                        MinActivePowerWatts = item.MinWatts,
                        MaxActivePowerWatts = item.MaxWatts,
                        AverageVoltageVolts = item.AvgVoltage,
                        AverageCurrentAmps = item.AvgCurrent,
                        AveragePowerFactor = item.AvgPowerFactor,
                        DataPointsCount = item.Count
                    }, stoppingToken);
                }

                if (projections.Count > 0)
                {
                    await aggregateRepo.SaveChangesAsync(stoppingToken);
                    _logger.LogDebug("Rollup worker processed {Count} minute intervals.", projections.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in recurrent rollup worker.");
            }

            await Task.Delay(LoopInterval, stoppingToken);
        }
    }
}
