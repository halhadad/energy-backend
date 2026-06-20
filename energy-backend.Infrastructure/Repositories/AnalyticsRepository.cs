using energy_backend.Core.Projections;
using energy_backend.Core.Enums;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories;

public class AnalyticsRepository(EnergyDbContext context) : IAnalyticsRepository
{
    public async Task<List<AggregateMetricRow>> GetAggregateMetricsAsync(
        Guid orgId, DateTime start, DateTime end, TimeGranularity granularity)
    {
        IQueryable<IEnergyAggregate> query = granularity switch
        {
            TimeGranularity.Minute => context.AggregateMinuteEnergies.AsNoTracking(),
            TimeGranularity.Hour => context.AggregateHourEnergies.AsNoTracking(),
            TimeGranularity.Day => context.AggregateDayEnergies.AsNoTracking(),
            TimeGranularity.Month => context.AggregateMonthEnergies.AsNoTracking(),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity))
        };

        return await query
            .Where(a => a.OrgId == orgId && a.Timestamp >= start && a.Timestamp < end)
            .OrderBy(a => a.Timestamp)
            .Select(a => new AggregateMetricRow(
                a.Timestamp,
                a.TotalActiveEnergyKwh,
                a.AverageActivePowerWatts,
                a.MinActivePowerWatts,
                a.MaxActivePowerWatts,
                a.AverageVoltageVolts,
                a.AverageCurrentAmps,
                a.AveragePowerFactor,
                a.DataPointsCount))
            .ToListAsync();
    }

    public async Task<List<DeviceSnapshotRow>> GetDeviceSnapshotMetricsAsync(
        Guid orgId, DateTime start, DateTime end, TimeGranularity granularity)
    {
        IQueryable<IEnergyAggregate> query = granularity switch
        {
            TimeGranularity.Minute => context.AggregateMinuteEnergies.AsNoTracking(),
            TimeGranularity.Hour => context.AggregateHourEnergies.AsNoTracking(),
            TimeGranularity.Day => context.AggregateDayEnergies.AsNoTracking(),
            TimeGranularity.Month => context.AggregateMonthEnergies.AsNoTracking(),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity))
        };

        var rows = await query
            .Where(a => a.OrgId == orgId && a.Timestamp >= start && a.Timestamp < end)
            .Select(a => new { a.Timestamp, a.DeviceId, a.TotalActiveEnergyKwh })
            .ToListAsync();

        var deviceIds = rows.Where(r => r.DeviceId != null).Select(r => r.DeviceId).Distinct().ToList();
        var deviceNames = await context.Devices
            .Where(d => deviceIds.Contains(d.DeviceId))
            .Select(d => new { d.DeviceId, d.Name })
            .ToDictionaryAsync(d => d.DeviceId, d => d.Name);

        return rows
            .Where(r => r.DeviceId != null)
            .Select(r => new DeviceSnapshotRow(
                r.Timestamp,
                deviceNames.TryGetValue(r.DeviceId!.Value, out var name)
                    ? name
                    : r.DeviceId.Value.ToString(),
                r.TotalActiveEnergyKwh))
            .ToList();
    }

    public async Task<AggregateMetricRow?> GetLatestMetricSnapshotAsync(
        Guid orgId, TimeGranularity granularity)
    {
        IQueryable<IEnergyAggregate> query = granularity switch
        {
            TimeGranularity.Minute => context.AggregateMinuteEnergies.AsNoTracking(),
            TimeGranularity.Hour => context.AggregateHourEnergies.AsNoTracking(),
            TimeGranularity.Day => context.AggregateDayEnergies.AsNoTracking(),
            TimeGranularity.Month => context.AggregateMonthEnergies.AsNoTracking(),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity))
        };

        var latest = await query
            .Where(a => a.OrgId == orgId)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        if (latest is null) return null;

        return new AggregateMetricRow(
            latest.Timestamp,
            latest.TotalActiveEnergyKwh,
            latest.AverageActivePowerWatts,
            latest.MinActivePowerWatts,
            latest.MaxActivePowerWatts,
            latest.AverageVoltageVolts,
            latest.AverageCurrentAmps,
            latest.AveragePowerFactor,
            latest.DataPointsCount);
    }
}