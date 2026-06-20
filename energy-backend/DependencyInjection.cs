using energy_backend.Api.Services;
using energy_backend.Application;
using energy_backend.Application.Interfaces;
using energy_backend.Core;
using energy_backend.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace energy_backend.Api
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationServices()
                .AddCoreServices()
                .AddInfrastructureServices(configuration);

            // Presentation-layer SignalR notification fan-out (uses IHubContext<UnifiedHub>).
            services.AddScoped<IHubNotificationService, HubNotificationService>();

            // Host-environment abstraction so the application layer stays framework-free.
            services.AddSingleton<IAppEnvironment, AppEnvironment>();

            return services;
        }
    }
}
