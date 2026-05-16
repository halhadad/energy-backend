using energy_backend.Application.Services;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using energy_backend.Hubs;
using energy_backend.Infrastructure.Repositories;
using energy_backend.Infrastructure.Seeding;
using energy_backend.Infrastructure.Services;
using energy_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// Core 
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<EnergyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("energy-backend.Infrastructure")));

// Auth 
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
                Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!))
        };

        // Allow JWT via query string for SignalR WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/unifiedHub"))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

// Application Services 
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IAlertService, AlertsService>();
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddScoped<IAggregationCoordinatorService, AggregationCoordinatorService>();
builder.Services.AddScoped<IRealTimeDataQueryService, RealTimeDataQueryService>();
builder.Services.AddScoped<IRealTimeDataStreamService, RealTimeDataStreamService>();
builder.Services.AddScoped<IAlertStreamService, AlertsStreamService>();
builder.Services.AddScoped<IHubNotificationService, HubNotificationService>();

// OrganisationAnalyticsService is used concretely by OrganisationService — keep as scoped concrete
builder.Services.AddScoped<OrganisationAnalyticsService>();

// Repositories 
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IAggregatedEnergyRepository, AggregatedEnergyRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IOrganisationRepository, OrganisationRepository>();

// SignalR 
builder.Services.AddSignalR();

// Background Services 
builder.Services.AddHostedService<EnergyReadingSimulator>();
builder.Services.AddHostedService<AlertsMonitorService>();
builder.Services.AddHostedService<HistoricalAggregationService>();
// SummaryBackfillService runs once on startup — keep if you want the DeviceConsumptionSummary table populated
// builder.Services.AddHostedService<SummaryBackfillService>();

var app = builder.Build();

// Pipeline 
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Seed on startup (only fills gaps, safe to run repeatedly)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await SeedData.SeedAggregatedEnergyDbAsync(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Seeding failed");
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapControllers();
app.MapHub<UnifiedHub>("/unifiedHub");

app.Run();