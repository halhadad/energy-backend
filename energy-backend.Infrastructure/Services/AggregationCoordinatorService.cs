using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    /// <summary>
    /// Receives a raw EnergyReading where EnergyValue = instantaneous Watts,
    /// and upserts minute/hour/day/month aggregate buckets.
    ///
    /// Watts to kWh conversion for bucket TotalEnergy:
    ///   kWh = W × (5 seconds / 3600 seconds per hour) / 1000 W per kW
    ///       = W × 5 / 3,600,000
    ///
    /// AverageWatts in each bucket is the rolling mean of all readings in that
    /// window — this is the value shown on the "Current Consumption" card.
    /// </summary>
    public class AggregationCoordinatorService : IAggregationCoordinatorService
    {
        private readonly EnergyDbContext _context;
        private readonly ILogger<AggregationCoordinatorService> _logger;
        private readonly IRealTimeDataStreamService _realTimeStreamService;

        private const float SecondsPerReading = 5f;

        public AggregationCoordinatorService(
            EnergyDbContext context,
            ILogger<AggregationCoordinatorService> logger,
            IRealTimeDataStreamService realTimeStreamService)
        {
            _context = context;
            _logger = logger;
            _realTimeStreamService = realTimeStreamService;
        }

        /// <summary>
        /// Convenience: upsert buckets + fire SignalR in one call.
        /// Fine for single-device scenarios. For multi-device batches, prefer
        /// UpsertBucketsAsync in a loop then NotifyOrgAsync once.
        /// </summary>
        public async Task ProcessEnergyReadingAsync(EnergyReading energyReading)
        {
            await UpsertBucketsAsync(energyReading);
            await NotifyOrgAsync(energyReading.DeviceId, energyReading.Timestamp);
        }

        /// <summary>
        /// Upserts all aggregate buckets for one reading. No SignalR fired here.
        /// </summary>
        public async Task UpsertBucketsAsync(EnergyReading energyReading)
        {
            if (energyReading.Device == null)
            {
                energyReading.Device = await _context.Devices
                    .Include(d => d.Organisation)
                    .FirstOrDefaultAsync(d => d.DeviceId == energyReading.DeviceId);

                if (energyReading.Device == null)
                {
                    _logger.LogWarning("Device not found for reading {Id}", energyReading.EnergyReadingId);
                    return;
                }
            }

            var orgId = energyReading.Device.OrganisationId;
            if (orgId == Guid.Empty) return;

            var ts = energyReading.Timestamp;

            // EnergyValue is now WATTS (instantaneous active power).
            float instantWatts = energyReading.EnergyValue;

            // Convert to kWh for the 5-second window:
            //   kWh = W × (5s / 3600s/h) / 1000
            //       = W × 5 / 3,600,000
            // Example: 20 W to 20 × 5 / 3,600,000 = 0.0000278 kWh 
            float kwhValue = instantWatts * SecondsPerReading / 3_600_000f;

            await UpsertMinuteAsync(orgId, energyReading.DeviceId, ts, instantWatts, kwhValue);
            await UpsertHourAsync(orgId, energyReading.DeviceId, ts, instantWatts, kwhValue);
            await UpsertDayAsync(orgId, energyReading.DeviceId, ts, instantWatts, kwhValue);
            await UpsertMonthAsync(orgId, energyReading.DeviceId, ts, instantWatts, kwhValue);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Queries the org-level sum of AverageWatts for the latest minute bucket
        /// and fires one SignalR notification. Call once after all devices in a slot
        /// have been processed via UpsertBucketsAsync.
        /// </summary>
        public async Task NotifyOrgAsync(Guid deviceId, DateTime ts)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null) return;
            var orgId = device.OrganisationId;

            var minuteBucket = new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, 0, DateTimeKind.Utc);

            // Grab any device's minute row for this org/bucket to pass into the
            // notifier (the notifier re-queries the org-level sum internally).
            var minute = await _context.AggregateMinuteEnergies
                .FirstOrDefaultAsync(a => a.OrgId == orgId && a.Timestamp == minuteBucket);

            if (minute != null)
                await _realTimeStreamService.NotifyMinuteAggregateUpdated(orgId, minute);
        }

        // Upsert helpers 
        // Each helper maintains:
        //   TotalEnergy   — cumulative kWh in the bucket window
        //   AverageWatts  — rolling mean of instantaneous watt readings
        //   MinWatts      — lowest reading seen in the window
        //   MaxWatts      — highest reading seen in the window
        //   DataPointsCount — number of 5-second readings in the window

        private async Task UpsertMinuteAsync(Guid orgId, Guid deviceId, DateTime ts,
            float instantWatts, float kwhValue)
        {
            var bucket = new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, 0, DateTimeKind.Utc);
            var agg = await _context.AggregateMinuteEnergies
                .FirstOrDefaultAsync(a => a.OrgId == orgId && a.DeviceId == deviceId && a.Timestamp == bucket);

            if (agg == null)
            {
                _context.AggregateMinuteEnergies.Add(new AggregateMinuteEnergy
                {
                    OrgId = orgId,
                    DeviceId = deviceId,
                    Timestamp = bucket,
                    TotalEnergy = kwhValue,
                    AverageWatts = instantWatts,
                    MinWatts = instantWatts,
                    MaxWatts = instantWatts,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergy += kwhValue;
                agg.DataPointsCount++;
                // Rolling mean: newAvg = prevAvg + (new - prevAvg) / n
                agg.AverageWatts += (instantWatts - agg.AverageWatts) / agg.DataPointsCount;
                if (instantWatts < agg.MinWatts) agg.MinWatts = instantWatts;
                if (instantWatts > agg.MaxWatts) agg.MaxWatts = instantWatts;
            }
        }

        private async Task UpsertHourAsync(Guid orgId, Guid deviceId, DateTime ts,
            float instantWatts, float kwhValue)
        {
            var bucket = new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, 0, 0, DateTimeKind.Utc);
            var agg = await _context.AggregateHourEnergies
                .FirstOrDefaultAsync(a => a.OrgId == orgId && a.DeviceId == deviceId && a.Timestamp == bucket);

            if (agg == null)
            {
                _context.AggregateHourEnergies.Add(new AggregateHourEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = orgId,
                    DeviceId = deviceId,
                    Timestamp = bucket,
                    TotalEnergy = kwhValue,
                    AverageWatts = instantWatts,
                    MinWatts = instantWatts,
                    MaxWatts = instantWatts,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergy += kwhValue;
                agg.DataPointsCount++;
                agg.AverageWatts += (instantWatts - agg.AverageWatts) / agg.DataPointsCount;
                if (instantWatts < agg.MinWatts) agg.MinWatts = instantWatts;
                if (instantWatts > agg.MaxWatts) agg.MaxWatts = instantWatts;
            }
        }

        private async Task UpsertDayAsync(Guid orgId, Guid deviceId, DateTime ts,
            float instantWatts, float kwhValue)
        {
            var bucket = new DateTime(ts.Year, ts.Month, ts.Day, 0, 0, 0, DateTimeKind.Utc);
            var agg = await _context.AggregateDayEnergies
                .FirstOrDefaultAsync(a => a.OrgId == orgId && a.DeviceId == deviceId && a.Timestamp == bucket);

            if (agg == null)
            {
                _context.AggregateDayEnergies.Add(new AggregateDayEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = orgId,
                    DeviceId = deviceId,
                    Timestamp = bucket,
                    TotalEnergy = kwhValue,
                    AverageWatts = instantWatts,
                    MinWatts = instantWatts,
                    MaxWatts = instantWatts,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergy += kwhValue;
                agg.DataPointsCount++;
                agg.AverageWatts += (instantWatts - agg.AverageWatts) / agg.DataPointsCount;
                if (instantWatts < agg.MinWatts) agg.MinWatts = instantWatts;
                if (instantWatts > agg.MaxWatts) agg.MaxWatts = instantWatts;
            }
        }

        private async Task UpsertMonthAsync(Guid orgId, Guid deviceId, DateTime ts,
            float instantWatts, float kwhValue)
        {
            var bucket = new DateTime(ts.Year, ts.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var agg = await _context.AggregateMonthEnergies
                .FirstOrDefaultAsync(a => a.OrgId == orgId && a.DeviceId == deviceId && a.Timestamp == bucket);

            if (agg == null)
            {
                _context.AggregateMonthEnergies.Add(new AggregateMonthEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = orgId,
                    DeviceId = deviceId,
                    Timestamp = bucket,
                    TotalEnergy = kwhValue,
                    AverageWatts = instantWatts,
                    MinWatts = instantWatts,
                    MaxWatts = instantWatts,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergy += kwhValue;
                agg.DataPointsCount++;
                agg.AverageWatts += (instantWatts - agg.AverageWatts) / agg.DataPointsCount;
                if (instantWatts < agg.MinWatts) agg.MinWatts = instantWatts;
                if (instantWatts > agg.MaxWatts) agg.MaxWatts = instantWatts;
            }
        }
    }
}