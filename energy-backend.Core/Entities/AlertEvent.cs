using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Entities;

namespace energy_backend.Core.Entities
{
    public class AlertEvent
    {
        public Guid AlertEventId { get; set; }
        public Guid AlertId { get; set; }
        public Guid OrganisationId { get; set; }

        public string Name { get; set; } = string.Empty;
        public float Threshold { get; set; }
        public float TriggeredEnergy { get; set; }
        public DateTime TriggeredAt { get; set; }

        public Alert? Alert { get; set; }
    }
}
