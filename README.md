# Energy Monitor

Full-stack IoT energy monitoring app: live dashboard, historical analytics, and threshold alerts over a real-time feed.

- **`/`** — the backend (this directory): .NET 9 REST API + SignalR hub, documented below.
- **[`frontend/`](frontend/README.md)** — the React dashboard that consumes it.

## Backend

.NET 9 REST API and real-time push server for an energy monitoring dashboard. Models a Shelly 3EM three-phase energy monitor: simulates live readings, rolls them up into aggregate buckets, evaluates power-threshold alerts, and streams everything to connected clients over SignalR.

### Run locally

Requires .NET 9 SDK and a SQL Server instance (or SQL Server LocalDB).

**1. Set required secrets**

```bash
dotnet user-secrets set "AppSettings:Token" "<jwt-signing-key-min-64-chars>" --project energy-backend
dotnet user-secrets set "Email:Username" "<mailtrap-username>" --project energy-backend
dotnet user-secrets set "Email:Password" "<mailtrap-password>" --project energy-backend
```

`AppSettings:Token` must be at least 64 characters (HMAC-SHA512 key). The app throws on startup if it is missing or too short.

**2. Set the connection string**

In `energy-backend/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=EnergyDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

**3. Run**

```bash
dotnet run --project energy-backend
```

The API starts on `http://localhost:5041`. Schema is created automatically via `EnsureCreated` on first run — no migrations step needed.

### Architecture

Four projects; dependencies flow inward:

```
energy-backend          (Api)           controllers, JWT auth, CORS, SignalR hub, hosted workers
energy-backend.Application              orchestrators, business services, DTOs, AutoMapper profiles, interfaces
energy-backend.Infrastructure           EF Core DbContext, repositories, background workers, email, auth adapters
energy-backend.Core                     domain entities, enums, repository interfaces, shared contracts
```

Each layer owns a `DependencyInjection.cs`. Register new services in the matching layer's file.

### Data model

Raw `EnergyReading` rows (5-second cadence) are rolled up into four pre-computed aggregate tiers:

```
EnergyReading -> AggregateMinuteEnergy -> AggregateHourEnergy -> AggregateDayEnergy -> AggregateMonthEnergy
```

Each aggregate has a unique index on `{DeviceId, Timestamp}`. All kWh accounting derives from `ActiveEnergyKwh` on the reading — never a hardcoded interval constant, so the simulator interval can change freely.

### Background workers

| Worker | What it does |
|---|---|
| `MockDataSimulationWorker` | Generates readings every `SimulatorIntervalSeconds` and updates minute buckets |
| `LiveBroadcastWorker` | Pushes the live SignalR tick on its own timer, independent of the simulator |
| `HistoricalDownsamplingWorker` | Promotes minute buckets up to hour / day / month |
| `AlertNotificationEvaluator` | Evaluates power thresholds and triggers in-app and email notifications |

Each tier has exactly one writer. The downsampler does not write minute buckets; the simulator worker does not write hour/day/month.

### Real-time (SignalR)

Single hub `UnifiedHub` at `/unifiedHub`, JWT-gated. Clients subscribe to groups by org:

- `live:org:{orgId}` — raw 5s ticks (`ReceiveLiveTick`)
- `chart:org:{orgId}:{preset}` — aggregate rollup updates (`ReceiveChartUpdate`)
- `historical:org:{orgId}` — once-per-minute summary signal (`ReceiveHistoricalUpdate`)
- `alerts:org:{orgId}` — alert trigger / resolve events (`alert-triggered`, `alert-resolved`)

JWT auth for WebSockets: the hub reads `access_token` from the query string since browsers cannot set `Authorization` headers on WS connections.

### Alert state machine

```
Monitoring -> Triggered  (power crosses threshold while alert is not yet active)
Triggered  -> Resolved   (manual resolve only; sets ResolvedAt — terminal, never re-fires)
```

The evaluator skips any alert where `ResolvedAt is not null`. Email notifications are gated by a 24-hour per-alert cooldown and the org owner's `Setting.RequireEmail` flag.

### Configuration

| Section | Key | Default | Notes |
|---|---|---|---|
| `AppSettings` | `Token` | — | Required. JWT signing key, min 64 chars |
| `AppSettings` | `Issuer`, `Audience` | — | JWT claims |
| `ConnectionStrings` | `DefaultConnection` | — | SQL Server connection string |
| `EnergyWorkers` | `SimulatorIntervalSeconds` | 5 | Reading cadence |
| `EnergyWorkers` | `BroadcastIntervalSeconds` | 5 | Live push cadence |
| `EnergyWorkers` | `DownsamplerLookbackHours` | 2 | How far back the downsampler looks |
| `EnergyWorkers` | `AlertEvaluationIntervalSeconds` | 5 | Alert check cadence |
| `EnergySettings` | `CostPerKwh` | 0.28 | Default electricity rate |
| `EnergySettings` | `CarbonKgPerKwh` | 0.233 | Carbon factor for CO₂ estimates |
| `Email` | `Provider` | `Log` | `Log` (dev) or `Smtp` |
| `Email` | `Host`, `Port`, `Username`, `Password` | — | SMTP credentials (user-secrets in dev) |

### Tests

A self-contained console harness — no xUnit, no external packages, builds offline.

```bash
dotnet run --project energy-backend.Tests
```

Covers page-orchestration logic, aggregate math, and alert cooldown gating. Exits non-zero on failure.

### Production notes

- **Database**: Uses `EnsureCreated` — fine for demos, use migrations for production.
- **Schema changes**: New columns added to a live DB need a manual `ALTER TABLE` since there are no migrations.
- **Email**: `LoggingEmailSender` is the dev default (logs to console). Switch `Email:Provider` to `Smtp` and supply SMTP credentials for real delivery. Tested against Mailtrap sandbox.
- **PostgreSQL**: Swap `Microsoft.EntityFrameworkCore.SqlServer` for `Npgsql.EntityFrameworkCore.PostgreSQL` and change `UseSqlServer` to `UseNpgsql` in `Infrastructure/DependencyInjection.cs`. Username comparisons are case-sensitive on PostgreSQL by default.
- **JWT key**: Generate a random 64+ byte string. Do not check it into source control — use user-secrets or the `AppSettings__Token` environment variable.
