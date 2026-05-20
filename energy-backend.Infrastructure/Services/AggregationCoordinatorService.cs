using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    /// <summary>
    /// Receives a raw EnergyReading and upserts minute/hour/day/month aggregate buckets.
    ///
    /// All running averages use Welford's online algorithm for numerical stability.
    /// Power-to-energy: kWh = W × 5s / 3,600,000
    /// Current is stored directly from the reading (I = P / V / PF already computed by simulator).
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

        public async Task ProcessEnergyReadingAsync(EnergyReading reading)
        {
            await UpsertBucketsAsync(reading);
            await NotifyOrgAsync(reading.DeviceId, reading.Timestamp);
        }

        public async Task UpsertBucketsAsync(EnergyReading reading)
        {
            if (reading.Device == null)
            {
                reading.Device = await _context.Devices
                    .Include(d => d.Organisation)
                    .FirstOrDefaultAsync(d => d.DeviceId == reading.DeviceId);

                if (reading.Device == null)
                {
                    _logger.LogWarning("Device {DeviceId} not found; skipping reading.", reading.DeviceId);
                    return;
                }
            }

            var orgId = reading.Device.OrganisationId;
            if (orgId == Guid.Empty) return;

            var ts = reading.Timestamp;
            float watts = reading.ActivePowerWatts;
            float volts = reading.VoltageVolts;
            float amps = reading.CurrentAmps;
            float pf = reading.PowerFactor;
            float kwhThisReading = watts * ReadingIntervalSeconds / 3_600_000f;

            await UpsertMinuteAsync(orgId, reading.DeviceId, ts, watts, volts, amps, pf, kwhThisReading);
            await UpsertHourAsync(orgId, reading.DeviceId, ts, watts, volts, amps, pf, kwhThisReading);
            await UpsertDayAsync(orgId, reading.DeviceId, ts, watts, volts, amps, pf, kwhThisReading);
            await UpsertMonthAsync(orgId, reading.DeviceId, ts, watts, volts, amps, pf, kwhThisReading);

            await _context.SaveChangesAsync();
        }

        public async Task NotifyOrgAsync(Guid deviceId, DateTime slotTimestamp)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null) return;

            var minuteBucket = new DateTime(
                slotTimestamp.Year, slotTimestamp.Month, slotTimestamp.Day,
                slotTimestamp.Hour, slotTimestamp.Minute, 0, DateTimeKind.Utc);

            var anyRow = await _context.AggregateMinuteEnergies
                .FirstOrDefaultAsync(a => a.OrgId == device.OrganisationId && a.Timestamp == minuteBucket);

            if (anyRow != null)
                await _stream.NotifyMinuteAggregateUpdated(device.OrganisationId, anyRow);
        }

        // ── Upsert helpers ────────────────────────────────────────────────────

        private async Task UpsertMinuteAsync(Guid orgId, Guid deviceId, DateTime ts,
            float watts, float volts, float amps, float pf, float kwh)
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
                    AverageActivePowerWatts = watts,
                    MinActivePowerWatts = watts,
                    MaxActivePowerWatts = watts,
                    AverageVoltageVolts = volts,
                    AverageCurrentAmps = amps,
                    AveragePowerFactor = pf,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergyKwh += kwh;
                agg.DataPointsCount++;
                agg.AverageActivePowerWatts += (watts - agg.AverageActivePowerWatts) / agg.DataPointsCount;
                agg.AverageVoltageVolts += (volts - agg.AverageVoltageVolts) / agg.DataPointsCount;
                agg.AverageCurrentAmps += (amps - agg.AverageCurrentAmps) / agg.DataPointsCount;
                agg.AveragePowerFactor += (pf - agg.AveragePowerFactor) / agg.DataPointsCount;
                if (watts < agg.MinActivePowerWatts) agg.MinActivePowerWatts = watts;
                if (watts > agg.MaxActivePowerWatts) agg.MaxActivePowerWatts = watts;
            }
        }

        private async Task UpsertHourAsync(Guid orgId, Guid deviceId, DateTime ts,
            float watts, float volts, float amps, float pf, float kwh)
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
                    AverageActivePowerWatts = watts,
                    MinActivePowerWatts = watts,
                    MaxActivePowerWatts = watts,
                    AverageVoltageVolts = volts,
                    AverageCurrentAmps = amps,
                    AveragePowerFactor = pf,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergyKwh += kwh;
                agg.DataPointsCount++;
                agg.AverageActivePowerWatts += (watts - agg.AverageActivePowerWatts) / agg.DataPointsCount;
                agg.AverageVoltageVolts += (volts - agg.AverageVoltageVolts) / agg.DataPointsCount;
                agg.AverageCurrentAmps += (amps - agg.AverageCurrentAmps) / agg.DataPointsCount;
                agg.AveragePowerFactor += (pf - agg.AveragePowerFactor) / agg.DataPointsCount;
                if (watts < agg.MinActivePowerWatts) agg.MinActivePowerWatts = watts;
                if (watts > agg.MaxActivePowerWatts) agg.MaxActivePowerWatts = watts;
            }
        }

        private async Task UpsertDayAsync(Guid orgId, Guid deviceId, DateTime ts,
            float watts, float volts, float amps, float pf, float kwh)
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
                    AverageActivePowerWatts = watts,
                    MinActivePowerWatts = watts,
                    MaxActivePowerWatts = watts,
                    AverageVoltageVolts = volts,
                    AverageCurrentAmps = amps,
                    AveragePowerFactor = pf,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergyKwh += kwh;
                agg.DataPointsCount++;
                agg.AverageActivePowerWatts += (watts - agg.AverageActivePowerWatts) / agg.DataPointsCount;
                agg.AverageVoltageVolts += (volts - agg.AverageVoltageVolts) / agg.DataPointsCount;
                agg.AverageCurrentAmps += (amps - agg.AverageCurrentAmps) / agg.DataPointsCount;
                agg.AveragePowerFactor += (pf - agg.AveragePowerFactor) / agg.DataPointsCount;
                if (watts < agg.MinActivePowerWatts) agg.MinActivePowerWatts = watts;
                if (watts > agg.MaxActivePowerWatts) agg.MaxActivePowerWatts = watts;
            }
        }

        private async Task UpsertMonthAsync(Guid orgId, Guid deviceId, DateTime ts,
            float watts, float volts, float amps, float pf, float kwh)
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
                    AverageActivePowerWatts = watts,
                    MinActivePowerWatts = watts,
                    MaxActivePowerWatts = watts,
                    AverageVoltageVolts = volts,
                    AverageCurrentAmps = amps,
                    AveragePowerFactor = pf,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergyKwh += kwh;
                agg.DataPointsCount++;
                agg.AverageActivePowerWatts += (watts - agg.AverageActivePowerWatts) / agg.DataPointsCount;
                agg.AverageVoltageVolts += (volts - agg.AverageVoltageVolts) / agg.DataPointsCount;
                agg.AverageCurrentAmps += (amps - agg.AverageCurrentAmps) / agg.DataPointsCount;
                agg.AveragePowerFactor += (pf - agg.AveragePowerFactor) / agg.DataPointsCount;
                if (watts < agg.MinActivePowerWatts) agg.MinActivePowerWatts = watts;
                if (watts > agg.MaxActivePowerWatts) agg.MaxActivePowerWatts = watts;
            }
        }

            }
        }