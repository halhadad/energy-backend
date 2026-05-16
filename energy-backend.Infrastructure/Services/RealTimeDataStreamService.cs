using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using energy_backend.Application.Services; // For IRealTimeDataStreamService, IRealTimeDataQueryService, IHubNotificationService
using energy_backend.Application.Models.SignalR; // For RealTimeChartDataDto, RealTimeChartBucketDto
using energy_backend.Core.Entities; // For AggregateMinuteEnergy, AggregateHourEnergy, etc.
using Microsoft.Extensions.Logging;

// NOTE: This service is now in the API/Presentation layer (energy-backend project)
// It depends on Application layer interfaces for Hub notifications and data querying.

namespace energy_backend.Infrastructure.Services // This is in the API layer
{
    public class RealTimeDataStreamService : IRealTimeDataStreamService // Renamed from ChartStreamService
    {
        private readonly IHubNotificationService _hubNotificationService; // Application layer abstraction for Hubs
        private readonly IRealTimeDataQueryService _realTimeDataQueryService; // Application layer abstraction for data query
        private readonly ILogger<RealTimeDataStreamService> _logger;

        public RealTimeDataStreamService(
            IHubNotificationService hubNotificationService,
            IRealTimeDataQueryService realTimeDataQueryService,
            ILogger<RealTimeDataStreamService> logger)
        {
            _hubNotificationService = hubNotificationService;
            _realTimeDataQueryService = realTimeDataQueryService;
            _logger = logger;
        }

        public async Task SubscribeToChart(string connectionId, Guid orgId, string range)
        {
            if (!IsValidChartRange(range))
            {
                await _hubNotificationService.SendErrorAsync(connectionId, $"Invalid chart range: {range}");
                return;
            }

            var groupName = GetChartGroupName(orgId, range);
            await _hubNotificationService.AddToChartGroupAsync(connectionId, groupName);

            _logger.LogInformation("Client {ConnectionId} subscribed to chart {GroupName}", connectionId, groupName);

            // MANDATORY CATCH-UP WINDOW (Server-controlled)
            var now = DateTime.UtcNow;
            var from = now - TimeSpan.FromMinutes(2); // Safety window: 1 to 2 minutes

            RealTimeChartDataDto catchUpData = new RealTimeChartDataDto { Range = range };

            // Aggregate from per-device aggregates to organization-level for the catch-up window
            switch (range)
            {
                case "minute":
                case "hour": // For hourly chart, catch-up with minute aggregates
                    catchUpData.Buckets = await _realTimeDataQueryService.GetAggregateMinuteEnergyForOrgAsync(orgId, from, now);
                    break;
                case "day":
                case "month": // For daily/monthly charts, catch-up with hour aggregates
                    catchUpData.Buckets = await _realTimeDataQueryService.GetAggregateHourEnergyForOrgAsync(orgId, from, now);
                    break;
            }

            // Send missed buckets immediately
            if (catchUpData.Buckets.Any())
            {
                await _hubNotificationService.SendRealTimeChartCatchUpAsync(connectionId, catchUpData);
                _logger.LogInformation("Sent {Count} catch-up buckets to {ConnectionId} for chart {Range}", catchUpData.Buckets.Count, connectionId, range);
            }

            await SendCurrentBucketUpdate(orgId, range, connectionId);
        }

        public async Task UnsubscribeFromChart(string connectionId, Guid orgId, string range)
        {
            if (!IsValidChartRange(range))
            {
                _logger.LogWarning("Client {ConnectionId} attempted to unsubscribe from invalid chart range: {Range}", connectionId, range);
                return;
            }

            var groupName = GetChartGroupName(orgId, range);
            await _hubNotificationService.RemoveFromChartGroupAsync(connectionId, groupName);

            _logger.LogInformation("Client {ConnectionId} unsubscribed from chart {GroupName}", connectionId, groupName);
        }

        public async Task NotifyMinuteAggregateUpdated(Guid orgId, AggregateMinuteEnergy updatedMinuteAggregate)
        {
            // Query the current org-level minute aggregate (summing all devices for that minute)
            var orgMinuteAggregate = await _realTimeDataQueryService.GetLatestAggregateMinuteEnergyForOrgAsync(orgId);

            if (orgMinuteAggregate != null)
            {
                // Notify minute chart subscribers
                var minuteGroupName = GetChartGroupName(orgId, "minute");
                await _hubNotificationService.SendRealTimeChartUpdateAsync(minuteGroupName, new RealTimeChartDataDto
                {
                    Range = "minute",
                    Buckets = new List<RealTimeChartBucketDto> { orgMinuteAggregate }
                });

                // For hourly charts, also update the current hour bucket (aggregated)
                var orgHourAggregate = await _realTimeDataQueryService.GetLatestAggregateHourEnergyForOrgAsync(orgId);

                if (orgHourAggregate != null)
                {
                    await _hubNotificationService.SendRealTimeChartUpdateAsync(GetChartGroupName(orgId, "hour"), new RealTimeChartDataDto
                    {
                        Range = "hour",
                        Buckets = new List<RealTimeChartBucketDto> { orgHourAggregate }
                    });
                }
            }
        }

