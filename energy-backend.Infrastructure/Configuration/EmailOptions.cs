namespace energy_backend.Infrastructure.Configuration;

public sealed class EmailOptions
{
    public const string Section = "Email";

    // "Log" writes to the log, "Smtp" actually sends
    public string Provider { get; set; } = "Log";

    public string FromAddress { get; set; } = "noreply@energy.local";
    public string FromName { get; set; } = "Energy Monitor";

    // SMTP settings (used only when Provider = "Smtp").
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseSsl { get; set; } = true;
}
