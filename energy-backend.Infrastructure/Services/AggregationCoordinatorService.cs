using System;
using System.Linq;
using System.Threading.Tasks;
using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using energy_backend.Core.Interfaces;

namespace energy_backend.Infrastructure.Services
{
    public class AggregationCoordinatorService : IAggregationCoordinatorService
    {
        private readonly EnergyDbContext _context;
        private readonly ILogger<AggregationCoordinatorService> _logger;
        private readonly IRealTimeDataStreamService _realTimeStreamService; // Injected service for chart streaming
        // private readonly IEventPublisher _eventPublisher; // Placeholder for event publishing

        public AggregationCoordinatorService(
            EnergyDbContext context,
            ILogger<AggregationCoordinatorService> logger,
            IRealTimeDataStreamService realTimeStreamService) // Injected
        {
            _context = context;
            _logger = logger;
            _realTimeStreamService = realTimeStreamService;
        }

        public async Task ProcessEnergyReadingAsync(EnergyReading energyReading)
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
            var value = energyReading.EnergyValue;

            // All four buckets calculated in memory, one SaveChanges at the end
            await UpsertMinuteAsync(orgId, energyReading.DeviceId, ts, value);
            await UpsertHourAsync(orgId, energyReading.DeviceId, ts, value);
            await UpsertDayAsync(orgId, energyReading.DeviceId, ts, value);
            await UpsertMonthAsync(orgId, energyReading.DeviceId, ts, value);

            await _context.SaveChangesAsync(); //  single roundtrip

            // Notify stream after save
            var minute = await _context.AggregateMinuteEnergies
                .FirstOrDefaultAsync(a => a.OrgId == orgId && a.DeviceId == energyReading.DeviceId
                    && a.Timestamp == new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, 0, DateTimeKind.Utc));

            if (minute != null)
                await _realTimeStreamService.NotifyMinuteAggregateUpdated(orgId, minute);
        }

        private async Task UpsertMinuteAsync(Guid orgId, Guid deviceId, DateTime ts, float value)
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
                    TotalEnergy = value,
                    AverageWatts = value,
                    MinWatts = value,
                    MaxWatts = value,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergy += value;
                agg.DataPointsCount++;
                agg.AverageWatts = agg.TotalEnergy / agg.DataPointsCount;
                if (value < agg.MinWatts) agg.MinWatts = value;
                if (value > agg.MaxWatts) agg.MaxWatts = value;
            }
        }

        private async Task UpsertHourAsync(Guid orgId, Guid deviceId, DateTime ts, float value)
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
                    TotalEnergy = value,
                    AverageWatts = value,
                    MinWatts = value,
                    MaxWatts = value,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergy += value; agg.DataPointsCount++;
                agg.AverageWatts = agg.TotalEnergy / agg.DataPointsCount;
                if (value < agg.MinWatts) agg.MinWatts = value;
                if (value > agg.MaxWatts) agg.MaxWatts = value;
            }
        }

        private async Task UpsertDayAsync(Guid orgId, Guid deviceId, DateTime ts, float value)
        {
            var bucket = ts.Date;
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
                    TotalEnergy = value,
                    AverageWatts = value,
                    MinWatts = value,
                    MaxWatts = value,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergy += value; agg.DataPointsCount++;
                agg.AverageWatts = agg.TotalEnergy / agg.DataPointsCount;
                if (value < agg.MinWatts) agg.MinWatts = value;
                if (value > agg.MaxWatts) agg.MaxWatts = value;
            }
        }

        private async Task UpsertMonthAsync(Guid orgId, Guid deviceId, DateTime ts, float value)
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
                    TotalEnergy = value,
                    AverageWatts = value,
                    MinWatts = value,
                    MaxWatts = value,
                    DataPointsCount = 1
                });
            }
            else
            {
                agg.TotalEnergy += value; agg.DataPointsCount++;
                agg.AverageWatts = agg.TotalEnergy / agg.DataPointsCount;
                if (value < agg.MinWatts) agg.MinWatts = value;
                if (value > agg.MaxWatts) agg.MaxWatts = value;
            }
        }
    }
}