using System.Text;
using energy_backend.Application;
using energy_backend.Data;
using energy_backend.Application.Hubs;
using energy_backend.Infrastructure;
using energy_backend.Infrastructure.Services;
using energy_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Infrastructure.Services;
using energy_backend.Infrastructure.Seeding;


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
    });

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();


builder.Services.AddApplicationServices();

builder.Services.AddSignalR();
builder.Services.AddHostedService<OverviewDataBackgroundService>();
builder.Services.AddHostedService<AggregationService>();
builder.Services.AddHostedService<EnergyReadingSimulator>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

//seeding
/*using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
    await SeedData.SeedEnergyReadingsEvery5SecAsync(context); // <- raw 5s data
    await SeedData.SeedAggregatedEnergyDbAsync(context);        // <- hourly aggregates
}*/

app.MapControllers();

app.MapHub<energy_backend.Application.Hubs.OverviewHub>("/overviewHub");


app.Run();
