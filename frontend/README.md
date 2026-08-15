# Energy Monitor — Frontend

React dashboard for the energy monitoring backend. Shows live power readings, historical analytics with cost and carbon estimates, per-device breakdowns, and a threshold alert system with real-time push over SignalR.

## Run locally

Requires Node 18+. The backend must be running first.

```bash
npm install
npm run dev
```

Runs on `http://localhost:5173` by default.

## Environment

Create a `.env` file in the project root:

```env
VITE_API_URL=http://localhost:5041
```

If `VITE_API_URL` is not set, the app falls back to `http://localhost:5041`.

## Pages

| Route | Page | What it shows |
|---|---|---|
| `/` | Live Dashboard | Instantaneous power (W), voltage, current, power factor per phase; last-30-min chart; per-device breakdown; live cost ticker. Updates every 5s over SignalR. |
| `/analytics` | Historical Analytics | Trend chart and summary: total kWh, cost, CO₂, peak demand, month-to-date budget vs actual and projection, period-over-period comparison, per-device ranking. Presets: 24h / 7d / 30d. Updates once per minute. |
| `/organizations` | Organizations | Create and manage organisations (multi-tenant). Each org has its own devices, alerts, and electricity rate. |
| `/organizations/devices/:orgId` | Devices | Paginated device list for an org. Add, edit, delete devices. |
| `/alerts` | Alerts | Create power-threshold alerts. Active alerts show a pulsing red nav dot and play an audio beep on trigger and every 2 minutes while active. Triggered alerts stay triggered until manually resolved. Resolved alerts are terminal history. |
| `/settings` | Settings | Notification email address and master email toggle. Organisation billing rate. |
| `/login` | Login | JWT authentication. |
| `/signup` | Signup | Register with username, email, and password. |

## Real-time

A single shared SignalR connection is established after login. Hooks subscribe to org-scoped groups:

- `useGlobalAlerts` — subscribes globally, seeds active alert count on mount, plays a beep on trigger and every 2 minutes while alerts remain active
- `useAlerts` — manages the alerts page state, handles `alert-triggered` / `alert-resolved` events
- `useLiveDashboard` — receives `ReceiveLiveTick` every 5s
- `useHistoricalData` — receives `ReceiveHistoricalUpdate` once per minute

Audio uses the Web Audio API. The context is primed on the first user interaction to satisfy browser autoplay policy.

## State

Redux + redux-persist. The `user` slice stores auth tokens, organisations, selected org, and `activeAlertCount` (drives the nav dot and reminder beep interval). Persisted to `localStorage` so the session survives a page refresh.

## Stack

- React 18, Vite
- Redux Toolkit + redux-persist
- React Router v6
- Recharts
- @microsoft/signalr
- react-hot-toast
- react-icons

## Build

```bash
npm run build
```

Output goes to `dist/`. Serve with any static host (Vercel, Netlify, Cloudflare Pages).

## Production notes

- **CORS**: The backend must list the frontend origin in its CORS policy.
- **SignalR on free hosting**: Platforms that sleep idle processes (Render free, Railway free) will drop SignalR connections and stop background workers. The live feed and alert features require an always-on backend.
- **Auth tokens**: Stored in `localStorage` via redux-persist. Fine for a demo; a production app would use HttpOnly cookies.
