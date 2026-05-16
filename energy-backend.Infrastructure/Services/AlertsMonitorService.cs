using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using energy_backend.Application.Services;

namespace energy_backend.Infrastructure.Services
{

    public class AlertsMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider; // Keep for now to get DBContext and Org Data
        // REMOVED: cannot safely inject scoped service into singleton
        // private readonly IAlertStreamService _alertStreamService; // Injected

        public AlertsMonitorService(
            IServiceProvider serviceProvider // Keep serviceProvider to create scope for DbContext
                                             // REMOVED: IAlertStreamService alertStreamService
        )
        {
            _serviceProvider = serviceProvider;
            // REMOVED assignment
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAlertsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Keep polling for alert checks for now
            }
        }

        private async Task CheckAlertsAsync(CancellationToken ct)
        {
            // create a new scope per iteration
            using var scope = _serviceProvider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
            var alertStream = scope.ServiceProvider.GetRequiredService<IAlertStreamService>();

            var alerts = await db.Alerts
                .Include(a => a.Organisation)
                .ToListAsync(ct);

            foreach (var alert in alerts)
            {
                var latestMinuteAggregate = await db.AggregateMinuteEnergies
                    .Where(a => a.OrgId == alert.OrganisationId)
                    .OrderByDescending(a => a.Timestamp)
                    .FirstOrDefaultAsync(ct);

                var energy = latestMinuteAggregate?.AverageWatts ?? 0;

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

                    await alertStream.NotifyAlertTriggered(alert.OrganisationId, evt);
                }
                else if (energy < alert.Threshold * 0.9f && alert.IsActive)
                {
                    alert.IsActive = false;

                    await alertStream.NotifyAlertResolved(alert.OrganisationId, alert);
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }
}