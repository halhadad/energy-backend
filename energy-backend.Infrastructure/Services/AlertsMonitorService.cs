using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class AlertsMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AlertsMonitorService> _logger;

        public AlertsMonitorService(IServiceScopeFactory scopeFactory, ILogger<AlertsMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await CheckAlertsAsync(stoppingToken); }
                catch (Exception ex) { _logger.LogError(ex, "AlertsMonitorService error"); }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task CheckAlertsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
            var alertStream = scope.ServiceProvider.GetRequiredService<IAlertStreamService>();

            var alerts = await alertService.GetAlertsWithOrganisationsAsync();

            foreach (var alert in alerts)
            {
                try
                {
                    // Get the latest minute timestamp for this org, then SUM
                    // AverageWatts across all devices at that timestamp.
                    // The old code used FirstOrDefaultAsync — returning only one
                    // device's row and ignoring the rest of the organisation.
                    var latestTs = await db.AggregateMinuteEnergies
                        .Where(a => a.OrgId == alert.OrganisationId)
                        .MaxAsync(a => (DateTime?)a.Timestamp, ct);

                    float totalWatts = 0f;
                    if (latestTs.HasValue)
                    {
                        totalWatts = await db.AggregateMinuteEnergies
                            .Where(a => a.OrgId == alert.OrganisationId && a.Timestamp == latestTs.Value)
                            .SumAsync(a => a.AverageWatts, ct);
                    }

                    if (totalWatts > alert.Threshold && !alert.IsActive)
                    {
                        alert.IsActive = true;
                        alert.LastTriggeredAt = DateTime.UtcNow;

                        var evt = new AlertEvent
                        {
                            AlertEventId = Guid.NewGuid(),
                            AlertId = alert.AlertId,
                            OrganisationId = alert.OrganisationId,
                            Name = alert.Name,
                            Threshold = alert.Threshold,
                            TriggeredEnergy = totalWatts,
                            TriggeredAt = DateTime.UtcNow
                        };

                        db.AlertEvents.Add(evt);
                        await db.SaveChangesAsync(ct);
                        await alertStream.NotifyAlertTriggered(alert.OrganisationId, evt);

                        _logger.LogInformation("Alert '{Name}' triggered at {W:F1} W (threshold {T:F1} W)",
                            alert.Name, totalWatts, alert.Threshold);
                    }
                    else if (totalWatts < alert.Threshold * 0.9f && alert.IsActive)
                    {
                        alert.IsActive = false;
                        await db.SaveChangesAsync(ct);
                        await alertStream.NotifyAlertResolved(alert.OrganisationId, alert);

                        _logger.LogInformation("Alert '{Name}' resolved", alert.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking alert {AlertId}", alert.AlertId);
                }
            }
        }
    }
}