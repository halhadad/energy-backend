using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using energy_backend.Application.Models.Projections;
using energy_backend.Application.Interfaces;

namespace energy_backend.Infrastructure.Repositories;

public class EnergyRollupRepository(EnergyDbContext context) : IEnergyRollupRepository
{
    public async Task<List<MinuteRollupProjection>> GetRawMinuteRollupsAsync(
        DateTime since,
        CancellationToken ct = default)
    {
        return await context.EnergyReadings
            .Where(r => r.Timestamp >= since)
            .GroupBy(r => new
            {
                r.OrgId,
                r.DeviceId,
                MinuteSlot = new DateTime(r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day,
                    r.Timestamp.Hour, r.Timestamp.Minute, 0, DateTimeKind.Utc)
            })
            .Select(g => new MinuteRollupProjection(
                g.Key.OrgId,
                g.Key.DeviceId,
                g.Key.MinuteSlot,
                g.Max(x => x.ActiveEnergyKwh) - g.Min(x => x.ActiveEnergyKwh),
                g.Average(x => x.ActivePowerWatts),
                g.Min(x => x.ActivePowerWatts),
                g.Max(x => x.ActivePowerWatts),
                g.Average(x => x.VoltageVolts),
                g.Average(x => x.CurrentAmps),
                g.Average(x => x.PowerFactor),
                g.Count()))
            .ToListAsync(ct);
    }
}
