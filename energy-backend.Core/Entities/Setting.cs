namespace energy_backend.Core.Entities
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

        /// <summary>
        /// User-configured electricity price in their local currency per kWh.
        /// Used to compute cost metrics on the dashboard.
        /// Default 0.28 (USD/EUR global average). 
        /// Frontend lets the user label the currency themselves.
        /// </summary>
        public float ElectricityCostPerKwh { get; set; } = 0.28f;

        // Navigation properties
        public User? User { get; set; }
    }
}