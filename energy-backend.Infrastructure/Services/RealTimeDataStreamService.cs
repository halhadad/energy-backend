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

            // ── Catch-up: send recent historical data from the CORRECT table ──
            // Each range has its own aggregate table. Using the wrong table gives
            // the client data at the wrong granularity on first connect.
            var now = DateTime.UtcNow;
            RealTimeChartDataDto catchUp;

            switch (range)
            {
                case "minute":
                    catchUp = new RealTimeChartDataDto
                    {
                        Range = range,
                        Buckets = await _query.GetAggregateMinuteEnergyForOrgAsync(
                            orgId, now.AddMinutes(-5), now)  // last 5 minutes
                    };
                    break;

                case "hour":
                    catchUp = new RealTimeChartDataDto
                    {
                        Range = range,
                        Buckets = await _query.GetAggregateHourEnergyForOrgAsync(
                            orgId, now.AddHours(-2), now)    // last 2 hours
                    };
                    break;

                case "day":
                    catchUp = new RealTimeChartDataDto
                    {
                        Range = range,
                        Buckets = await _query.GetAggregateDayEnergyForOrgAsync(
                            orgId, now.AddDays(-2), now)     // last 2 days
                    };
                    break;

                default: // "month"
                    catchUp = new RealTimeChartDataDto
                    {
                        Range = range,
                        Buckets = await _query.GetAggregateDayEnergyForOrgAsync(
                            orgId, now.AddDays(-60), now)
                    };
                    break;
            }

            if (catchUp.Buckets.Any())
            {
                await _hub.SendRealTimeChartCatchUpAsync(connectionId, catchUp);
                _logger.LogInformation("Sent {Count} catch-up buckets ({Range}) to {ConnId}",
                    catchUp.Buckets.Count, range, connectionId);
            }

            // Also send the single latest bucket so the card updates immediately
            await SendLatestBucket(connectionId, orgId, range);
        }

        public async Task UnsubscribeFromChart(string connectionId, Guid orgId, string range)
        {
            if (!IsValidRange(range)) return;
            await _hub.RemoveFromChartGroupAsync(connectionId, GroupName(orgId, range));
        }

        public async Task NotifyMinuteAggregateUpdated(Guid orgId, AggregateMinuteEnergy _)
        {
            // Re-query org-level sum (all devices) rather than using the passed row
            // which is only one device's bucket.
            var latest = await _query.GetLatestAggregateMinuteEnergyForOrgAsync(orgId);
            if (latest == null) return;

            // Notify minute subscribers
            await _hub.SendRealTimeChartUpdateAsync(GroupName(orgId, "minute"), new RealTimeChartDataDto
            {
                Range = "minute",
                Buckets = new List<RealTimeChartBucketDto> { latest }
            });

            // Also push the current hour bucket so the day chart updates
            var latestHour = await _query.GetLatestAggregateHourEnergyForOrgAsync(orgId);
            if (latestHour != null)
            {
                await _hub.SendRealTimeChartUpdateAsync(GroupName(orgId, "hour"), new RealTimeChartDataDto
                {
                    Range = "hour",
                    Buckets = new List<RealTimeChartBucketDto> { latestHour }
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
                await _hub.SendRealTimeChartCatchUpAsync(connectionId, new RealTimeChartDataDto
                {
                    Range = range,
                    Buckets = new List<RealTimeChartBucketDto> { bucket }
                });
            }
        }

        private static string GroupName(Guid orgId, string range) => $"chart:org:{orgId}:{range}";
        private static bool IsValidRange(string range) =>
            range is "minute" or "hour" or "day" or "month";
    }
}