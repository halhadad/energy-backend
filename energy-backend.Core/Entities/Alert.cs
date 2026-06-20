namespace energy_backend.Core.Entities
{
    public class Alert
    {
        public Guid AlertId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double ThresholdValue { get; set; }
        public bool IsActive { get; set; }

        // Set when the user manually resolves a triggered alert. A resolved alert is
        // terminal: it stays resolved (history) and is never re-evaluated/re-triggered.
        public DateTime? ResolvedAt { get; set; }

        public bool EmailEnabled { get; set; } = true;
        public bool InAppNotificationEnabled { get; set; } = true;

        public double InAppCooldownMinutes { get; set; } = 60;
        public double EmailCooldownMinutes { get; set; } = 1440;

        public DateTime? LastTriggeredInAppAt { get; set; }
        public DateTime? LastTriggeredEmailAt { get; set; }

        public Organisation? Organisation { get; set; }
    }
}
