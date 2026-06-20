using System.ComponentModel.DataAnnotations;

namespace energy_backend.Core.Entities
{
    public class Organisation
    {
        [Key]
        public Guid OrganisationId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal MonthlyBudgetUsd { get; set; }
        public string CurrencyCode { get; set; } = "USD";

        public User? User { get; set; }
        public ICollection<Device>? Devices { get; set; }
    }
}
