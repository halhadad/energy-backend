using energy_backend.Core.Entities;
using energy_backend.Data;
using energy_backend.RealTime.energy_backend.Application.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace energy_backend.Infrastructure.Services
{

    public class AlertsMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public AlertsMonitorService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAlertsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task CheckAlertsAsync(CancellationToken ct)
        {
            // create a new scope per iteration
            using var scope = _serviceProvider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
            var notifier = scope.ServiceProvider.GetRequiredService<IAlertsNotifier>();

            var alerts = await db.Alerts
                .Include(a => a.Organisation)
                    .ThenInclude(o => o.Devices)
                .ToListAsync(ct);

            foreach (var alert in alerts)
            {
                var energy = alert.Organisation!.Devices.Sum(d => d.EnergyConsumption);

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

                    var evtDto = new
                    {
                        AlertEventId = evt.AlertEventId,
                        AlertId = evt.AlertId,
                        OrganisationId = evt.OrganisationId,
                        Name = evt.Name,
                        Threshold = evt.Threshold,
                        TriggeredEnergy = evt.TriggeredEnergy,
                        TriggeredAt = evt.TriggeredAt
                    };

                    await notifier.NotifyAsync(alert.AlertId, evtDto, ct);
                }

                if (energy < alert.Threshold * 0.9f && alert.IsActive)
                {
                    alert.IsActive = false;
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }
}

