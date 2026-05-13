# Alert System - Data Flow & Architecture Diagrams

## 1. Complete Alert System Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ALERT SYSTEM FLOW                               │
└─────────────────────────────────────────────────────────────────────────┘

USER SIDE (CREATE ALERT)
────────────────────────
	User
	  │
	  │ POST /api/Alerts
	  │ { name, threshold, orgId }
	  │
	  ▼
 ┌─────────────────────────┐
 │  AlertsController       │
 │  CreateAlert()          │
 └──────────┬──────────────┘
			│
			│ AlertsService.CreateAlertAsync()
			│
			▼
 ┌─────────────────────────┐
 │  AlertRepository        │
 │  AddAsync(alert)        │
 └──────────┬──────────────┘
			│
			▼
	┌──────────────────┐
	│  Database        │
	│  Alert table     │
	└──────────────────┘


MONITORING SIDE (BACKGROUND SERVICE)
───────────────────────────────────────
	Every 5 seconds
	  │
	  ▼
 ┌──────────────────────────────┐
 │  AlertsMonitorService        │
 │  ExecuteAsync()              │
 │  CheckAlertsAsync()          │
 └──────────┬───────────────────┘
			│
			│ Gets Alerts with Organisations & Devices
			│
			▼
	┌──────────────────────┐
	│  Database            │
	│  Alert (+ related)   │
	└──────────┬───────────┘
			   │
			   │ Calculate: energy = sum(device.EnergyConsumption)
			   │
			   ▼
		┌─────────────────────┐
		│ Check Threshold     │
		└─────────────────────┘

		┌──────────────────────────┐
		│ energy > alert.Threshold?│
		└──────────┬───────────────┘
				   │
		┌──────────┴──────────┐
		│ YES                 │ NO
		│                     │
		▼                     ▼
  ┌──────────────┐    ┌────────────────────┐
  │ !IsActive?   │    │ IsActive?          │
  └──────┬───────┘    └────────┬───────────┘
		 │                     │
		 │ YES                 │ YES
		 │                     │
		 ▼                     ▼
  ┌────────────────────┐ ┌──────────────────────┐
  │ Set IsActive=true  │ │ Set IsActive=false   │
  │ Create AlertEvent  │ │ (Reset @ 90%)        │
  │ Save to DB         │ └──────────┬───────────┘
  └────────┬───────────┘            │
		   │                        ▼
		   │                   ┌──────────┐
		   │                   │ Done     │
		   │                   └──────────┘
		   │
		   │ Call notifier.NotifyAsync()
		   │
		   ▼
  ┌──────────────────────────────────┐
  │ SignalRAlertsNotifier            │
  │ NotifyAsync(alertId, alertEvent) │
  └──────────┬───────────────────────┘
			 │
			 │ Get connections for alert
			 │
			 ▼
  ┌──────────────────────────────────┐
  │ AlertsConnectionTracker          │
  │ GetConnectionsForAlert(alertId)  │
  └──────────┬───────────────────────┘
			 │
			 ▼
  ┌──────────────────────────────────┐
  │ For each connected client:       │
  │ SendAsync("AlertTriggered", evt) │
  └──────────┬───────────────────────┘
			 │
			 ▼
	   WebSocket
	   (SignalR Hub)
			 │
			 ▼
	  ┌─────────────┐
	  │ Client App  │
	  │ Shows Alert │
	  └─────────────┘


CLIENT SIDE (RECEIVE ALERTS)
────────────────────────────
	Client App
	  │
	  │ Connect to /hubs/alerts?access_token=...
	  │
	  ▼
  ┌─────────────────────┐
  │ AlertsHub           │
  │ OnConnectedAsync()  │
  └────────┬────────────┘
		   │
		   │ Subscribe(alertId)
		   │
		   ▼
  ┌──────────────────────────────────┐
  │ AlertsConnectionTracker          │
  │ Add(connectionId, userId, alert) │
  └──────────┬───────────────────────┘
			 │
			 ▼
	Connection registered
	Awaiting alerts...
			 │
			 │ (from monitoring side)
			 │ "AlertTriggered" message
			 │
			 ▼
	┌─────────────────────┐
	│ Client receives     │
	│ alert notification  │
	│ Shows to user       │
	└─────────────────────┘
