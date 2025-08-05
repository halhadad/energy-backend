using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using energy_backend.Application.Hubs;
using energy_backend.Data;

namespace Infrastructure.Services
{
    public class OverviewDataBackgroundService : BackgroundService
    {
        private readonly IHubContext<OverviewHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;

        public OverviewDataBackgroundService(
            IHubContext<OverviewHub> hubContext,
            IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();

                
                var data = await dbContext.Devices
                    .Select(d => new { d.DeviceId, d.Name, d.EnergyConsumption })
                    .ToListAsync(stoppingToken);
                var simData = 5;
                await _hubContext.Clients.All.SendAsync("ReceiveOverviewData", simData, cancellationToken: stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
