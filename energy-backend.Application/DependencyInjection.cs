using energy_backend.Application.Common.Mapping;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace energy_backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper scans this assembly for Profile subclasses.
        services.AddAutoMapper(cfg => { }, typeof(EnergyMappingProfile).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<ISnapshotService, SnapshotService>();
        services.AddScoped<IMockDataAggregationService, MockDataAggregationService>();

        // Page 1: Real-Time Dashboard (raw 5-second readings).
        services.AddScoped<ILiveDataOrchestratorService, LiveDataOrchestratorService>();

        // Page 2: Historical Analytics (preset-driven aggregate rollups).
        services.AddScoped<IHistoricalDataOrchestratorService, HistoricalDataOrchestratorService>();

        return services;
    }
}
