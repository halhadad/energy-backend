namespace energy_backend.Application.Models;

public sealed class EmailMessage
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
}
