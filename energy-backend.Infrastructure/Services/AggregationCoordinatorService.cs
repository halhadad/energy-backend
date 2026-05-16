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
            // Ensure OrgId is available
            if (energyReading.Device == null)
            {
                energyReading.Device = await _context.Devices
                    .Include(d => d.Organisation)
                    .FirstOrDefaultAsync(d => d.DeviceId == energyReading.DeviceId);

                if (energyReading.Device == null)
                {
                    _logger.LogWarning("Device not found for EnergyReading {EnergyReadingId}.", energyReading.EnergyReadingId);
                    return;
                }
            }

            // SAFETY: ensure Organisation exists
            var orgId = energyReading.Device.OrganisationId;

            if (orgId == Guid.Empty)
            {
                _logger.LogWarning("OrganisationId missing for Device {DeviceId}", energyReading.DeviceId);
                return;
            }

            // Determine the minute bucket timestamp
            var minuteTimestamp = new DateTime(
                energyReading.Timestamp.Year,
                energyReading.Timestamp.Month,
                energyReading.Timestamp.Day,
                energyReading.Timestamp.Hour,
                energyReading.Timestamp.Minute,
                0,
                DateTimeKind.Utc); // Ensure UTC

            var aggregate = await _context.AggregateMinuteEnergies
                .FirstOrDefaultAsync(a =>
                    a.OrgId == orgId &&
                    a.DeviceId == energyReading.DeviceId &&
                    a.Timestamp == minuteTimestamp);

            if (aggregate == null)
            {
                aggregate = new AggregateMinuteEnergy
                {
                    OrgId = orgId,
                    DeviceId = energyReading.DeviceId,
                    Timestamp = minuteTimestamp,
                    TotalEnergy = energyReading.EnergyValue,
                    AverageWatts = energyReading.EnergyValue,
                    MinWatts = energyReading.EnergyValue,
                    MaxWatts = energyReading.EnergyValue,
                    DataPointsCount = 1
                };

                _context.AggregateMinuteEnergies.Add(aggregate);
            }
            else
            {
                aggregate.TotalEnergy += energyReading.EnergyValue;
                aggregate.DataPointsCount++;
                aggregate.AverageWatts = aggregate.TotalEnergy / aggregate.DataPointsCount;

                if (energyReading.EnergyValue < aggregate.MinWatts)
                    aggregate.MinWatts = energyReading.EnergyValue;

                if (energyReading.EnergyValue > aggregate.MaxWatts)
                    aggregate.MaxWatts = energyReading.EnergyValue;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated Minute Aggregate for OrgId {OrgId}, DeviceId {DeviceId} at {Timestamp}",
                aggregate.OrgId, aggregate.DeviceId, aggregate.Timestamp);

            // Notify RealTimeDataStreamService about the updated minute aggregate
            await _realTimeStreamService.NotifyMinuteAggregateUpdated(aggregate.OrgId, aggregate);

            // --- Real-time Roll-up to Hour/Day/Month ---
            await RollUpToHourAsync(orgId, energyReading.DeviceId, energyReading.Timestamp, energyReading.EnergyValue);
            await RollUpToDayAsync(orgId, energyReading.DeviceId, energyReading.Timestamp, energyReading.EnergyValue);
            await RollUpToMonthAsync(orgId, energyReading.DeviceId, energyReading.Timestamp, energyReading.EnergyValue);
        }

        private async Task RollUpToHourAsync(Guid orgId, Guid deviceId, DateTime timestamp, float energyValue)
        {
            var hourTimestamp = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, DateTimeKind.Utc);
            var aggregate = await _context.AggregateHourEnergies
                .FirstOrDefaultAsync(a => a.OrgId == orgId && a.DeviceId == deviceId && a.Timestamp == hourTimestamp);

            if (aggregate == null)
            {
                aggregate = new AggregateHourEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = orgId,
                    DeviceId = deviceId,
                    Timestamp = hourTimestamp,
                    TotalEnergy = energyValue,
                    AverageWatts = energyValue,
                    MinWatts = energyValue,
                    MaxWatts = energyValue,
                    DataPointsCount = 1
                };
                _context.AggregateHourEnergies.Add(aggregate);
            }
            else
            {
                aggregate.TotalEnergy += energyValue;
                aggregate.DataPointsCount++;
                aggregate.AverageWatts = aggregate.TotalEnergy / aggregate.DataPointsCount;
                if (energyValue < aggregate.MinWatts) aggregate.MinWatts = energyValue;
                if (energyValue > aggregate.MaxWatts) aggregate.MaxWatts = energyValue;
            }
            await _context.SaveChangesAsync();
            await _realTimeStreamService.NotifyHourAggregateUpdated(orgId, aggregate);
        }

        private async Task RollUpToDayAsync(Guid orgId, Guid deviceId, DateTime timestamp, float energyValue)
        {
            var dayTimestamp = timestamp.Date;
            var aggregate = await _context.AggregateDayEnergies
                .FirstOrDefaultAsync(a => a.OrgId == orgId && a.DeviceId == deviceId && a.Timestamp == dayTimestamp);

            if (aggregate == null)
            {
                aggregate = new AggregateDayEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = orgId,
                    DeviceId = deviceId,
                    Timestamp = dayTimestamp,
                    TotalEnergy = energyValue,
                    AverageWatts = energyValue,
                    MinWatts = energyValue,
                    MaxWatts = energyValue,
                    DataPointsCount = 1
                };
                _context.AggregateDayEnergies.Add(aggregate);
            }
            else
            {
                aggregate.TotalEnergy += energyValue;
                aggregate.DataPointsCount++;
                aggregate.AverageWatts = aggregate.TotalEnergy / aggregate.DataPointsCount;
                if (energyValue < aggregate.MinWatts) aggregate.MinWatts = energyValue;
                if (energyValue > aggregate.MaxWatts) aggregate.MaxWatts = energyValue;
            }
            await _context.SaveChangesAsync();
            await _realTimeStreamService.NotifyDayAggregateUpdated(orgId, aggregate);
        }

        private async Task RollUpToMonthAsync(Guid orgId, Guid deviceId, DateTime timestamp, float energyValue)
        {
            var monthTimestamp = new DateTime(timestamp.Year, timestamp.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var aggregate = await _context.AggregateMonthEnergies
                .FirstOrDefaultAsync(a => a.OrgId == orgId && a.DeviceId == deviceId && a.Timestamp == monthTimestamp);

            if (aggregate == null)
            {
                aggregate = new AggregateMonthEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = orgId,
                    DeviceId = deviceId,
                    Timestamp = monthTimestamp,
                    TotalEnergy = energyValue,
                    AverageWatts = energyValue,
                    MinWatts = energyValue,
                    MaxWatts = energyValue,
                    DataPointsCount = 1
                };
                _context.AggregateMonthEnergies.Add(aggregate);
            }
            else
            {
                aggregate.TotalEnergy += energyValue;
                aggregate.DataPointsCount++;
                aggregate.AverageWatts = aggregate.TotalEnergy / aggregate.DataPointsCount;
                if (energyValue < aggregate.MinWatts) aggregate.MinWatts = energyValue;
                if (energyValue > aggregate.MaxWatts) aggregate.MaxWatts = energyValue;
            }
            await _context.SaveChangesAsync();
            await _realTimeStreamService.NotifyMonthAggregateUpdated(orgId, aggregate);
        }
    }
}