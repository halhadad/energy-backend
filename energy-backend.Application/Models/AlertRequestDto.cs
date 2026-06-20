namespace energy_backend.Application.Models
{
    public class AlertRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid OrganisationId { get; set; }
        public double Threshold { get; set; }

        // Per-alert notification channels (default on).
        public bool EmailEnabled { get; set; } = true;
        public bool InAppEnabled { get; set; } = true;
    }
}
