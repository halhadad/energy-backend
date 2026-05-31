using energy_backend.Application.Interfaces;
using energy_backend.Core.Interfaces;
using energy_backend.Core.Repositories;
using energy_backend.Infrastructure.BackgroundWorkers;
using energy_backend.Infrastructure.Repositories;
using energy_backend.Infrastructure.Seeding;
using energy_backend.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace energy_backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IOrganisationRepository, OrganisationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<IAggregateRepository, AggregateRepository>();
        services.AddScoped<IAlertEvaluationRepository, AlertEvaluationRepository>();
        services.AddScoped<IEnergyRollupRepository, EnergyRollupRepository>();
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

        services.AddScoped<IRealTimeDataStreamService, ChartNotificationService>();
        services.AddScoped<IAlertStreamService, AlertNotificationService>();
        services.AddScoped<IHubNotificationService, HubNotificationService>();

        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();


        services.AddHostedService<MockDataSimulationWorker>();
        services.AddHostedService<AlertNotificationEvaluator>();
        services.AddHostedService<RecurrentDataRollupWorker>();
        services.AddHostedService<HistoricalDownsamplingWorker>();

        return services;
    }
}
