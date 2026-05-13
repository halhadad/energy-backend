

using energy_backend.Core.Entities;

namespace energy_backend.RealTime
{

    namespace energy_backend.Application.Realtime
    {
        public interface IAlertsNotifier
        {
            Task NotifyAsync(Guid alertId, object evt, CancellationToken ct);
        }
    }

}
