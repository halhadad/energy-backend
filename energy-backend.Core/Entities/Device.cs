using energy_backend.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace energy_backend.Core.Entities
{
    public class Device
    {
        [Key]
        public Guid DeviceId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DeviceType Type { get; set; }
        public string? Description { get; set; }
        public double RatedPowerWatts { get; set; }

        public Organisation? Organisation { get; set; }
    }
}
