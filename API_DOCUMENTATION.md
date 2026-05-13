# Energy Backend - Complete API Documentation

## Overview
**Base URL:** `http://localhost:5041`  
**API Version:** .NET 9  
**Authentication:** JWT Bearer Token (except public endpoints)

---

## 📋 Table of Contents
1. [Authentication APIs](#authentication-apis)
2. [Organisation APIs](#organisation-apis)
3. [Device APIs](#device-apis)
4. [Energy APIs](#energy-apis)
5. [SignalR Hubs](#signalr-hubs)
6. [OpenAPI/Scalar Documentation](#openapiscalar-documentation)
7. [CORS Configuration](#cors-configuration)
8. [Authentication Details](#authentication-details)
9. [DTOs & Models](#dtos--models)
10. [Background Services](#background-services)
11. [Database Seeding](#database-seeding)
12. [Example Requests](#example-requests)
13. [Status Codes Summary](#status-codes-summary)

---

## 🔐 Authentication APIs

### Base Route: `/api/Auth`

> **Note:** The route is capitalized (`Auth`) as per ASP.NET Core routing conventions

#### 1. **User Registration**
- **Endpoint:** `POST /api/Auth/register`
- **Description:** Register a new user account
- **Authentication:** None (Public)
- **Request Body:**
```json
{
	"username": "string",
	"password": "string"
}
```
- **Response:** 
  - **Success (200):** User object
  - **Error (400):** "Username already exists"

#### 2. **User Login**
- **Endpoint:** `POST /api/Auth/login`
- **Description:** Login with credentials to receive JWT tokens
- **Authentication:** None (Public)
- **Request Body:**
```json
{
	"username": "string",
	"password": "string"
}
```
- **Response:**
  - **Success (200):** `TokenResponseDto`
```json
{
	"accessToken": "string",
	"refreshToken": "string"
}
```
  - **Error (400):** "Invalid credentials"

#### 3. **Refresh Token**
- **Endpoint:** `POST /api/Auth/refresh-token`
- **Description:** Refresh expired access token using refresh token
- **Authentication:** None (Public)
- **Request Body:**
```json
{
	"refreshToken": "string"
}
```
- **Response:**
  - **Success (200):** `TokenResponseDto` with new tokens
  - **Error (401):** "Invalid refresh token"

#### 4. **Authenticated User Check**
- **Endpoint:** `GET /api/Auth`
- **Description:** Verify user is authenticated (requires valid JWT)
- **Authentication:** Required (Bearer Token)
- **Response:**
  - **Success (200):** "You are authenticated!"

#### 5. **Admin Only Endpoint**
- **Endpoint:** `GET /api/Auth/admin-only`
- **Description:** Admin-only access endpoint
- **Authentication:** Required (Bearer Token with Admin role)
- **Response:**
  - **Success (200):** "You are in an admin only room!"

---

## 🏢 Organisation APIs

### Base Route: `/api/Organisation`
**Authentication:** Required (Bearer Token)

> **Note:** The route is capitalized (`Organisation`) as per ASP.NET Core routing conventions

#### 1. **Get All Organisations**
- **Endpoint:** `GET /api/Organisation`
- **Description:** Retrieve all organisations owned by authenticated user
- **Response (200):** `List<Organisation>`
- **Error (400):** "Error fetching organisations"
- **Error (401):** "Invalid User."

#### 2. **Create Organisation**
- **Endpoint:** `POST /api/Organisation`
- **Description:** Create a new organisation
- **Request Body:**
```json
{
	"name": "string (required)",
	"type": "string (required)"
}
```
- **Response (200):** `OrganisationResponseDto`
- **Error (400):** "Organisation name and type are required." or "Error creating organisation"
- **Error (401):** "Invalid User."

#### 3. **Update Organisation**
- **Endpoint:** `PUT /api/Organisation/{organisationId}`
- **Parameters:**
  - `organisationId` (Guid, path parameter, required)
- **Request Body:**
```json
{
	"name": "string",
	"type": "string"
}
```
- **Response (200):** `OrganisationResponseDto`
- **Error (400):** "Invalid organisation data."
- **Error (401):** "Invalid User."
- **Error (404):** "Organisation not found"

#### 4. **Delete Organisation**
- **Endpoint:** `DELETE /api/Organisation/{organisationId}`
- **Parameters:**
  - `organisationId` (Guid, path parameter, required)
- **Response (200):** `true`
- **Error (400):** "Invalid organisation ID."
- **Error (401):** "Invalid User."
- **Error (404):** "Organisation not found"

#### 5. **Check if User Has Organisation**
- **Endpoint:** `GET /api/Organisation/HasOrganisation`
- **Description:** Check if authenticated user has at least one organisation
- **Response (200):** `boolean`
- **Error (400):** Error message
- **Error (401):** "Invalid User."

#### 6. **Get Organisation Analytics**
- **Endpoint:** `GET /api/Organisation/GetOrganisationAnalytics/{organisationId}`
- **Parameters:**
  - `organisationId` (Guid, path parameter, required)
- **Description:** Retrieve analytics data for a specific organisation
- **Response (200):** `OrganisationAnalyticsDto`
- **Error (400):** Error message
- **Error (401):** "Invalid User."

---

## 📱 Device APIs

### Base Route: `/api/Device`
**Authentication:** Required (Bearer Token)

> **Note:** The route is capitalized (`Device`) as per ASP.NET Core routing conventions

#### 1. **Get All Devices**
- **Endpoint:** `GET /api/Device`
- **Description:** Retrieve all devices for authenticated user
- **Response (200):** `List<DeviceResponseDto>`
- **Error (401):** "Invalid user."

#### 2. **Get Devices by Organisation**
- **Endpoint:** `GET /api/Device/byOrganisation/{orgId}`
- **Parameters:**
  - `orgId` (Guid, path parameter, required)
- **Description:** Get all devices in a specific organisation
- **Response (200):** `List<DeviceResponseDto>`
- **Error (401):** "Invalid user."

#### 3. **Get Device by ID**
- **Endpoint:** `GET /api/Device/{deviceId}`
- **Parameters:**
  - `deviceId` (Guid, path parameter, required)
- **Response (200):** `DeviceResponseDto`
- **Error (401):** "Invalid user."
- **Error (404):** Device not found

#### 4. **Create Device**
- **Endpoint:** `POST /api/Device`
- **Description:** Create a new device
- **Request Body:**
```json
{
	"name": "string",
	"type": "string",
	"organisationId": "uuid"
}
```
- **Response (200):** `DeviceResponseDto`
- **Error (400):** "Invalid organisation or data."
- **Error (401):** "Invalid user."

#### 5. **Update Device**
- **Endpoint:** `PUT /api/Device/{deviceId}`
- **Parameters:**
  - `deviceId` (Guid, path parameter, required)
- **Request Body:**
```json
{
	"name": "string",
	"type": "string",
	"organisationId": "uuid"
}
```
- **Response (200):** `DeviceResponseDto`
- **Error (401):** "Invalid user."
- **Error (404):** Device not found

#### 6. **Delete Device**
- **Endpoint:** `DELETE /api/Device/{deviceId}`
- **Parameters:**
  - `deviceId` (Guid, path parameter, required)
- **Response (200):** Success message
- **Error (401):** "Invalid user."
- **Error (404):** Device not found

---

## ⚡ Energy APIs

### Base Route: `/api/Energy`

> **Note:** The route is capitalized (`Energy`) as per ASP.NET Core routing conventions

#### 1. **Get Energy Data**
- **Endpoint:** `GET /api/Energy`
- **Description:** Retrieve energy data (simulated)
- **Response (200):** `Energy` object
- **Note:** Limited implementation, used for basic energy data retrieval

---

## 📡 SignalR Hubs

### 1. **RealTime Hub**
- **Route:** `/overviewHub`
- **Authentication:** Required (JWT token via query string `?access_token=...`)
- **Protocol:** WebSocket

**Connection Requirements:**
- Query Parameter: `access_token` (JWT token)
- Claims Expected:
  - `orgId` or `org` (from claims or query string)
  - `NameIdentifier` or `sub` (User ID)

**Methods (Client → Server):**

#### `RequestOverviewNow()`
- **Description:** Request immediate overview data
- **Parameters:** None
- **Response:** Emits `ReceiveOverviewData` event

**Events (Server → Client):**

#### `ReceiveOverviewData`
- **Description:** Real-time overview data push (every 5 seconds or on demand)
- **Data Model:** Real-time overview DTO with:
  - Device status breakdown
  - Time series energy consumption
  - Statistics (avg, min, max)
  - Current real-time values

#### `Error`
- **Description:** Error messages from server
- **Data:** Error description string

---

### 2. **Alerts Hub**
- **Route:** `/hubs/alerts`
- **Authentication:** Required (JWT token via query string `?access_token=...`)
- **Protocol:** WebSocket

**Connection Requirements:**
- Query Parameter: `access_token` (JWT token)
- Claims Expected:
  - `NameIdentifier` (User ID)

**Methods (Client → Server):**

#### `Subscribe(alertId)`
- **Description:** Subscribe to alerts for a specific alert ID
- **Parameters:**
  - `alertId` (Guid) - The alert to subscribe to
- **Response:** Task completion

#### `Unsubscribe(alertId)`
- **Description:** Unsubscribe from alerts for a specific alert ID
- **Parameters:**
  - `alertId` (Guid) - The alert to unsubscribe from
- **Response:** Task completion

**Events (Server → Client):**

#### Server-pushed events
- The hub may emit alert-related events to subscribed clients
- Listen for alert updates from the `AlertsMonitorService`

**Connection Lifecycle:**
- On disconnect, the hub automatically removes the connection tracker
- Use `Subscribe()` to start receiving alerts for a specific alert ID
- Use `Unsubscribe()` to stop receiving alerts

---

## 📚 OpenAPI/Scalar Documentation

### Scalar API Reference
- **Route:** `/scalar/v1`
- **Description:** Interactive API documentation with schema explorer
- **Availability:** Development environment only
- **Features:**
  - Browse all endpoints
  - Try API calls directly
  - View request/response schemas
  - Authentication testing

### OpenAPI Specification
- **Route:** `/openapi/v1.json`
- **Description:** OpenAPI 3.0 specification
- **Availability:** Development environment only

---

## 🔄 CORS Configuration

**Allowed Origins:** `http://localhost:5173`  
**Allowed Methods:** Any  
**Allowed Headers:** Any  
**Allow Credentials:** Yes

---

## 🔑 Authentication Details

### JWT Token
- **Header:** `Authorization: Bearer <token>`
- **Issuer:** Configured in `AppSettings:Issuer`
- **Audience:** Configured in `AppSettings:Audience`
- **Secret Key:** Configured in `AppSettings:Token`
- **Lifetime:** Configured per token type

### Claims Expected
- `NameIdentifier` or `sub`: User ID (Guid)
- `orgId` or `org`: Organisation ID (Guid, for RealTimeHub)
- `alertId`: Alert ID (Guid, for AlertsHub)
- Additional custom claims as configured

---

## 🗂️ DTOs & Models

### Key DTOs:
- **UserRequestDto:** `{ username, password }`
- **TokenResponseDto:** `{ accessToken, refreshToken }`
- **RefreshTokenRequestDto:** `{ refreshToken }`
- **OrganisationRequestDto:** `{ name, type }`
- **OrganisationResponseDto:** Organisation with full details
- **OrganisationAnalyticsDto:** Organization analytics data
- **DeviceRequestDto:** `{ name, type, organisationId }`
- **DeviceResponseDto:** Device with full details
- **SignalR DTOs:**
  - `BreakdownDto`: Device status breakdown
  - `TimeSeriesDto`: Time series data
  - `StatsDto`: Statistical data
  - `RealTimeDto`: Real-time values

---

## 🚀 Background Services

The backend includes the following hosted background services:

1. **AggregationService**
   - Aggregates energy readings from 5-second intervals
   - Creates hourly energy consumption summaries
   - Runs continuously in the background

2. **EnergyReadingSimulator**
   - Simulates realistic energy consumption data
   - Generates 5-second interval readings
   - Useful for testing and development

3. **SummaryBackfillService**
   - Backfills missing device consumption summaries
   - Ensures data consistency
   - Runs on a schedule

4. **AlertsMonitorService**
   - Monitors alerts and their conditions
   - Notifies users via SignalR when alerts trigger
   - Maintains alert state and history

---

## 📊 Database Seeding

On application startup, the database is seeded with:
- 5-second interval energy readings
- Hourly aggregated energy data
- Device consumption summaries

This ensures the application has test data ready for development and testing.

---

## 🔗 Example Requests

### Example 1: Register & Login
```sh
# Register
POST http://localhost:5041/api/Auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "password": "SecurePass123!"
}

# Login
POST http://localhost:5041/api/Auth/login
Content-Type: application/json

{
  "username": "john_doe",
  "password": "SecurePass123!"
}
```

### Example 2: Create Organisation
```sh
POST http://localhost:5041/api/Organisation
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "name": "Acme Corp",
  "type": "Commercial"
}
```

### Example 3: Create Device
```sh
POST http://localhost:5041/api/Device
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "name": "Solar Panel 1",
  "type": "Solar",
  "organisationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### Example 4: Get Organisation Analytics
```sh
GET http://localhost:5041/api/Organisation/GetOrganisationAnalytics/550e8400-e29b-41d4-a716-446655440000
Authorization: Bearer <access_token>
```

### Example 5: Connect to RealTime Hub (JavaScript)
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5041/overviewHub?access_token=" + accessToken)
  .withAutomaticReconnect()
  .build();

connection.on("ReceiveOverviewData", (data) => {
  console.log("Real-time data:", data);
});

connection.start().catch(err => console.error(err));

// Request data on demand
connection.invoke("RequestOverviewNow").catch(err => console.error(err));
```

### Example 6: Connect to Alerts Hub (JavaScript)
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5041/hubs/alerts?access_token=" + accessToken)
  .withAutomaticReconnect()
  .build();

connection.start().catch(err => console.error(err));

// Subscribe to a specific alert
const alertId = "550e8400-e29b-41d4-a716-446655440000";
connection.invoke("Subscribe", alertId).catch(err => console.error(err));

// Unsubscribe from an alert
connection.invoke("Unsubscribe", alertId).catch(err => console.error(err));
```

---

## ✅ Status Codes Summary

| Code | Usage |
|------|-------|
| 200 | Success - Request completed successfully |
| 400 | Bad Request - Validation error or invalid data |
| 401 | Unauthorized - Invalid or missing authentication token |
| 404 | Not Found - Resource does not exist |
| 500 | Server Error - Internal server error |

---

## 🛠️ Development & Testing

### Using Scalar API Reference
1. Run the application in development mode
2. Navigate to `http://localhost:5041/scalar/v1`
3. Test endpoints directly with authentication
4. View live API documentation

### Using .http Files
The repository includes an `energy-backend.http` file for testing with REST Client extensions in Visual Studio Code or Visual Studio.

---

## 🔧 Configuration

Key configuration settings are stored in `appsettings.json`:

```json
{
  "AppSettings": {
	"Token": "your-secret-key-here",
	"Issuer": "your-issuer",
	"Audience": "your-audience"
  },
  "ConnectionStrings": {
	"DefaultConnection": "Server=...;Database=...;..."
  }
}
```

---

## 📝 Notes

- All timestamps are in UTC
- All IDs are GUIDs (Globally Unique Identifiers)
- Real-time updates push data every 5 seconds unless requested immediately
- SignalR connections are kept alive with automatic reconnection
- Database migrations are applied automatically on startup
- Sensitive data (tokens, credentials) should never be logged or exposed

---

**Last Updated:** May 12, 2026  
**Repository:** https://github.com/halhadad/energy-backend  
**Target Framework:** .NET 9  
**Language:** C# 13.0
