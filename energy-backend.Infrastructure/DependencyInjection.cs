using energy_backend.Application.Configuration;
using energy_backend.Application.Interfaces;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.BackgroundWorkers;
using energy_backend.Infrastructure.Configuration;
using energy_backend.Infrastructure.Repositories;
using energy_backend.Infrastructure.Seeding;
using energy_backend.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace energy_backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options. Application-layer services receive the plain value (registered from
        // IOptions<T>.Value) so that layer stays free of the Options framework.
        services.Configure<EnergyWorkerOptions>(configuration.GetSection(EnergyWorkerOptions.Section));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.Section));

        services.Configure<EnergySettings>(configuration.GetSection(EnergySettings.Section));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<EnergySettings>>().Value);

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.Section));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AuthOptions>>().Value);

        // Repositories
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IOrganisationRepository, OrganisationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<IAggregateRepository, AggregateRepository>();
        services.AddScoped<IAlertEvaluationRepository, AlertEvaluationRepository>();
        services.AddScoped<IEnergyRollupRepository, EnergyRollupRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<IEnergyReadingRepository, EnergyReadingRepository>();
        services.AddScoped<IEnergyRateRepository, EnergyRateRepository>();

        // Auth (framework-bound implementations)
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();

        // Email sender selected by Email:Provider (defaults to the dev logging sender).
        var emailProvider = configuration.GetSection(EmailOptions.Section)["Provider"];
        if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, LoggingEmailSender>();

        // Seeding / streaming services
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();
        services.AddScoped<IRealTimeDataStreamService, ChartNotificationService>();
        services.AddScoped<IAlertStreamService, AlertNotificationService>();
        // IHubNotificationService is registered in the API layer, alongside the SignalR hub.

        // Background workers.
        // Minute buckets are maintained exclusively by MockDataSimulationWorker (via
        // MockDataAggregationService); HistoricalDownsamplingWorker promotes minute -> hour/day/month.
        // Do not add a second writer for minute buckets: it would double-count TotalActiveEnergyKwh.
        services.AddHostedService<MockDataSimulationWorker>();
        services.AddHostedService<LiveBroadcastWorker>();
        services.AddHostedService<AlertNotificationEvaluator>();
        services.AddHostedService<HistoricalDownsamplingWorker>();

        return services;
    }
}
