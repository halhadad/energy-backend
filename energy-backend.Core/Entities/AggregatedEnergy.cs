using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Entities;

namespace energy_backend.Core.Entities
{
    public class AggregatedEnergy
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public float TotalKwh { get; set; }
        public DateTime PeriodStartTime { get; set; }  

        // navigation property
        public Device Device { get; set; } = null!;
    }

}
