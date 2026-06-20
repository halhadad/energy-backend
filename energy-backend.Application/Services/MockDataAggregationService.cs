using energy_backend.Application.Interfaces;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace energy_backend.Application.Services;

public class MockDataAggregationService(
    IAggregateRepository aggregates,
    IDeviceRepository devices,
    IRealTimeDataStreamService stream,
    ILogger<MockDataAggregationService> logger) : IMockDataAggregationService
{
    public async Task UpsertBucketsAsync(EnergyReading reading)
    {
        var device = reading.Device ?? await devices.GetByDeviceIdAsync(reading.DeviceId);
        if (device is null)
        {
            logger.LogWarning("Device {DeviceId} not found; skipping bucket upsert.", reading.DeviceId);
            return;
        }

        var orgId = device.OrganisationId;
        if (orgId == Guid.Empty) return;

        // Use the reading's own interval energy; never recompute from a hardcoded interval.
        var kwh = reading.ActiveEnergyKwh;

        // This service maintains the minute tier only. Hour/Day/Month are owned by
        // HistoricalDownsamplingWorker, which promotes completed minute buckets up the chain.
        // A second writer on the higher tiers would create divergent rows for the same slot.
        await aggregates.UpsertMinuteAsync(
            Build<AggregateMinuteEnergy>(orgId, reading.DeviceId, Bucket(reading.Timestamp, BucketSize.Minute), reading, kwh));

        await aggregates.SaveChangesAsync();
    }

    public async Task NotifyOrgAsync(Guid orgId, IEnumerable<Guid> deviceIds, DateTime slotTimestamp)
    {
        if (orgId == Guid.Empty) return;

        var minuteBucket = Bucket(slotTimestamp, BucketSize.Minute);

        // Buckets are stored per device, and the chart group is per org, so push one
        // minute-aggregate update per device for this slot. (Hour/Day/Month are promoted
        // asynchronously by HistoricalDownsamplingWorker and refetched by the client.)
        foreach (var deviceId in deviceIds.Distinct())
        {
            var minuteRow = await aggregates.GetMinuteAsync(deviceId, minuteBucket);
            if (minuteRow is not null)
                await stream.NotifyMinuteAggregateUpdated(orgId, minuteRow);
        }

        // Signal the historical page to refetch once per minute (when a minute bucket
        // completes), not on every 5-second tick.
        if (slotTimestamp.Second == 0)
            await stream.NotifyHistoricalUpdatedAsync(orgId);
    }

    private static T Build<T>(Guid orgId, Guid deviceId, DateTime ts, EnergyReading r, double kwh)
        where T : IEnergyAggregate, new() => new()
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            DeviceId = deviceId,
            Timestamp = ts,
            TotalActiveEnergyKwh = kwh,
            AverageActivePowerWatts = r.ActivePowerWatts,
            MinActivePowerWatts = r.ActivePowerWatts,
            MaxActivePowerWatts = r.ActivePowerWatts,
            AverageVoltageVolts = r.VoltageVolts,
            AverageCurrentAmps = r.CurrentAmps,
            AveragePowerFactor = r.PowerFactor,
            DataPointsCount = 1
        };

    private enum BucketSize { Minute, Hour, Day, Month }

    private static DateTime Bucket(DateTime ts, BucketSize size) => size switch
    {
        BucketSize.Minute => new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, 0, DateTimeKind.Utc),
        BucketSize.Hour => new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, 0, 0, DateTimeKind.Utc),
        BucketSize.Day => new DateTime(ts.Year, ts.Month, ts.Day, 0, 0, 0, DateTimeKind.Utc),
        BucketSize.Month => new DateTime(ts.Year, ts.Month, 1, 0, 0, 0, DateTimeKind.Utc),
        _ => throw new ArgumentOutOfRangeException(nameof(size))
    };
}