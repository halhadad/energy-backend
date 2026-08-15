namespace energy_backend.Core.Entities
{
    public class Setting
    {
        public Guid SettingId { get; set; }
        public Guid UserId { get; set; }
        public bool RequireEmail { get; set; } = true;

        // Navigation properties
        public User? User { get; set; }
    }
}