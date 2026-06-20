namespace energy_backend.Application.Models
{
    public class AlertResponseDto
    {
        public Guid AlertId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public string ThresholdUnit { get; set; } = "W";
        public double CurrentPowerWatts { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string Status { get; set; } = "Monitoring";
        public bool EmailEnabled { get; set; }
        public bool InAppEnabled { get; set; }
    }
}
