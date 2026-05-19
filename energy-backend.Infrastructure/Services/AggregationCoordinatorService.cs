using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    /// <summary>
    /// Receives a raw EnergyReading (PowerWatts = instantaneous active power in W)
    /// and upserts minute / hour / day / month aggregate buckets.
    ///
    /// Power → Energy conversion for TotalEnergyKwh in each bucket:
    ///   kWh = W × window_seconds / 3,600,000
    ///
    /// AveragePowerWatts uses Welford's online algorithm for numerical stability:
    ///   newAvg = prevAvg + (newValue − prevAvg) / newCount
    /// </summary>
    public class AggregationCoordinatorService : IAggregationCoordinatorService
    {
        private readonly EnergyDbContext _context;
        private readonly ILogger<AggregationCoordinatorService> _logger;
        private readonly IRealTimeDataStreamService _stream;

        private const float ReadingIntervalSeconds = 5f;

        public AggregationCoordinatorService(
            EnergyDbContext context,
            ILogger<AggregationCoordinatorService> logger,
            IRealTimeDataStreamService stream)
        {
            _context = context;
            _logger = logger;
            _stream = stream;
        }

        /// <summary>Convenience: upsert + notify in one call.</summary>
        public async Task ProcessEnergyReadingAsync(EnergyReading reading)
        {
            await UpsertBucketsAsync(reading);
            await NotifyOrgAsync(reading.DeviceId, reading.Timestamp);
        }

        /// <summary>Pure DB upsert — no SignalR.</summary>
        public async Task UpsertBucketsAsync(EnergyReading reading)
        {
            // Resolve device + org if not already loaded
            if (reading.Device == null)
            {
                reading.Device = await _context.Devices
                    .Include(d => d.Organisation)
                    .FirstOrDefaultAsync(d => d.DeviceId == reading.DeviceId);

                if (reading.Device == null)
                {
                    _logger.LogWarning("Device {DeviceId} not found; skipping reading {ReadingId}",
                        reading.DeviceId, reading.EnergyReadingId);
                    return;
                }
            }

            var orgId = reading.Device.OrganisationId;
            if (orgId == Guid.Empty) return;

            var ts = reading.Timestamp;

            // PowerWatts is instantaneous active power (W) — exactly what the IoT
            // device reports. Convert to kWh for the 5-second window:
            //   kWh = W × 5s / 3,600,000
            // Example: 100 W to 100 × 5 / 3,600,000 = 0.0001389 kWh 
            float watts = reading.PowerWatts;
            float kwhThisReading = watts * ReadingIntervalSeconds / 3_600_000f;

            await UpsertMinuteAsync(orgId, reading.DeviceId, ts, watts, kwhThisReading);
            await UpsertHourAsync(orgId, reading.DeviceId, ts, watts, kwhThisReading);
            await UpsertDayAsync(orgId, reading.DeviceId, ts, watts, kwhThisReading);
            await UpsertMonthAsync(orgId, reading.DeviceId, ts, watts, kwhThisReading);

            await _context.SaveChangesAsync();
        }

        /// <summary>Fire one SignalR update for the org after all devices are upserted.</summary>
        public async Task NotifyOrgAsync(Guid deviceId, DateTime slotTimestamp)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null) return;

            var minuteBucket = new DateTime(
                slotTimestamp.Year, slotTimestamp.Month, slotTimestamp.Day,
                slotTimestamp.Hour, slotTimestamp.Minute, 0, DateTimeKind.Utc);

            // Pass any one device's row — the stream service re-queries the org sum internally
            var anyRow = await _context.AggregateMinuteEnergies
                .FirstOrDefaultAsync(a => a.OrgId == device.OrganisationId && a.Timestamp == minuteBucket);

            if (anyRow != null)
                await _stream.NotifyMinuteAggregateUpdated(device.OrganisationId, anyRow);
        }

        // Upsert helpers 

        private async Task UpsertMinuteAsync(Guid orgId, Guid deviceId,
            DateTime ts, float watts, float kwh)
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
                    TotalEnergyKwh = kwh,
                    AveragePowerWatts = watts,
                    MinPowerWatts = watts,
                    MaxPowerWatts = watts,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergyKwh += kwh;
                agg.DataPointsCount++;
                agg.AveragePowerWatts += (watts - agg.AveragePowerWatts) / agg.DataPointsCount;
                if (watts < agg.MinPowerWatts) agg.MinPowerWatts = watts;
                if (watts > agg.MaxPowerWatts) agg.MaxPowerWatts = watts;
            }
        }

        private async Task UpsertHourAsync(Guid orgId, Guid deviceId,
            DateTime ts, float watts, float kwh)
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
                    TotalEnergyKwh = kwh,
                    AveragePowerWatts = watts,
                    MinPowerWatts = watts,
                    MaxPowerWatts = watts,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergyKwh += kwh;
                agg.DataPointsCount++;
                agg.AveragePowerWatts += (watts - agg.AveragePowerWatts) / agg.DataPointsCount;
                if (watts < agg.MinPowerWatts) agg.MinPowerWatts = watts;
                if (watts > agg.MaxPowerWatts) agg.MaxPowerWatts = watts;
            }
        }

        private async Task UpsertDayAsync(Guid orgId, Guid deviceId,
            DateTime ts, float watts, float kwh)
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
                    TotalEnergyKwh = kwh,
                    AveragePowerWatts = watts,
                    MinPowerWatts = watts,
                    MaxPowerWatts = watts,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergyKwh += kwh;
                agg.DataPointsCount++;
                agg.AveragePowerWatts += (watts - agg.AveragePowerWatts) / agg.DataPointsCount;
                if (watts < agg.MinPowerWatts) agg.MinPowerWatts = watts;
                if (watts > agg.MaxPowerWatts) agg.MaxPowerWatts = watts;
            }
        }

        private async Task UpsertMonthAsync(Guid orgId, Guid deviceId,
            DateTime ts, float watts, float kwh)
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
                    TotalEnergyKwh = kwh,
                    AveragePowerWatts = watts,
                    MinPowerWatts = watts,
                    MaxPowerWatts = watts,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergyKwh += kwh;
                agg.DataPointsCount++;
                agg.AveragePowerWatts += (watts - agg.AveragePowerWatts) / agg.DataPointsCount;
                if (watts < agg.MinPowerWatts) agg.MinPowerWatts = watts;
                if (watts > agg.MaxPowerWatts) agg.MaxPowerWatts = watts;
            }
        }
    }
}