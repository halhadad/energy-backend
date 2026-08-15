using System.Net;
using System.Net.Mail;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace energy_backend.Infrastructure.Services;

// sends over smtp, used when Email:Provider is "Smtp"
public class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = string.IsNullOrEmpty(_options.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.Username, _options.Password)
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = message.Subject,
            Body = message.Body
        };
        mail.To.Add(message.To);

        await client.SendMailAsync(mail, ct);
        logger.LogInformation("Sent email to {To}: {Subject}", message.To, message.Subject);
    }
}
