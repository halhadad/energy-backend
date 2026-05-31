using System.ComponentModel.DataAnnotations;
namespace energy_backend.Core.Entities
{

    public class EnergyReading
    {
        [Key]
        public Guid EnergyReadingId { get; set; }
        public Guid OrgId { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public float ActivePowerWatts { get; set; }
        public float VoltageVolts { get; set; }
        public float CurrentAmps { get; set; }
        public float PowerFactor { get; set; }
        public float ActiveEnergyKwh { get; set; }

        public Device? Device { get; set; }
    }
}