```

---

## 2. Class Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                    INTERFACE HIERARCHY                           │
└─────────────────────────────────────────────────────────────────┘

IAlertService (interface)
	│
	└── AlertsService (implementation)
		├── GetActiveAlertsAsync()
		├── CreateAlertAsync()
		└── DeleteAlertAsync()

IAlertsNotifier (interface)
	│
	└── SignalRAlertsNotifier (implementation)
		└── NotifyAsync(alertId, evt, ct)


┌─────────────────────────────────────────────────────────────────┐
│                    CLASS DEPENDENCIES                            │
└─────────────────────────────────────────────────────────────────┘

AlertsMonitorService (BackgroundService)
	│
	├── depends on: EnergyDbContext
	├── depends on: IAlertsNotifier
	└── uses: AlertsConnectionTracker

AlertsController
	│
	└── depends on: IAlertService

SignalRAlertsNotifier
	│
	├── depends on: IHubContext<AlertsHub>
	└── depends on: AlertsConnectionTracker

AlertsHub (SignalR Hub)
	│
	└── depends on: AlertsConnectionTracker


┌─────────────────────────────────────────────────────────────────┐
│                    ENTITY RELATIONSHIPS                          │
└─────────────────────────────────────────────────────────────────┘

Organisation
	│
	├── 1 ──→ N (Alerts)
	│         └── Alert
	│             ├── AlertId (PK)
	│             ├── Threshold
	│             ├── IsActive
	│             ├── LastTriggeredAt
	│             └── 1 ──→ N (AlertEvents)
	│                       └── AlertEvent
	│                           ├── AlertEventId (PK)
	│                           ├── TriggeredEnergy
	│                           ├── TriggeredAt
	│
	└── 1 ──→ N (Devices)
			└── Device
				└── EnergyConsumption (used for threshold check)
```

---

## 3. State Machine: Alert Lifecycle

```
┌──────────────────────────────────────────────────────────────────┐
│                    ALERT STATE MACHINE                            │
└──────────────────────────────────────────────────────────────────┘

						   ┌─────────────────┐
						   │   INACTIVE      │
						   │  (IsActive=F)   │
						   └────────┬────────┘
									│
					┌───────────────┘
					│
					│ Energy > Threshold
					│ & !IsActive
					│
					▼
			┌─────────────────┐
			│  TRIGGERED      │
			│  (IsActive=T)   │
			│                 │
			│ Actions:        │
			│ • Set IsActive  │
			│ • Create Event  │
			│ • Notify users  │
			│ • Log timestamp │
			└────────┬────────┘
					 │
					 │ Energy < Threshold * 0.9
					 │ (Hysteresis at 90%)
					 │
					 ▼
			┌─────────────────┐
			│  RESOLVED       │
			│  (IsActive=F)   │
			└────────┬────────┘
					 │
					 └─────────────────────────────────────┐
														   │
														   ▼
											┌──────────────────────┐
											│    Ready for next    │
											│    trigger (back to  │
											│    INACTIVE)         │
											└──────────────────────┘


KEY CONCEPT: Hysteresis
──────────────────────
Why not use same threshold for both on and off?

Off threshold = Threshold × 0.9 (prevents rapid on/off flapping)

Example:
  Threshold = 100
  On trigger:  Energy > 100  ✓
  Off trigger: Energy < 90   ✓ (not 100)

This prevents oscillating around the boundary.
```

---

## 4. Real-time Message Flow

