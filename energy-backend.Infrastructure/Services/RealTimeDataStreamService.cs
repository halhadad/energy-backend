using energy_backend.Application.Models.SignalR;
using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class RealTimeDataStreamService : IRealTimeDataStreamService
    {
        private readonly IHubNotificationService _hub;
        private readonly IRealTimeDataQueryService _query;
        private readonly ILogger<RealTimeDataStreamService> _logger;

        public RealTimeDataStreamService(
            IHubNotificationService hub,
            IRealTimeDataQueryService query,
            ILogger<RealTimeDataStreamService> logger)
        {
            _hub = hub;
            _query = query;
            _logger = logger;
        }

        public async Task SubscribeToChart(string connectionId, Guid orgId, string range)
        {
            if (!IsValidRange(range))
            {
                await _hub.SendErrorAsync(connectionId, $"Invalid chart range: {range}");
                return;
            }

            var groupName = GroupName(orgId, range);
            await _hub.AddToChartGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnId} subscribed to {Group}", connectionId, groupName);

            var now = DateTime.UtcNow;
            RealTimeChartDataDto catchUp;

            // Org-level totals
            List<RealTimeChartBucketDto> orgBuckets;
            List<DeviceBucketsDto> deviceSeries;

            switch (range)
            {
                case "minute":
                    orgBuckets = await _query.GetAggregateMinuteEnergyForOrgAsync(orgId, now.AddMinutes(-5), now);
                    deviceSeries = await _query.GetDeviceMinuteBucketsForOrgAsync(orgId, now.AddMinutes(-5), now);
                    break;
                case "hour":
                    orgBuckets = await _query.GetAggregateHourEnergyForOrgAsync(orgId, now.AddHours(-2), now);
                    deviceSeries = await _query.GetDeviceHourBucketsForOrgAsync(orgId, now.AddHours(-2), now);
                    break;
                case "day":
                    orgBuckets = await _query.GetAggregateDayEnergyForOrgAsync(orgId, now.AddDays(-2), now);
                    deviceSeries = await _query.GetDeviceDayBucketsForOrgAsync(orgId, now.AddDays(-2), now);
                    break;
                default: // month
                    orgBuckets = await _query.GetAggregateDayEnergyForOrgAsync(orgId, now.AddDays(-60), now);
                    deviceSeries = await _query.GetDeviceDayBucketsForOrgAsync(orgId, now.AddDays(-60), now);
                    break;
            }

            catchUp = new RealTimeChartDataDto
            {
                Range = range,
                Buckets = orgBuckets,
                DeviceSeries = deviceSeries
            };

            if (catchUp.Buckets.Any())
            {
                await _hub.SendRealTimeChartCatchUpAsync(connectionId, catchUp);
                _logger.LogInformation("Sent {Count} catch-up buckets ({Range}) to {ConnId}",
                    catchUp.Buckets.Count, range, connectionId);
            }

            await SendLatestBucket(connectionId, orgId, range);
        }

        public async Task UnsubscribeFromChart(string connectionId, Guid orgId, string range)
        {
            if (!IsValidRange(range)) return;
            await _hub.RemoveFromChartGroupAsync(connectionId, GroupName(orgId, range));
        }

        public async Task NotifyMinuteAggregateUpdated(Guid orgId, AggregateMinuteEnergy _)
        {
            var latest = await _query.GetLatestAggregateMinuteEnergyForOrgAsync(orgId);
            var latestDevices = await _query.GetLatestDeviceMinuteBucketsForOrgAsync(orgId);
            if (latest == null) return;

            await _hub.SendRealTimeChartUpdateAsync(GroupName(orgId, "minute"), new RealTimeChartDataDto
            {
                Range = "minute",
                Buckets = new List<RealTimeChartBucketDto> { latest },
                DeviceSeries = latestDevices
            });

            var latestHour = await _query.GetLatestAggregateHourEnergyForOrgAsync(orgId);
            if (latestHour != null)
            {
                await _hub.SendRealTimeChartUpdateAsync(GroupName(orgId, "hour"), new RealTimeChartDataDto
                {
                    Range = "hour",
                    Buckets = new List<RealTimeChartBucketDto> { latestHour }
                    // DeviceSeries omitted on incremental hour push — client already has them from catch-up
                });
            }
        }

        public async Task NotifyHourAggregateUpdated(Guid orgId, AggregateHourEnergy _)
        {
            var latest = await _query.GetLatestAggregateHourEnergyForOrgAsync(orgId);
            if (latest == null) return;

            await _hub.SendRealTimeChartUpdateAsync(GroupName(orgId, "hour"), new RealTimeChartDataDto
            {
                Range = "hour",
                Buckets = new List<RealTimeChartBucketDto> { latest }
            });

            var latestDay = await _query.GetLatestAggregateDayEnergyForOrgAsync(orgId);
            if (latestDay != null)
            {
                await _hub.SendRealTimeChartUpdateAsync(GroupName(orgId, "day"), new RealTimeChartDataDto
                {
                    Range = "day",
                    Buckets = new List<RealTimeChartBucketDto> { latestDay }
                });
            }
        }

        public async Task NotifyDayAggregateUpdated(Guid orgId, AggregateDayEnergy _)
        {
            var latest = await _query.GetLatestAggregateDayEnergyForOrgAsync(orgId);
            if (latest == null) return;

            await _hub.SendRealTimeChartUpdateAsync(GroupName(orgId, "day"), new RealTimeChartDataDto
            {
                Range = "day",
                Buckets = new List<RealTimeChartBucketDto> { latest }
            });
        }

        public async Task NotifyMonthAggregateUpdated(Guid orgId, AggregateMonthEnergy _)
        {
            var latest = await _query.GetLatestAggregateMonthEnergyForOrgAsync(orgId);
            if (latest == null) return;

            await _hub.SendRealTimeChartUpdateAsync(GroupName(orgId, "month"), new RealTimeChartDataDto
            {
                Range = "month",
                Buckets = new List<RealTimeChartBucketDto> { latest }
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task SendLatestBucket(string connectionId, Guid orgId, string range)
        {
            RealTimeChartBucketDto? bucket = range switch
            {
                "minute" => await _query.GetLatestAggregateMinuteEnergyForOrgAsync(orgId),
                "hour" => await _query.GetLatestAggregateHourEnergyForOrgAsync(orgId),
                "day" => await _query.GetLatestAggregateDayEnergyForOrgAsync(orgId),
                "month" => await _query.GetLatestAggregateMonthEnergyForOrgAsync(orgId),
                _ => null
            };

            if (bucket != null)
            {
                List<DeviceBucketsDto> deviceBuckets = range == "minute"
                    ? await _query.GetLatestDeviceMinuteBucketsForOrgAsync(orgId)
                    : new List<DeviceBucketsDto>();

                await _hub.SendRealTimeChartCatchUpAsync(connectionId, new RealTimeChartDataDto
                {
                    Range = range,
                    Buckets = new List<RealTimeChartBucketDto> { bucket },
                    DeviceSeries = deviceBuckets
                });
            }
        }

        private static string GroupName(Guid orgId, string range) => $"chart:org:{orgId}:{range}";
        private static bool IsValidRange(string range) => range is "minute" or "hour" or "day" or "month";
    }
}