using energy_backend.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;


namespace energy_backend.Application.Hubs
{
    [Authorize]
    public class RealTimeHubss : Hub
    {
        private readonly IRealTimeService _overviewService;

        public RealTimeHubss(IRealTimeService overviewService)
        {
            _overviewService = overviewService;
            Console.WriteLine($"Service is null? {_overviewService == null}");
        }

        public override async Task OnConnectedAsync()
        {
            var orgId = Context.User.FindFirst("orgId")?.Value;

            if (!string.IsNullOrEmpty(orgId))
            {
                Console.WriteLine("CONNECTED!");
                await Groups.AddToGroupAsync(Context.ConnectionId, orgId);
            }

            await base.OnConnectedAsync();
        }

        public async Task SendOverviewUpdate()
        {
            var orgId = Context.User.FindFirst("orgId")?.Value;

            if (Guid.TryParse(orgId, out var organisationId))
            {
                var overview = await _overviewService.GetOverviewDataAsync(organisationId);
                await Clients.Group(organisationId.ToString()).SendAsync("ReceiveOverviewData", overview);
            }
        }
    }

}
