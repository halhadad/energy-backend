using energy_backend.Application.Configuration;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Interfaces;
using energy_backend.Core.Services;
using Microsoft.Extensions.Logging;

namespace energy_backend.Application.Services;

public class LiveDataOrchestratorService(
    IEnergyReadingRepository readingRepo,
    IEnergyRateRepository rateRepo,
    EnergySettings settings,
    ILogger<LiveDataOrchestratorService> logger) : ILiveDataOrchestratorService
{
    public async Task<LiveSnapshotDto> GetLiveSnapshotAsync(Guid organisationId)
    {
        var now = DateTime.UtcNow;
        var windowStart = DateTime.SpecifyKind(now.AddMinutes(-settings.LiveWindowMinutes), DateTimeKind.Utc);

        logger.LogDebug(
            "LiveDataOrchestrator: snapshot for OrgId={OrgId} [{Start:O} → {End:O}]",
            organisationId, windowStart, now);

        var readings = await readingRepo.GetReadingsSinceAsync(organisationId, windowStart);
        var currentRate = await rateRepo.GetCurrentRateAsync(organisationId);

        // Group by the 5-second timestamp slot, aggregate all devices per slot
        var readingDtos = readings
            .GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var ts = DateTime.SpecifyKind(g.Key, DateTimeKind.Utc);
                var totalWatts = g.Sum(r => r.ActivePowerWatts);

                return new LiveReadingDto
                {
                    Timestamp = ts,
                    ActivePowerWatts = totalWatts,
                    VoltageVolts = g.Average(r => r.VoltageVolts),
                    CurrentAmps = g.Sum(r => r.CurrentAmps),
                    PowerFactor = g.Average(r => r.PowerFactor),
                    CostRatePerHour = (decimal)(totalWatts / 1000.0) * currentRate
                };
            })
            .ToList();

        logger.LogDebug(
            "LiveDataOrchestrator: returning {Count} ticks for OrgId={OrgId}",
            readingDtos.Count, organisationId);

        return new LiveSnapshotDto
        {
            OrgId = organisationId,
            SnapshotAt = now,
            CurrentRatePerKwh = currentRate,
            Readings = readingDtos
        };
    }
}