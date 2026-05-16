using energy_backend.Application.Services;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using energy_backend.Infrastructure.Repositories;
using energy_backend.Infrastructure.Seeding;
using energy_backend.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using energy_backend.Hubs;
using System.Text;
using energy_backend.Application; // Added for UnifiedHub


var builder = WebApplication.CreateBuilder(args);

//cors
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowFrontend",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173")
                                        .AllowAnyMethod()
                                        .AllowAnyHeader()
                                        .AllowCredentials();
                      });
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<EnergyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("energy-backend.Infrastructure")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AppSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["AppSettings:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/overviewHub") || path.StartsWithSegments("/unifiedHub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });


// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<OrganisationAnalyticsService>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IRealTimeService, RealTimeService>();
builder.Services.AddScoped<IAlertService, AlertsService>();

builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddScoped<IAggregationCoordinatorService, AggregationCoordinatorService>();

builder.Services.AddScoped<IDeviceStreamService, DeviceStreamService>();
builder.Services.AddScoped<IAlertStreamService, AlertsStreamService>();
builder.Services.AddScoped<IRealTimeDataStreamService, RealTimeDataStreamService>();

builder.Services.AddScoped<IRealTimeDataQueryService, RealTimeDataQueryService>();
builder.Services.AddScoped<IAlertQueryService, AlertQueryService>();
builder.Services.AddScoped<IDeviceQueryService, DeviceQueryService>();

builder.Services.AddScoped<IHubNotificationService, HubNotificationService>();

// Repos
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IAggregatedEnergyRepository, AggregatedEnergyRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IOrganisationRepository, OrganisationRepository>();

builder.Services.AddApplicationServices();

builder.Services.AddSignalR();

// Hosted Services (Temporarily disabled for debugging SignalR)
// builder.Services.AddHostedService<HistoricalAggregationService>();
// builder.Services.AddHostedService<EnergyReadingSimulator>();
// builder.Services.AddHostedService<SummaryBackfillService>();
// builder.Services.AddHostedService<AlertsMonitorService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


//seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try 
    {
        await SeedData.SeedEnergyReadingsEvery5SecAsync(context);
        await SeedData.SeedAggregatedEnergyDbAsync(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseWebSockets();

// Map UnifiedHub
app.MapHub<UnifiedHub>("/unifiedHub");

app.Run();