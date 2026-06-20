using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace energy_backend.Infrastructure.Services;

// logs the message instead of sending it, used when Email:Provider is "Log"
public class LoggingEmailSender(
    IOptions<EmailOptions> options,
    ILogger<LoggingEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] From {From} <{FromAddr}> To {To} | {Subject}\n{Body}",
            _options.FromName, _options.FromAddress, message.To, message.Subject, message.Body);
        return Task.CompletedTask;
    }
}
