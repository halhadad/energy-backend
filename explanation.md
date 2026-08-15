Repository: energy-backend

Overview

This repository is a .NET 9 backend for an energy monitoring application. It provides a REST API and real-time updates via SignalR. The solution is split into multiple projects following a layered architecture (Api, Application, Core, Infrastructure).

High-level projects

- energy-backend (API project)
  - Contains Program.cs which is the application entrypoint, API controllers, and wiring for dependency injection (AddApiServices).
  - Exposes REST endpoints and the SignalR hub for real-time updates.

- energy-backend.Application
  - Contains application-level services and DTOs (business logic orchestration). Interfaces used by the API live here.

- energy-backend.Core
  - Contains domain entities (models), repository interfaces, and basic shared contracts.

- energy-backend.Infrastructure
  - Contains implementations for repositories, EF Core DbContext, SignalR hub services, background workers, and seeding.

How the app starts (Program.cs)

- Program.cs sets up WebApplication using minimal hosting model.
- It registers CORS policy called "AllowFrontend" allowing origin http://localhost:5173.
- Adds Controllers, OpenAPI (Swagger) and SignalR.
- Registers the EnergyDbContext (EF Core) to use SQL Server. The migrations assembly is set to "energy-backend.Infrastructure".
- Configures JWT authentication (JwtBearer) and sets up message-received logic so SignalR connections can provide the access token as a query string parameter when connecting to the "/unifiedHub" path.
- Calls AddApiServices() extension to wire up application, core, and infrastructure services.
- On development environment, it maps OpenAPI endpoints.
- Seeds aggregated energy DB (SeedData.SeedAggregatedEnergyDbAsync).
- Enables HTTPS redirection, CORS, routing, authentication, authorization, websockets, maps controllers and maps the SignalR hub at "/unifiedHub".

Example snippet from Program.cs:

```csharp
builder.Services.AddDbContext<EnergyDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("DefaultConnection"),
		b => b.MigrationsAssembly("energy-backend.Infrastructure")));

app.MapHub<UnifiedHub>("/unifiedHub");
```

Dependency injection (AddApiServices / AddInfrastructureServices)

- The API project has a static class DependencyInjection.AddApiServices that chains AddApplicationServices(), AddCoreServices(), and AddInfrastructureServices(). This is how services from other projects get registered in the DI container.

- infrastructure/DependencyInjection.cs registers concrete implementations for repositories, SignalR helpers and hosted background workers. Example registrations:

```csharp
services.AddScoped<IAlertRepository, AlertRepository>();
services.AddScoped<IRealTimeDataStreamService, LiveEnergySignalRHub>();
services.AddScoped<IHubNotificationService, HubNotificationService>();

services.AddHostedService<MockDataSimulationWorker>();
services.AddHostedService<AlertNotificationEvaluator>();
```

Controllers and endpoints

- Controllers are in the API project and follow typical patterns: annotated with [ApiController] and route attributes like [Route("api/[controller]")].
- They use constructor injection for the services they need and extract user id from JWT claims for user-scoped operations.

Example: EnergyController exposes a POST /api/Energy/snapshot endpoint which requires authorization and calls IAggregationService.GetSnapshotAsync.

```csharp
[Authorize]
[HttpPost("snapshot")]
public async Task<ActionResult<AggregationResultDto>> GetSnapshot([FromBody] AggregationRequestDto request)
{
	if (!ModelState.IsValid) return BadRequest(ModelState);
	var result = await aggregationService.GetSnapshotAsync(request);
	return Ok(result);
}
```

SignalR and real-time flow

- The SignalR hub type is UnifiedHub (energy-backend.Infrastructure/Hubs/UnifiedHub.cs) which is authorized via [Authorize]. Clients must connect with a valid JWT token.
- The hub exposes methods the client can call over the websocket connection: SubscribeToChart, UnsubscribeFromChart, SubscribeToAlerts, UnsubscribeFromAlerts.
- Authorization inside the hub checks that the connected user owns the organisation via IOrganisationRepository.GetByIdAsync.
- For sending messages to groups and clients the project uses an IHubNotificationService implementation (HubNotificationService) that uses IHubContext<UnifiedHub> to send messages or manage group membership.

HubNotificationService (representative methods):

