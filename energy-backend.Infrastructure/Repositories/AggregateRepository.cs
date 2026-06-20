using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace energy_backend.Infrastructure.Repositories;

public class AggregateRepository(EnergyDbContext context) : IAggregateRepository
{
    public async Task UpsertMinuteAsync(AggregateMinuteEnergy incoming, CancellationToken ct = default)
    {
        var existing = await context.AggregateMinuteEnergies
            .FirstOrDefaultAsync(a => a.OrgId == incoming.OrgId
                                   && a.DeviceId == incoming.DeviceId
                                   && a.Timestamp == incoming.Timestamp, ct);
        if (existing is null) context.AggregateMinuteEnergies.Add(incoming);
        else ApplyRunningAverage(existing, incoming);
    }
    public async Task UpsertHourAsync(AggregateHourEnergy incoming, CancellationToken ct = default)
    {
        var existing = await context.AggregateHourEnergies
            .FirstOrDefaultAsync(a => a.OrgId == incoming.OrgId
                                   && a.DeviceId == incoming.DeviceId
                                   && a.Timestamp == incoming.Timestamp, ct);
        if (existing is null) context.AggregateHourEnergies.Add(incoming);
        else ApplyRunningAverage(existing, incoming);
    }
    public async Task UpsertDayAsync(AggregateDayEnergy incoming, CancellationToken ct = default)
    {
        var existing = await context.AggregateDayEnergies
            .FirstOrDefaultAsync(a => a.OrgId == incoming.OrgId
                                   && a.DeviceId == incoming.DeviceId
                                   && a.Timestamp == incoming.Timestamp, ct);
        if (existing is null) context.AggregateDayEnergies.Add(incoming);
        else ApplyRunningAverage(existing, incoming);
    }
    public async Task UpsertMonthAsync(AggregateMonthEnergy incoming, CancellationToken ct = default)
    {
        var existing = await context.AggregateMonthEnergies
            .FirstOrDefaultAsync(a => a.OrgId == incoming.OrgId
                                   && a.DeviceId == incoming.DeviceId
                                   && a.Timestamp == incoming.Timestamp, ct);
        if (existing is null) context.AggregateMonthEnergies.Add(incoming);
        else ApplyRunningAverage(existing, incoming);
    }
    public async Task<AggregateMinuteEnergy?> GetLatestMinuteAsync(Guid orgId, DateTime timestamp, CancellationToken ct = default)
        => await context.AggregateMinuteEnergies.FirstOrDefaultAsync(a => a.OrgId == orgId && a.Timestamp == timestamp, ct);
    public async Task<AggregateMinuteEnergy?> GetMinuteAsync(Guid deviceId, DateTime timestamp, CancellationToken ct = default)
        => await context.AggregateMinuteEnergies.FirstOrDefaultAsync(a => a.DeviceId == deviceId && a.Timestamp == timestamp, ct);
    public async Task<AggregateHourEnergy?> GetLatestHourAsync(Guid orgId, DateTime timestamp, CancellationToken ct = default)
        => await context.AggregateHourEnergies.FirstOrDefaultAsync(a => a.OrgId == orgId && a.Timestamp == timestamp, ct);
    public async Task<AggregateDayEnergy?> GetLatestDayAsync(Guid orgId, DateTime timestamp, CancellationToken ct = default)
        => await context.AggregateDayEnergies.FirstOrDefaultAsync(a => a.OrgId == orgId && a.Timestamp == timestamp, ct);
    public async Task<AggregateMonthEnergy?> GetLatestMonthAsync(Guid orgId, DateTime timestamp, CancellationToken ct = default)
        => await context.AggregateMonthEnergies.FirstOrDefaultAsync(a => a.OrgId == orgId && a.Timestamp == timestamp, ct);
    public async Task<List<AggregateMinuteEnergy>> GetMinutesBeforeAsync(DateTime from, DateTime boundary, CancellationToken ct = default)
        => await context.AggregateMinuteEnergies
            .Where(m => m.Timestamp >= from && m.Timestamp < boundary)
            .ToListAsync(ct);
    public async Task<List<AggregateHourEnergy>> GetHoursBeforeAsync(DateTime boundary, CancellationToken ct = default)
        => await context.AggregateHourEnergies.Where(h => h.Timestamp < boundary).ToListAsync(ct);
    public async Task<List<AggregateDayEnergy>> GetDaysBeforeAsync(DateTime boundary, CancellationToken ct = default)
        => await context.AggregateDayEnergies.Where(d => d.Timestamp < boundary).ToListAsync(ct);
    public async Task<bool> HourExistsAsync(Guid orgId, Guid? deviceId, DateTime hourSlot, CancellationToken ct = default)
        => await context.AggregateHourEnergies.AnyAsync(h => h.OrgId == orgId && h.DeviceId == deviceId && h.Timestamp == hourSlot, ct);
    public async Task<bool> DayExistsAsync(Guid orgId, Guid? deviceId, DateTime daySlot, CancellationToken ct = default)
        => await context.AggregateDayEnergies.AnyAsync(d => d.OrgId == orgId && d.DeviceId == deviceId && d.Timestamp == daySlot, ct);
    public async Task<bool> MonthExistsAsync(Guid orgId, Guid? deviceId, DateTime monthSlot, CancellationToken ct = default)
        => await context.AggregateMonthEnergies.AnyAsync(m => m.OrgId == orgId && m.DeviceId == deviceId && m.Timestamp == monthSlot, ct);
    public async Task AddHourAsync(AggregateHourEnergy aggregate, CancellationToken ct = default)
        => await context.AggregateHourEnergies.AddAsync(aggregate, ct);
    public async Task AddDayAsync(AggregateDayEnergy aggregate, CancellationToken ct = default)
        => await context.AggregateDayEnergies.AddAsync(aggregate, ct);
    public async Task AddMonthAsync(AggregateMonthEnergy aggregate, CancellationToken ct = default)
        => await context.AggregateMonthEnergies.AddAsync(aggregate, ct);
    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
    private static void ApplyRunningAverage(IEnergyAggregate existing, IEnergyAggregate incoming)
    {
        var oldCount = existing.DataPointsCount;
        var incomingCount = incoming.DataPointsCount;
        var newCount = oldCount + incomingCount;
        if (newCount <= 0) return;
        existing.TotalActiveEnergyKwh += incoming.TotalActiveEnergyKwh;
        existing.AverageActivePowerWatts =
            ((existing.AverageActivePowerWatts * oldCount) + (incoming.AverageActivePowerWatts * incomingCount)) / newCount;
        existing.AverageVoltageVolts =
            ((existing.AverageVoltageVolts * oldCount) + (incoming.AverageVoltageVolts * incomingCount)) / newCount;
        existing.AverageCurrentAmps =
            ((existing.AverageCurrentAmps * oldCount) + (incoming.AverageCurrentAmps * incomingCount)) / newCount;
        existing.AveragePowerFactor =
            ((existing.AveragePowerFactor * oldCount) + (incoming.AveragePowerFactor * incomingCount)) / newCount;
        existing.DataPointsCount = newCount;
        if (incoming.MinActivePowerWatts < existing.MinActivePowerWatts) existing.MinActivePowerWatts = incoming.MinActivePowerWatts;
        if (incoming.MaxActivePowerWatts > existing.MaxActivePowerWatts) existing.MaxActivePowerWatts = incoming.MaxActivePowerWatts;
    }
}