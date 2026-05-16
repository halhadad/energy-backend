using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace energy_backend.Core.Entities
{
    public class AggregateMonthEnergy
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrgId { get; set; }
        public Guid DeviceId { get; set; } // New: per-device aggregation

        // Timestamp represents the start of the month bucket (UTC)
        public DateTime Timestamp { get; set; }

        public float TotalEnergy { get; set; }
        public float AverageWatts { get; set; }
        public float MinWatts { get; set; }
        public float MaxWatts { get; set; }
        public int DataPointsCount { get; set; }

        public Device? Device { get; set; }
    }
}
