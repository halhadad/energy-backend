namespace energy_backend.Core.Entities;

public class AlertEvent
{
    public Guid AlertEventId { get; set; }
    public Guid AlertId { get; set; }
    public Guid OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double ThresholdValue { get; set; }
    public double TriggeredValueWatts { get; set; }
    public DateTime TriggeredAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public Alert? Alert { get; set; }
}