```csharp
public async Task SendRealTimeChartUpdateAsync(string groupName, RealTimeEnergyIntervalDto data)
{
	await _hubContext.Clients.Group(groupName).SendAsync("ReceiveChartUpdate", data);
}

public async Task AddToChartGroupAsync(string connectionId, string groupName)
	=> await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
```

LiveEnergySignalRHub (IRealTimeDataStreamService implementation)

- Handles client subscriptions and receives notifications from the rest of the system when aggregates change. When aggregates are updated, it builds RealTimeEnergyIntervalDto and uses HubNotificationService to publish to relevant group(s).

Database and EF Core (EnergyDbContext)

- DbContext defines DbSet<T> for Users, Organisations, Devices, Settings, Alerts, EnergyReadings, and aggregate tables for minute/hour/day/month.
- OnModelCreating configures indexes and relationships (for example AggregateMinuteEnergy has a unique index on { DeviceId, Timestamp } and relationships to Device and Organisation).

Seeding

- Program.cs calls SeedData.SeedAggregatedEnergyDbAsync to ensure required aggregated data exists. Seeders are in energy-backend.Infrastructure.Seeding.

Background workers

- Several IHostedService implementations are registered and run in the background:
  - MockDataSimulationWorker: probably generates fake readings for development/testing.
  - AlertNotificationEvaluator: evaluates alerts and triggers notifications when thresholds are crossed.
  - RecurrentDataRollupWorker and HistoricalDownsamplingWorker: perform periodic data aggregation and downsampling.

Authentication & SignalR details

- JWT authentication is configured. The JWT token signing key must be provided via configuration key AppSettings:Token (or user-secrets / env var). If missing, the app throws on startup.
- For SignalR, the JwtBearer event OnMessageReceived reads the access_token query parameter and assigns it to context.Token for SignalR websocket auth when connecting to the path starting with /unifiedHub.

How to run the project locally

1. Ensure you have .NET 9 SDK installed.
2. Configure appsettings.json or user-secrets with a connection string named "DefaultConnection" and AppSettings:Token, AppSettings:Issuer, AppSettings:Audience.
3. From the solution directory run `dotnet ef database update --project energy-backend.Infrastructure` to apply migrations, or let migrations run on startup depending on your setup.
4. Start the API project (e.g., via Visual Studio or `dotnet run --project energy-backend`).
5. Connect the front-end at http://localhost:5173 (allowed by CORS). For SignalR, connect to ws(s)://.../unifiedHub with the JWT token as a query string parameter: `?access_token=JWT_HERE`.

Files and where to look next (map)

- energy-backend/Program.cs - entry point and app-level configuration
- energy-backend/DependencyInjection.cs - wires Application/Core/Infrastructure DI
- energy-backend/Controllers/* - REST endpoints
- energy-backend.Infrastructure/DependencyInjection.cs - repository and service registrations
- energy-backend.Infrastructure/Data/EnergyDbContext.cs - EF Core DbContext and schema config
- energy-backend.Infrastructure/Hubs/UnifiedHub.cs - SignalR hub entrypoints and user/org authorization
- energy-backend.Infrastructure/Services/HubNotificationService.cs - hub context wrapper for sending messages
- energy-backend.Infrastructure/Services/LiveEnergySignalRHub.cs - adapter implementing IRealTimeDataStreamService
- energy-backend.Infrastructure/Services/LiveAlertSignalRHub.cs - adapter implementing IAlertStreamService
- energy-backend.Infrastructure/BackgroundWorkers/* - hosted workers for data generation and aggregation
- energy-backend.Infrastructure/Repositories/* - EF repository implementations
- energy-backend.Application/* - application services, DTOs, and interfaces

Notes for a non-.NET developer

- Projects: each project in the solution compiles to a .NET assembly (DLL). The API project references the other projects and is the one you run to start the server.
- Dependency Injection: services are registered in a global service container (builder.Services) and then injected into constructors. Instead of manually creating objects, the framework provides configured instances.
- SignalR: a real-time communication system similar to websockets. Clients connect and can call hub methods; server can send messages to specific clients or groups.
- EF Core: Object-Relational Mapper. The DbContext is the main entry point to query and save data. Migrations define the schema.

If you want, I can:
- Add more file excerpts for specific classes (repositories, services, DTOs).
- Point out where to change code to add a new endpoint or a new background worker.


