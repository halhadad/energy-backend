using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories;

public class AlertEvaluationRepository(EnergyDbContext context) : IAlertEvaluationRepository
{
    public async Task<(DateTime? LatestTimestamp, double TotalWatts)> GetLatestMinutePowerSumAsync(
        Guid orgId,
        CancellationToken ct = default)
    {
        // use raw readings so the threshold matches the live dashboard, not a dampened minute average
        var cutoff = DateTime.UtcNow.AddSeconds(-30);

        var recent = await context.EnergyReadings
            .Where(r => r.OrgId == orgId && r.Timestamp >= cutoff)
            .ToListAsync(ct);

        if (recent.Count == 0) return (null, 0d);

        // latest reading per device, then sum their power
        var latestPerDevice = recent
            .GroupBy(r => r.DeviceId)
            .Select(g => g.MaxBy(r => r.Timestamp)!);

        var total = latestPerDevice.Sum(r => r.ActivePowerWatts);
        var latestTs = recent.Max(r => r.Timestamp);
        return (latestTs, total);
    }
}
