namespace energy_backend.Core.Entities
{
    public class Alert
    {
        public Guid AlertId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public float ThresholdValue { get; set; }
        public bool IsActive { get; set; }

        // Notification Preferences
        public bool EmailEnabled { get; set; } = true;
        public bool InAppNotificationEnabled { get; set; } = true;

        // Smart Cooldowns
        public float InAppCooldownMinutes { get; set; } = 60; // Spammy is okay here
        public float EmailCooldownMinutes { get; set; } = 1440; // 24 hours: Do not spam email

        public DateTime? LastTriggeredInAppAt { get; set; }
        public DateTime? LastTriggeredEmailAt { get; set; }

        // Navigation properties
        public Organisation? Organisation { get; set; }
    }
}