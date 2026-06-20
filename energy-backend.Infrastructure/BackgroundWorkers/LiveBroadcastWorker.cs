using energy_backend.Application.Interfaces;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Configuration;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace energy_backend.Infrastructure.BackgroundWorkers;

// broadcasts each org's latest live tick on its own timer, separate from the simulator cadence
public class LiveBroadcastWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EnergyWorkerOptions _options;
    private readonly ILogger<LiveBroadcastWorker> _logger;

    public LiveBroadcastWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EnergyWorkerOptions> options,
        ILogger<LiveBroadcastWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var interval = TimeSpan.FromSeconds(_options.BroadcastIntervalSeconds);

        _logger.LogInformation(
            "LiveBroadcastWorker started (broadcast interval: {Interval}s, look-back window: {Window}s).",
            _options.BroadcastIntervalSeconds,
            _options.BroadcastIntervalSeconds * 3);

        while (!ct.IsCancellationRequested)
        {
            // Wait first so the first broadcast goes out after data exists.
            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { break; }

            try
            {
                await BroadcastAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "LiveBroadcastWorker: error in broadcast loop.");
            }
        }
    }

    private async Task BroadcastAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
        var stream = scope.ServiceProvider.GetRequiredService<IRealTimeDataStreamService>();
        var rateRepo = scope.ServiceProvider.GetRequiredService<IEnergyRateRepository>();

        // look back 3 intervals so a device that reported just before the last boundary still counts
        var windowSeconds = _options.BroadcastIntervalSeconds * 3;
        var since = DateTime.SpecifyKind(
            DateTime.UtcNow.AddSeconds(-windowSeconds), DateTimeKind.Utc);

        // newest first, so the GroupBy below keeps the most recent reading per device
        var readings = await db.EnergyReadings
            .Where(r => r.Timestamp >= since)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        if (readings.Count == 0)
        {
            _logger.LogDebug("LiveBroadcastWorker: no readings in window [{Since:O}, now]; skipping tick.", since);
            return;
        }

        // Latest reading per device (newest wins).
        var latestPerDevice = readings
            .GroupBy(r => r.DeviceId)
            .Select(g => g.First())
            .ToList();

        // Broadcast one live tick per organisation.
        foreach (var orgGroup in latestPerDevice.GroupBy(r => r.OrgId))
        {
            var orgId = orgGroup.Key;
            var orgReadings = orgGroup.ToList();

            var rate = await rateRepo.GetCurrentRateAsync(orgId, ct);
            await stream.NotifyLiveTickAsync(orgId, orgReadings, rate);

            _logger.LogDebug(
                "LiveBroadcastWorker: broadcast {Count} device(s) for OrgId={OrgId}.",
                orgReadings.Count, orgId);
        }
    }
}