        public async Task NotifyHourAggregateUpdated(Guid orgId, AggregateHourEnergy updatedHourAggregate)
        {
            // Query the current org-level hour aggregate (summing all devices for that hour)
            var orgHourAggregate = await _realTimeDataQueryService.GetLatestAggregateHourEnergyForOrgAsync(orgId);

            if (orgHourAggregate != null)
            {
                // Notify hour chart subscribers
                var hourGroupName = GetChartGroupName(orgId, "hour");
                await _hubNotificationService.SendRealTimeChartUpdateAsync(hourGroupName, new RealTimeChartDataDto
                {
                    Range = "hour",
                    Buckets = new List<RealTimeChartBucketDto> { orgHourAggregate }
                });

                // For daily charts, also update the current day bucket (aggregated)
                var orgDayAggregate = await _realTimeDataQueryService.GetLatestAggregateDayEnergyForOrgAsync(orgId);

                if (orgDayAggregate != null)
                {
                    await _hubNotificationService.SendRealTimeChartUpdateAsync(GetChartGroupName(orgId, "day"), new RealTimeChartDataDto
                    {
                        Range = "day",
                        Buckets = new List<RealTimeChartBucketDto> { orgDayAggregate }
                    });
                }
            }
        }

        public async Task NotifyDayAggregateUpdated(Guid orgId, AggregateDayEnergy updatedDayAggregate)
        {
            // Query the current org-level day aggregate (summing all devices for that day)
            var orgDayAggregate = await _realTimeDataQueryService.GetLatestAggregateDayEnergyForOrgAsync(orgId);

            if (orgDayAggregate != null)
            {
                // Notify day chart subscribers
                var dayGroupName = GetChartGroupName(orgId, "day");
                await _hubNotificationService.SendRealTimeChartUpdateAsync(dayGroupName, new RealTimeChartDataDto
                {
                    Range = "day",
                    Buckets = new List<RealTimeChartBucketDto> { orgDayAggregate }
                });

                // For monthly charts, also update the current month bucket (aggregated)
                var orgMonthAggregate = await _realTimeDataQueryService.GetLatestAggregateMonthEnergyForOrgAsync(orgId);

                if (orgMonthAggregate != null)
                {
                    await _hubNotificationService.SendRealTimeChartUpdateAsync(GetChartGroupName(orgId, "month"), new RealTimeChartDataDto
                    {
                        Range = "month",
                        Buckets = new List<RealTimeChartBucketDto> { orgMonthAggregate }
                    });
                }
            }
        }

        public async Task NotifyMonthAggregateUpdated(Guid orgId, AggregateMonthEnergy updatedMonthAggregate)
        {
            // Query the current org-level month aggregate (summing all devices for that month)
            var orgMonthAggregate = await _realTimeDataQueryService.GetLatestAggregateMonthEnergyForOrgAsync(orgId);

            if (orgMonthAggregate != null)
            {
                // Notify month chart subscribers
                var monthGroupName = GetChartGroupName(orgId, "month");
                await _hubNotificationService.SendRealTimeChartUpdateAsync(monthGroupName, new RealTimeChartDataDto
                {
                    Range = "month",
                    Buckets = new List<RealTimeChartBucketDto> { orgMonthAggregate }
                });
            }
        }

        private async Task SendCurrentBucketUpdate(Guid orgId, string range, string connectionId)
        {
            RealTimeChartBucketDto? currentBucket = null;

            switch (range)
            {
                case "minute":
                    currentBucket = await _realTimeDataQueryService.GetLatestAggregateMinuteEnergyForOrgAsync(orgId);
                    break;
                case "hour":
                    currentBucket = await _realTimeDataQueryService.GetLatestAggregateHourEnergyForOrgAsync(orgId);
                    break;
                case "day":
                    currentBucket = await _realTimeDataQueryService.GetLatestAggregateDayEnergyForOrgAsync(orgId);
                    break;
                case "month":
                    currentBucket = await _realTimeDataQueryService.GetLatestAggregateMonthEnergyForOrgAsync(orgId);
                    break;
            }

            if (currentBucket != null)
            {
                await _hubNotificationService.SendRealTimeChartCatchUpAsync(connectionId, new RealTimeChartDataDto
                {
                    Range = range,
                    Buckets = new List<RealTimeChartBucketDto> { currentBucket }
                });
            }
        }


        private string GetChartGroupName(Guid orgId, string range) => $"chart:org:{orgId}:{range}";

        private bool IsValidChartRange(string range) =>
            range == "minute" || range == "hour" || range == "day" || range == "month";
    }
}
