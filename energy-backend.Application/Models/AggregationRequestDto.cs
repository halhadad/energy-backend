using System;
using energy_backend.Application.Services; // For AggregationLevel

namespace energy_backend.Application.Models
{
    public class AggregationRequestDto
    {
        public Guid OrganisationId { get; set; }
        public Guid? DeviceId { get; set; } // Optional: aggregate for a specific device
        public AggregationLevel AggregationLevel { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