```
┌──────────────────────────────────────────────────────────────────┐
│               SIGNALR MESSAGE SEQUENCE DIAGRAM                    │
└──────────────────────────────────────────────────────────────────┘

CLIENT 1                      HUB                    MONITOR SERVICE
─────────                     ───                    ───────────────
   │                           │                            │
   │─── Connect ──────────────→│                            │
   │                           │                            │
   │←──── Connected ───────────│                            │
   │                           │                            │
   │─── Subscribe(alertId1) ──→│                            │
   │                           │                            │
   │      Add to tracker       │                            │
   │      (connId1, alert1)    │                            │
   │                           │                            │
   │                           │        Every 5 sec         │
   │                           │←────── Check alerts ──────│
   │                           │                            │
   │                           │    Alert 1 triggered!     │
   │                           │←──── NotifyAsync() ──────│
   │                           │                            │
   │←─ "AlertTriggered" ──────│                            │
   │   (evt details)           │                            │
   │                           │                            │
   │  (Client processes)       │                            │
   │                           │                            │
   │─── Unsubscribe ────────→│                            │
   │                           │                            │
   │      Remove from          │                            │
   │      tracker              │                            │
   │                           │                            │
   │─── Disconnect ────────→│                            │
   │                           │                            │
   │    Cleanup                │                            │
   │    (OnDisconnected)       │                            │
   │                           │                            │


CLIENT 2                      HUB                    MONITOR SERVICE
─────────                     ───                    ───────────────
   │                           │                            │
   │─── Connect ──────────────→│                            │
   │                           │                            │
   │←──── Connected ───────────│                            │
   │                           │                            │
   │─── Subscribe(alertId1) ──→│                            │
   │                           │                            │
   │      Add to tracker       │                            │
   │      (connId2, alert1)    │                            │
   │                           │                            │
   │                           │                            │
   │←─ "AlertTriggered" ──────│ (same alert as Client 1)   │
   │   (evt details)           │←──── NotifyAsync() ──────│
   │                           │      (targets both)       │
   │  (Client processes)       │                            │


SAME ALERT, MULTIPLE SUBSCRIBERS:
──────────────────────────────────
AlertsMonitorService detects: Energy > Alert1.Threshold
NotifyAsync(alertId=Alert1)
  ├─ GetConnectionsForAlert(Alert1)
  │  └─ Returns [connId1, connId2]
  │
  ├─ Send to connId1
  ├─ Send to connId2
  └─ (But NOT to Client 3 who subscribed to Alert2)
```

---

## 5. Alert Creation & Monitoring Timeline

```
┌──────────────────────────────────────────────────────────────────┐
│                    TIMELINE EXAMPLE                               │
└──────────────────────────────────────────────────────────────────┘

T=0s    User creates Alert: name="Power Surge", threshold=500kW

		POST /api/Alerts
		→ AlertsService.CreateAlertAsync()
		→ Saved to database
		→ Returns AlertId = "abc123"

T=1s    Monitoring cycle 1
		• Checks all alerts
		• Alert "abc123": Energy = 300kW (< 500kW)
		• Status: INACTIVE ✓

T=2s    Client subscribes to alert

		WebSocket /hubs/alerts?access_token=...
		→ AlertsHub.Subscribe("abc123")
		→ AlertsConnectionTracker.Add("conn-001", "user-xyz", "abc123")
		→ Now listening for alerts

T=5s    Monitoring cycle 2
		• Alert "abc123": Energy = 450kW (< 500kW)
		• Status: INACTIVE ✓
		• No notification sent

T=10s   User's device turns on!
		Energy spike detected

T=11s   Monitoring cycle 3 (at 11s mark, checks every 5s)
		• Alert "abc123": Energy = 750kW (> 500kW)
		• Previous IsActive = false
		• TRIGGER! ✓
		• Set IsActive = true
		• Create AlertEvent record
		• Call notifier.NotifyAsync("abc123", evt)

		SignalRAlertsNotifier:
		• Get connections: ["conn-001"]
		• Send "AlertTriggered" to client
		• Message includes: {
			  AlertEventId: "evt-001",
			  AlertId: "abc123",
			  Name: "Power Surge",
			  TriggeredEnergy: 750,
			  TriggeredAt: "2026-05-12T14:11:00Z"
		  }

		Client receives!
		• Shows notification to user: "Power Surge! 750kW > 500kW"

T=15s   Monitoring cycle 4
		• Energy drops to 650kW (> 500kW)
		• Still active ✓
		• No new notification (already triggered)

T=20s   Energy drops to 450kW (< 500 × 0.9 = 450)

T=21s   Monitoring cycle 5
		• Energy = 450kW (< 450)
		• IsActive = true (from previous trigger)
		• RESET! ✓
		• Set IsActive = false
		• Ready for next trigger
		• No notification (reset is silent)

T=25s   Device turns on again!
		Energy = 600kW

T=26s   Monitoring cycle 6
		• Energy = 600kW (> 500kW)
		• IsActive = false (was reset)
		• TRIGGER AGAIN! ✓
		• Creates new AlertEvent
		• Notifies subscribers
```

---

## 6. Connection Tracking in Memory

