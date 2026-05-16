using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Core.Entities;

namespace energy_backend.Core.Entities
{
    // Monthly
    public class DeviceConsumptionSummary
    {
        public Guid DeviceConsumptionSummaryId { get; set; }
        public Guid DeviceId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        public double TotalConsumption { get; set; }

        public DateTime LastUpdated { get; set; }

        // Navigation properties
        public Device? Device { get; set; }
    }
}
