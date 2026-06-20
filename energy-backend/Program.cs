using energy_backend.Api;
using energy_backend.Application.Models;
using energy_backend.Api.Hubs;
using energy_backend.Application.Configuration;
using energy_backend.Infrastructure.Data;
using energy_backend.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("AppSettings"));


builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddDbContext<EnergyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("energy-backend.Infrastructure")));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                Encoding.UTF8.GetBytes(GetRequiredJwtToken(builder.Configuration)))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/unifiedHub"))
                    context.Token = token;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await SeedAggregatedEnergyDbAsync(app.Services);

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapControllers();
app.MapHub<UnifiedHub>("/unifiedHub");

app.Run();

static string GetRequiredJwtToken(IConfiguration configuration)
{
    var token = configuration["AppSettings:Token"];
    if (string.IsNullOrWhiteSpace(token))
    {
        throw new InvalidOperationException(
            "JWT token is not configured. Set AppSettings:Token via dotnet user-secrets or the AppSettings__Token environment variable.");
    }

    // hmac-sha512 needs at least 64 bytes, fail early with a clear message
    if (Encoding.UTF8.GetByteCount(token) < 64)
    {
        throw new InvalidOperationException(
            "AppSettings:Token must be at least 64 bytes for HMAC-SHA512 signing.");
    }

    return token;
}

static async Task SeedAggregatedEnergyDbAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
    var settings = scope.ServiceProvider.GetRequiredService<EnergySettings>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // no migrations here, just make sure the schema exists on first run
        await context.Database.EnsureCreatedAsync();
        await SeedData.SeedAggregatedEnergyDbAsync(context, settings.CostPerKwh);
        logger.LogInformation("Seed completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Seeding failed.");
    }
}
