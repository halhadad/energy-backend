using energy_backend.Application.Interfaces;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services;

public class AlertNotificationEvaluator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertNotificationEvaluator> _logger;

    public AlertNotificationEvaluator(
        IServiceScopeFactory scopeFactory,
        ILogger<AlertNotificationEvaluator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CheckAlertsAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "AlertNotificationEvaluator error"); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task CheckAlertsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
        var evalRepo = scope.ServiceProvider.GetRequiredService<IAlertEvaluationRepository>();
        var alertStream = scope.ServiceProvider.GetRequiredService<IAlertStreamService>();

        var alerts = await alertRepo.GetAllWithOrganisationsAsync();

        foreach (var alert in alerts)
        {
            try
            {
                var (_, totalWatts) = await evalRepo.GetLatestMinutePowerSumAsync(alert.OrganisationId, ct);

                if (totalWatts > alert.ThresholdValue && !alert.IsActive)
                {
                    alert.IsActive = true;
                    alert.LastTriggeredInAppAt = DateTime.UtcNow;

                    var evt = new AlertEvent
                    {
                        AlertEventId = Guid.NewGuid(),
                        AlertId = alert.AlertId,
                        OrganisationId = alert.OrganisationId,
                        Name = alert.Name,
                        ThresholdValue = alert.ThresholdValue,
                        TriggeredValueWatts = totalWatts,
                        TriggeredAt = DateTime.UtcNow
                    };

                    await alertRepo.AddEventAsync(evt);
                    await alertRepo.SaveChangesAsync();
                    await alertStream.NotifyAlertTriggered(alert.OrganisationId, evt);

                    _logger.LogInformation(
                        "Alert '{Name}' triggered at {Watts:F1} W (threshold {Threshold:F1} W)",
                        alert.Name, totalWatts, alert.ThresholdValue);
                }
                else if (totalWatts < alert.ThresholdValue * 0.9f && alert.IsActive)
                {
                    alert.IsActive = false;
                    await alertRepo.SaveChangesAsync();
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
