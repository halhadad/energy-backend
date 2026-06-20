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
        public double ActivePowerWatts { get; set; }
        public double VoltageVolts { get; set; }
        public double CurrentAmps { get; set; }
        public double PowerFactor { get; set; }
        public double ActiveEnergyKwh { get; set; }

        public Device? Device { get; set; }
    }
}
