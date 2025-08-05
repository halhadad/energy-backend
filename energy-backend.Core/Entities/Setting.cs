namespace energy_backend.Entities
{
    public class Setting
    {
        public Guid SettingId { get; set; }
        public Guid UserId { get; set; }
        public Guid FavoriteOrg { get; set; } = Guid.Empty; 
        public bool PeakAlerts { get; set; } = false;
        public bool UnusualAlerts { get; set; } = false;
        public bool BudgetAlerts { get; set; } = true;
        public bool RequireEmail { get; set; } = true;

        // Navigation properties
        public User? User { get; set; }


    }
}
