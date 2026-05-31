using energy_backend.Api;
using energy_backend.Application.Models;
using energy_backend.Infrastructure.Data;
using energy_backend.Infrastructure.Hubs;
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

builder.Services.AddApiServices();

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

    return token;
}

static async Task SeedAggregatedEnergyDbAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await SeedData.SeedAggregatedEnergyDbAsync(context);
        logger.LogInformation("Seed completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Seeding failed.");
    }
}
