using System;

namespace energy_backend.Application.Models.SignalR
{
    public class RealTimeHubSubscription
    {
        public Guid OrganisationId { get; set; }
        public DateTime? LastFetchedTimestamp { get; set; } // The timestamp for which the last data was fetched
        // Frontend will handle aggregation based on the single-point data provided by the backend
    }
}
