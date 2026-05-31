using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace energy_backend.Core.Entities
{
    public class EnergyRate
    {
        public Guid Id { get; set; }
        public Guid OrganisationId { get; set; }

        public decimal RatePerKwh { get; set; }

        // The timeline bounds defining when this price is active
        public DateTime ValidFromUtc { get; set; }
        public DateTime? ValidToUtc { get; set; } // Null means this is the active "current" rate
    }
}