```
┌──────────────────────────────────────────────────────────────────┐
│          ALERTSCONNECTIONTRACKER IN-MEMORY STATE                  │
└──────────────────────────────────────────────────────────────────┘

Dictionary<string, (Guid UserId, Guid AlertId)>

┌─────────────────────────────────────────────────────┐
│ INITIAL STATE (Empty)                               │
├─────────────────────────────────────────────────────┤
│ _connections = {}                                   │
└─────────────────────────────────────────────────────┘

					↓ (Clients connect)

┌─────────────────────────────────────────────────────┐
│ Client 1 subscribes to Alert A                      │
│ Client 2 subscribes to Alert A                      │
│ Client 3 subscribes to Alert B                      │
├─────────────────────────────────────────────────────┤
│ _connections = {                                    │
│   "conn-001": (User1, AlertA),                      │
│   "conn-002": (User2, AlertA),                      │
│   "conn-003": (User3, AlertB)                       │
│ }                                                   │
└─────────────────────────────────────────────────────┘

					↓ (Alert A triggers)

┌─────────────────────────────────────────────────────┐
│ GetConnectionsForAlert(AlertA)                      │
│ → Filters by Value.AlertId == AlertA                │
│ → Returns ["conn-001", "conn-002"]                  │
│ → Sends notification to both                        │
│ → Client 3 NOT notified (different alert)           │
├─────────────────────────────────────────────────────┤
│ Notification: to conn-001 (User1)                   │
│              to conn-002 (User2)                    │
│              NOT to conn-003 (different alert)      │
└─────────────────────────────────────────────────────┘

					↓ (Client 1 disconnects)

┌─────────────────────────────────────────────────────┐
│ OnDisconnectedAsync("conn-001")                     │
│ → Remove("conn-001")                                │
├─────────────────────────────────────────────────────┤
│ _connections = {                                    │
│   "conn-002": (User2, AlertA),                      │
│   "conn-003": (User3, AlertB)                       │
│ }                                                   │
└─────────────────────────────────────────────────────┘

THREAD SAFETY: Uses lock (_lock) around all access
─────────────────────────────────────────────
• Add() - locked
• Remove() - locked
• GetConnectionsForAlert() - locked
• Prevents race conditions in multi-threaded env
```

---

## 7. Error Scenarios & Recovery

```
┌──────────────────────────────────────────────────────────────────┐
│                    ERROR HANDLING FLOW                            │
└──────────────────────────────────────────────────────────────────┘

SCENARIO 1: Database Error in Monitoring
──────────────────────────────────────────
	AlertsMonitorService.CheckAlertsAsync()
		│
		│ var alerts = await db.Alerts.ToListAsync()
		│ [SQL Connection timeout!]
		│
		├─ ❌ Current: Exception thrown, cycle stops
		└─ ✅ Recommended: Try-catch, continue next cycle

SCENARIO 2: Client Disconnects During Notification
──────────────────────────────────────────────────
	SignalRAlertsNotifier.NotifyAsync()
		│
		│ SendAsync("AlertTriggered", evt) to conn-001
		│ [Connection closed!]
		│
		├─ ✅ SignalR handles gracefully
		└─ ✅ Client removed on next disconnect check

SCENARIO 3: Client Doesn't Receive Message
─────────────────────────────────────────
	Client loses network connection
		│
		├─ AlertsMonitorService still runs
		├─ Still creates AlertEvent records
		├─ Tries to notify (connection doesn't exist)
		│
		├─ ✅ SignalR handles silently
		└─ When client reconnects, can query history

SCENARIO 4: Alert Threshold Changes Mid-Check
──────────────────────────────────────────────
	Thread 1: CheckAlertsAsync() reads alert.Threshold = 500
	Thread 2: Update alert.Threshold = 600
	Thread 1: Uses old value 500

	├─ ✅ Safe: EF tracks version
	├─ Slight delay in new threshold effect
	└─ Next cycle uses new value
```

---

## Summary: How Alerts Work

1. **Creation**: User creates alert via REST API
2. **Monitoring**: Background service checks every 5 seconds
3. **Triggering**: When energy > threshold, create AlertEvent & notify
4. **Reset**: When energy < threshold × 0.9, mark inactive
5. **Notification**: Find subscribed clients via AlertsConnectionTracker
6. **Delivery**: Send "AlertTriggered" message via SignalR
7. **Client**: WebSocket receives and displays to user

**Key Pattern**: Threshold + Hysteresis + State + Real-time Push = Robust Alert System ✨
