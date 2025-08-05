using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Data;
using energy_backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class EnergyReadingSimulator : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EnergyReadingSimulator> _logger;
        private readonly Random _rng = new();

        public EnergyReadingSimulator(IServiceScopeFactory scopeFactory, ILogger<EnergyReadingSimulator> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EnergyReadingSimulator started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
                    var now = DateTime.UtcNow;

                    var devices = await context.Devices.ToListAsync(stoppingToken);

                    foreach (var device in devices)
                    {
                        var reading = new EnergyReading
                        {
                            EnergyReadingId = Guid.NewGuid(),
                            DeviceId = device.DeviceId,
                            EnergyValue = (float)Math.Round(_rng.NextDouble() * 0.01, 5),
                            Timestamp = now
                        };

                        await context.EnergyReadings.AddAsync(reading, stoppingToken);
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Inserted readings at {Time}", now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inserting real-time readings");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

}
