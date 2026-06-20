using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace energy_backend.Infrastructure.Services;

public class AlertNotificationEvaluator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertNotificationEvaluator> _logger;
    private readonly EnergyWorkerOptions _options;

    public AlertNotificationEvaluator(
        IServiceScopeFactory scopeFactory,
        IOptions<EnergyWorkerOptions> options,
        ILogger<AlertNotificationEvaluator> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(_options.AlertEvaluationIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CheckAlertsAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "AlertNotificationEvaluator error"); }
            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task CheckAlertsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
        var evalRepo = scope.ServiceProvider.GetRequiredService<IAlertEvaluationRepository>();
        var alertStream = scope.ServiceProvider.GetRequiredService<IAlertStreamService>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var alerts = await alertRepo.GetAllWithOrganisationsAsync();
        var now = DateTime.UtcNow;

        foreach (var alert in alerts)
        {
            try
            {
                var (latestTs, totalWatts) = await evalRepo.GetLatestMinutePowerSumAsync(alert.OrganisationId, ct);

                // no readings yet, leave the alert as it is
                if (latestTs is null) continue;

                // resolved alerts are kept as history and never fire again
                if (alert.ResolvedAt is not null) continue;

                // fire once when power crosses the threshold, then it stays active until the user resolves it
                if (totalWatts > alert.ThresholdValue && !alert.IsActive)
                {
                    await TriggerAsync(alert, totalWatts, now, alertRepo, alertStream, emailSender);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking alert {AlertId}", alert.AlertId);
            }
        }
    }

    private async Task TriggerAsync(
        Alert alert, double totalWatts, DateTime now,
        IAlertRepository alertRepo, IAlertStreamService alertStream, IEmailSender emailSender)
    {
        alert.IsActive = true;

        // fires once per trigger, the client handles the repeat beep while it stays active
        if (alert.InAppNotificationEnabled)
        {
            var evt = new AlertEvent
            {
                AlertEventId = Guid.NewGuid(),
                AlertId = alert.AlertId,
                OrganisationId = alert.OrganisationId,
                Name = alert.Name,
                ThresholdValue = alert.ThresholdValue,
                TriggeredValueWatts = totalWatts,
                TriggeredAt = now
            };
            await alertRepo.AddEventAsync(evt);
            alert.LastTriggeredInAppAt = now;
            await alertStream.NotifyAlertTriggered(alert.OrganisationId, evt);
            _logger.LogInformation(
                "Alert '{Name}' triggered at {Watts:F1} W (threshold {Threshold:F1} W)",
                alert.Name, totalWatts, alert.ThresholdValue);
        }

        // email needs the alert toggle, the owner's setting, the cooldown, and an actual address
        var owner = alert.Organisation?.User;
        var requireEmail = owner?.Setting?.RequireEmail ?? true;
        var emailDue = alert.LastTriggeredEmailAt is null
            || now - alert.LastTriggeredEmailAt.Value >= TimeSpan.FromMinutes(alert.EmailCooldownMinutes);
        if (alert.EmailEnabled && requireEmail && emailDue
            && owner is not null && !string.IsNullOrWhiteSpace(owner.Email))
        {
            await emailSender.SendAsync(new EmailMessage
            {
                To = owner.Email,
                Subject = $"Energy alert: {alert.Name}",
                Body = $"Alert '{alert.Name}' triggered at {totalWatts:F1} W "
                     + $"(threshold {alert.ThresholdValue:F1} W) on {now:u}."
            });
            alert.LastTriggeredEmailAt = now;
        }

        await alertRepo.SaveChangesAsync();
    }
}
