using energy_backend.Application.Interfaces;
using energy_backend.Application.Services;
using Microsoft.Extensions.DependencyInjection;
namespace energy_backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<ISnapshotService, SnapshotService>();
        services.AddScoped<IEnergyAnalyticsOrchestratorService, EnergyAnalyticsOrchestratorService>();
        //services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IMockDataAggregationService, MockDataAggregationService>();
        return services;
    }
}