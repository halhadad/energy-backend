using System.Text;
using energy_backend.Application;
using energy_backend.Application.Hubs;
using energy_backend.Application.Services;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using energy_backend.Hubs;
using energy_backend.Infrastructure;
using energy_backend.Infrastructure.Repositories;
using energy_backend.Infrastructure.Seeding;
using energy_backend.Infrastructure.Services;
using energy_backend.Infrastructure.SignalR;
using energy_backend.RealTime;
using energy_backend.RealTime.energy_backend.Application.Realtime;
using energy_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;


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

                // Add AlertsHub here
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/overviewHub") || path.StartsWithSegments("/hubs/alerts")))
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

// Repos
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IAggregatedEnergyRepository, AggregatedEnergyRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IOrganisationRepository, OrganisationRepository>();
builder.Services.AddSingleton<RealTimeConnectionTracker>();




builder.Services.AddApplicationServices();

builder.Services.AddSignalR();

builder.Services.AddHostedService<AggregationService>();
builder.Services.AddHostedService<EnergyReadingSimulator>();
builder.Services.AddHostedService<SummaryBackfillService>();


builder.Services.AddSingleton<AlertsConnectionTracker>();
builder.Services.AddScoped<IAlertsNotifier, SignalRAlertsNotifier>();
builder.Services.AddHostedService<AlertsMonitorService>();





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
    await SeedData.SeedEnergyReadingsEvery5SecAsync(context); // <- raw 5s data
    await SeedData.SeedAggregatedEnergyDbAsync(context);        // <- hourly aggregates
}




app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRouting(); 
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseWebSockets();
app.MapHub<RealTimeHub>("/overviewHub");
app.MapHub<AlertsHub>("/hubs/alerts");

app.Run();
