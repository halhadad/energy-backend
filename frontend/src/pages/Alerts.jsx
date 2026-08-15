import { useState } from "react";
import { FaBell, FaPlusCircle, FaExclamationTriangle, FaTrash, FaCheck } from "react-icons/fa";
import AlertForm from "../components/AlertForm";
import { useAlerts } from "../hooks/useAlerts";
import { useSelector } from "react-redux";

const Alerts = () => {
  const [showForm, setShowForm] = useState(false);

  // org comes from redux, no local fetch needed
  const selectedOrg = useSelector((s) => s.user.selectedOrgId ?? null);

  const { alerts, alertEvents, connectionStatus, createAlert, resolveAlert, deleteAlert } =
    useAlerts(selectedOrg);

  const handleAddAlert = async (newAlert) => {
    if (!selectedOrg) return;
    await createAlert({
      name: newAlert.name || "Power Alert",
      organisationId: selectedOrg,
      threshold: parseFloat(newAlert.threshold),
      emailEnabled: newAlert.emailEnabled ?? true,
      inAppEnabled: newAlert.inAppEnabled ?? true,
    });
    setShowForm(false);
  };

  const handleDelete = async (id) => {
    if (window.confirm("Delete this alert?")) {
      await deleteAlert(id);
    }
  };

  const statusDot = {
    Connected: "var(--green)",
    Connecting: "var(--amber)",
    Error: "var(--red)",
    Disconnected: "var(--red)",
  }[connectionStatus] ?? "var(--amber)";

  return (
    <div className="fade-in p-5 space-y-4 overflow-y-auto" style={{ maxHeight: "calc(100vh - 57px)" }}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-1.5">
            <span
              style={{
                width: 6, height: 6, borderRadius: "50%",
                background: statusDot, display: "inline-block",
              }}
              className={connectionStatus === "Connected" ? "animate-pulse-slow" : ""}
            />
            <span style={{ fontSize: "0.75rem", color: "var(--text-muted)", textTransform: "uppercase", letterSpacing: "0.05em" }}>
              {connectionStatus === "Connected" ? "Live" : connectionStatus}
            </span>
          </div>
        </div>
        <button
          onClick={() => setShowForm(true)}
          disabled={!selectedOrg}
          className="btn-primary"
          style={{
            fontSize: "0.8125rem",
            padding: "0.375rem 0.875rem",
            display: "flex",
            alignItems: "center",
            gap: "0.375rem",
            opacity: !selectedOrg ? 0.5 : 1,
            cursor: !selectedOrg ? "not-allowed" : "pointer",
          }}
        >
          <FaPlusCircle style={{ fontSize: "0.75rem" }} />
          Create Alert
        </button>
      </div>

      {!selectedOrg && (
        <div style={{
          background: "rgba(245,158,11,.08)", border: "1px solid var(--amber)",
          borderRadius: "var(--radius-sm)", padding: "0.625rem 0.875rem",
          fontSize: "0.8125rem", color: "var(--amber)",
        }}>
          Select an organisation in the sidebar to manage alerts.
        </div>
      )}

      {/* Configured Alerts */}
      <div className="stat-card">
        <div className="flex items-center gap-2 mb-3">
          <FaBell style={{ color: "var(--blue)", fontSize: "0.875rem" }} />
          <span style={{ fontWeight: 600, fontSize: "0.9375rem" }}>Configured Alerts</span>
          {alerts.length > 0 && (
            <span style={{
              background: "var(--bg-elevated)", border: "1px solid var(--border)",
              borderRadius: "999px", fontSize: "0.6875rem", fontWeight: 600,
              padding: "0.125rem 0.5rem", color: "var(--text-muted)",
            }}>
              {alerts.length}
            </span>
          )}
        </div>

        {alerts.length === 0 ? (
          <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)", padding: "1rem 0" }}>
            No alerts configured. Create one to start monitoring power thresholds.
          </p>
        ) : (
          <div className="space-y-2">
            {alerts.map((alert) => {
              const status =
                alert.status ??
                (alert.isActive || alert.IsActive
                  ? "Triggered"
                  : alert.resolvedAt || alert.ResolvedAt
                  ? "Resolved"
                  : "Monitoring");
              const statusColor =
                status === "Triggered"
                  ? "var(--red)"
                  : status === "Resolved"
                  ? "var(--text-muted)"
                  : "var(--green)";
              const statusBg =
                status === "Triggered"
                  ? "rgba(239,68,68,.12)"
                  : status === "Resolved"
                  ? "rgba(148,163,184,.12)"
                  : "rgba(34,197,94,.1)";
              return (
              <div
                key={alert.alertId}
                style={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  padding: "0.75rem",
                  background: "var(--bg-elevated)",
                  border: "1px solid var(--border)",
                  borderRadius: "var(--radius-sm)",
                  borderLeft: `3px solid ${statusColor}`,
                  opacity: status === "Resolved" ? 0.7 : 1,
                }}
              >
                <div>
                  <p style={{ fontWeight: 500, fontSize: "0.875rem" }}>
                    {alert.name || alert.Name || "Alert"}
                  </p>
                  <p style={{ fontSize: "0.75rem", color: "var(--text-muted)", marginTop: "0.125rem" }}>
                    Threshold: <span style={{ color: "var(--amber)" }}>
                      {(alert.threshold ?? alert.Threshold ?? 0).toFixed(1)} W
                    </span>
                    {(alert.lastTriggeredAt || alert.LastTriggeredAt) && (
                      <span style={{ marginLeft: "0.75rem" }}>
                        Last: {new Date(alert.lastTriggeredAt || alert.LastTriggeredAt).toLocaleString()}
                      </span>
                    )}
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <span style={{
                    fontSize: "0.6875rem", fontWeight: 600, padding: "0.2rem 0.5rem",
                    borderRadius: "999px",
                    background: statusBg,
                    color: statusColor,
                    border: `1px solid ${statusColor}`,
                  }}>
                    {status}
                  </span>
                  {status === "Triggered" && (
                    <button
                      onClick={() => resolveAlert(alert.alertId)}
                      title="Resolve alert"
                      style={{
                        display: "flex", alignItems: "center", gap: "0.3rem",
                        fontSize: "0.6875rem", fontWeight: 600,
                        padding: "0.25rem 0.6rem", borderRadius: "999px",
                        background: "var(--green)", color: "#0b1f12",
                        border: "none", cursor: "pointer",
                      }}
                    >
                      <FaCheck style={{ fontSize: "0.625rem" }} />
                      Resolve
                    </button>
                  )}
                  <button
                    onClick={() => handleDelete(alert.alertId)}
                    style={{
                      background: "none", border: "none", cursor: "pointer",
                      color: "var(--text-muted)", fontSize: "0.8125rem", padding: "0.25rem",
                    }}
                    onMouseEnter={(e) => (e.currentTarget.style.color = "var(--red)")}
                    onMouseLeave={(e) => (e.currentTarget.style.color = "var(--text-muted)")}
                    title="Delete"
                  >
                    <FaTrash />
                  </button>
                </div>
              </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Recent trigger events */}
      <div className="stat-card">
        <div className="flex items-center gap-2 mb-3">
          <FaExclamationTriangle style={{ color: "var(--amber)", fontSize: "0.875rem" }} />
          <span style={{ fontWeight: 600, fontSize: "0.9375rem" }}>Recent Triggers</span>
          {alertEvents.length > 0 && (
            <span style={{
              background: "var(--bg-elevated)", border: "1px solid var(--border)",
              borderRadius: "999px", fontSize: "0.6875rem", fontWeight: 600,
              padding: "0.125rem 0.5rem", color: "var(--text-muted)",
            }}>
              {alertEvents.length}
            </span>
          )}
        </div>

        {alertEvents.length === 0 ? (
          <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)", padding: "1rem 0" }}>
            No triggered events yet.
          </p>
        ) : (
          <div
            className="space-y-2"
            style={{ maxHeight: "24rem", overflowY: "auto" }}
          >
            {[...alertEvents].reverse().map((event, idx) => (
              <div
                key={event.alertEventId ?? `event-${idx}`}
                style={{
                  padding: "0.75rem",
                  background: "var(--bg-elevated)",
                  border: "1px solid var(--border)",
                  borderRadius: "var(--radius-sm)",
                  borderLeft: "3px solid var(--red)",
                }}
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p style={{ fontWeight: 500, fontSize: "0.875rem", color: "var(--red)" }}>
                      {event.name || event.Name}
                    </p>
                    <p style={{ fontSize: "0.75rem", color: "var(--text-muted)", marginTop: "0.25rem" }}>
                      Power at trigger:{" "}
                      <span style={{ color: "var(--text-primary)", fontWeight: 600 }}>
                        {(event.triggeredValueWatts ?? event.TriggeredValueWatts ?? 0).toFixed(1)} W
                      </span>
                      <span style={{ margin: "0 0.5rem", color: "var(--border)" }}>|</span>
                      Threshold: {(event.thresholdValue ?? event.ThresholdValue ?? 0).toFixed(1)} W
                    </p>
                  </div>
                  <span style={{ fontSize: "0.6875rem", color: "var(--text-muted)", whiteSpace: "nowrap", flexShrink: 0 }}>
                    {(event.triggeredAt || event.TriggeredAt)
                      ? new Date(event.triggeredAt || event.TriggeredAt).toLocaleString()
                      : "Just now"}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <AlertForm
        isOpen={showForm}
        onClose={() => setShowForm(false)}
        onSubmit={handleAddAlert}
      />
    </div>
  );
};

export default Alerts;
