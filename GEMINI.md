# Project: energy-backend

## Project Overview

The `energy-backend` project is an ASP.NET Core Web API designed to monitor and manage energy consumption, devices, and organizations. It provides functionalities for user authentication, real-time data aggregation, and an alert system to notify users of significant energy consumption events. The application leverages a modular architecture with distinct layers for API, Application, Core (Domain), and Infrastructure concerns.

**Key Technologies:**
*   **Backend:** ASP.NET Core Web API
*   **Database:** SQL Server (via Entity Framework Core)
*   **Authentication:** JWT Bearer Token
*   **Real-time Communication:** SignalR
*   **Background Services:** Hosted Services for data aggregation, energy reading simulation, summary backfilling, and alert monitoring.
*   **API Documentation:** OpenAPI/Scalar

## Architecture Highlights:

*   **Layered Architecture:** The project is structured into `energy-backend.Api`, `energy-backend.Application`, `energy-backend.Core`, and `energy-backend.Infrastructure` projects.
*   **Dependency Injection:** Services are registered and managed using ASP.NET Core's built-in Dependency Injection container, primarily configured in `energy-backend/Program.cs`.
*   **Data Aggregation:** A background service (`AggregationService`) runs daily to process raw energy readings into hourly aggregated data.
*   **Real-time Alerts:** An `AlertsMonitorService` periodically checks for defined alert conditions based on organizational energy consumption and uses SignalR (`SignalRAlertsNotifier`) to push real-time notifications to connected clients.
*   **Data Seeding:** Initial data for energy readings and aggregated energy is seeded into the database upon application startup.

## Building and Running

This project uses the .NET SDK.

**Prerequisites:**
*   .NET SDK (compatible with .NET 8.0, as inferred from project structure)
*   SQL Server instance (or Docker Desktop with SQL Server image)

**Steps:**

1.  **Restore Dependencies:**
    ```bash
    dotnet restore
    ```

2.  **Build the Project:**
    ```bash
    dotnet build
    ```

3.  **Apply Database Migrations (if necessary):**
    The project uses Entity Framework Core migrations. Ensure your connection string in `appsettings.json` is correctly configured.
    ```bash
    dotnet ef database update --project energy-backend.Infrastructure
    ```

4.  **Run the Application:**
    ```bash
    dotnet run --project energy-backend.Api
    ```
    The API will typically run on `https://localhost:7045` and `http://localhost:5141` by default. Check `energy-backend/Properties/launchSettings.json` for exact URLs.

5.  **Access API Documentation:**
    Once the application is running, you can access the API documentation via Scalar at `/swagger` endpoint (e.g., `https://localhost:7045/swagger`).

## Development Conventions

*   **Coding Style:** Adhere to standard C# coding conventions and practices.
*   **Testing:** (Placeholder - TODO: Add details on testing strategy if tests are found or created.)
*   **Dependency Management:** Dependencies are managed via NuGet packages.
*   **Configuration:** Application settings are managed through `appsettings.json` and environment variables. Sensitive information should be stored securely (e.g., Azure Key Vault, user secrets).
*   **CORS:** Configured to allow communication from `http://localhost:5173` for frontend development.

---
**Note for Gemini Agent:**
This `GEMINI.md` provides a foundational understanding of the project. For specific tasks, further deep dives into relevant files and documentation (like `API_DOCUMENTATION.md` or `ALERT_SYSTEM_ANALYSIS.md`) might be necessary.
