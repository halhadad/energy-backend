using energy_backend.Application.Interfaces;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Core.Services;
using Microsoft.Extensions.Logging;
namespace energy_backend.Application.Services;

public class MockDataAggregationService : IMockDataAggregationService
{
    private readonly IAggregateRepository _aggregates;
    private readonly IDeviceRepository _devices;
    private readonly IRealTimeDataStreamService _stream;
    private readonly ILogger<MockDataAggregationService> _logger;
    private const float ReadingIntervalSeconds = 5f;
    public MockDataAggregationService(
        IAggregateRepository aggregates,
        IDeviceRepository devices,
        IRealTimeDataStreamService stream,
        ILogger<MockDataAggregationService> logger)
    {
        _aggregates = aggregates;
        _devices = devices;
        _stream = stream;
        _logger = logger;
    }
    public async Task UpsertBucketsAsync(EnergyReading reading)
    {
        var device = reading.Device ?? await _devices.GetByDeviceIdAsync(reading.DeviceId);
        if (device is null)
        {
            _logger.LogWarning("Device {DeviceId} not found; skipping reading.", reading.DeviceId);
            return;
        }
        var orgId = device.OrganisationId;
        if (orgId == Guid.Empty) return;
        var watts = reading.ActivePowerWatts;
        var volts = reading.VoltageVolts;
        var amps = reading.CurrentAmps;
        var pf = reading.PowerFactor;
        var kwh = EnergyCalculator.CalculateKwhFromWatts(watts, ReadingIntervalSeconds);
        await _aggregates.UpsertMinuteAsync(Build<AggregateMinuteEnergy>(orgId, reading.DeviceId, Bucket(reading.Timestamp, BucketSize.Minute), watts, volts, amps, pf, kwh));
        await _aggregates.UpsertHourAsync(Build<AggregateHourEnergy>(orgId, reading.DeviceId, Bucket(reading.Timestamp, BucketSize.Hour), watts, volts, amps, pf, kwh));
        await _aggregates.UpsertDayAsync(Build<AggregateDayEnergy>(orgId, reading.DeviceId, Bucket(reading.Timestamp, BucketSize.Day), watts, volts, amps, pf, kwh));
        await _aggregates.UpsertMonthAsync(Build<AggregateMonthEnergy>(orgId, reading.DeviceId, Bucket(reading.Timestamp, BucketSize.Month), watts, volts, amps, pf, kwh));
        await _aggregates.SaveChangesAsync();
    }
    public async Task NotifyOrgAsync(Guid deviceId, DateTime slotTimestamp)
    {
        var device = await _devices.GetByDeviceIdAsync(deviceId);
        if (device is null) return;
        var orgId = device.OrganisationId;
        var minuteBucket = Bucket(slotTimestamp, BucketSize.Minute);
        var hourBucket = Bucket(slotTimestamp, BucketSize.Hour);
        var dayBucket = Bucket(slotTimestamp, BucketSize.Day);
        var monthBucket = Bucket(slotTimestamp, BucketSize.Month);
        var minuteRow = await _aggregates.GetLatestMinuteAsync(orgId, minuteBucket);
        if (minuteRow is not null)
            await _stream.NotifyMinuteAggregateUpdated(orgId, minuteRow);
        if (slotTimestamp.Minute == 0)
        {
            var hourRow = await _aggregates.GetLatestHourAsync(orgId, hourBucket);
            if (hourRow is not null)
                await _stream.NotifyHourAggregateUpdated(orgId, hourRow);
        }
        if (slotTimestamp is { Hour: 0, Minute: 0 })
        {
            var dayRow = await _aggregates.GetLatestDayAsync(orgId, dayBucket);
            if (dayRow is not null)
                await _stream.NotifyDayAggregateUpdated(orgId, dayRow);
            var monthRow = await _aggregates.GetLatestMonthAsync(orgId, monthBucket);
            if (monthRow is not null)
                await _stream.NotifyMonthAggregateUpdated(orgId, monthRow);
        }
    }
    private static T Build<T>(Guid orgId, Guid deviceId, DateTime ts,
        float watts, float volts, float amps, float pf, float kwh)
        where T : IEnergyAggregate, new() => new()
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            DeviceId = deviceId,
            Timestamp = ts,
            TotalActiveEnergyKwh = kwh,
            AverageActivePowerWatts = watts,
            MinActivePowerWatts = watts,
            MaxActivePowerWatts = watts,
            AverageVoltageVolts = volts,
            AverageCurrentAmps = amps,
            AveragePowerFactor = pf,
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