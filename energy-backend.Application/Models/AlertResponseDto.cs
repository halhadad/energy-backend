namespace energy_backend.Application.Models
{
    public class AlertResponseDto
    {
        public Guid AlertId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Threshold value — see ThresholdUnit for the unit.</summary>
        public float Threshold { get; set; }

        /// <summary>Always "W" (Watts). Included so the frontend can display the unit.</summary>
        public string ThresholdUnit { get; set; } = "W";

        public float CurrentPowerWatts { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
    }
}