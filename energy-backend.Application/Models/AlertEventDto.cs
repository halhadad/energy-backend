namespace energy_backend.Application.Models;

public class AlertEventDto
{
    public Guid AlertEventId { get; set; }
    public Guid AlertId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double ThresholdValue { get; set; }
    public double TriggeredValueWatts { get; set; }
    public DateTime TriggeredAt { get; set; }
}
