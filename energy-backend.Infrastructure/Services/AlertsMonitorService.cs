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
                try
                {
                    await CheckAlertsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AlertsMonitorService error during check cycle");
                }
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
                    var latestMinute = await db.AggregateMinuteEnergies
                        .Where(a => a.OrgId == alert.OrganisationId)
                        .OrderByDescending(a => a.Timestamp)
                        .FirstOrDefaultAsync(ct);

                    var energy = latestMinute?.AverageWatts ?? 0;

                    if (energy > alert.Threshold && !alert.IsActive)
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
                            TriggeredEnergy = energy,
                            TriggeredAt = DateTime.UtcNow
                        };

                        db.AlertEvents.Add(evt);
                        await db.SaveChangesAsync(ct);
                        await alertStream.NotifyAlertTriggered(alert.OrganisationId, evt);

                        _logger.LogInformation("Alert {Name} triggered for org {OrgId} at {Energy}W",
                            alert.Name, alert.OrganisationId, energy);
                    }
                    else if (energy < alert.Threshold * 0.9f && alert.IsActive)
                    {
                        alert.IsActive = false;
                        await db.SaveChangesAsync(ct);
                        await alertStream.NotifyAlertResolved(alert.OrganisationId, alert);

                        _logger.LogInformation("Alert {Name} resolved for org {OrgId}", alert.Name, alert.OrganisationId);
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