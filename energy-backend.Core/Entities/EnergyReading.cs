namespace energy_backend.Entities
{
    public class EnergyReading
    {
        public Guid EnergyReadingId { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public float EnergyValue { get; set; }


        // Navigation properties
        public Device? Device { get; set; } // many to one relationship
    }
    
}
