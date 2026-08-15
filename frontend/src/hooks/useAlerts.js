import { useState, useEffect, useRef, useCallback } from "react";
import { createSignalRConnection, disconnectSignalR } from "../utils/signalr";
import { fetchWithAuth } from "../utils/api";
import { useSelector, useDispatch } from "react-redux";
import { decrementActiveAlerts } from "../redux/user/userSlice";

function normalizeApiUrl(url) {
    const trimmed = (url ?? "http://localhost:5041").replace(/\/$/, "");
    return trimmed.endsWith("/api") ? trimmed : `${trimmed}/api`;
}

const API_BASE_URL = normalizeApiUrl(import.meta.env.VITE_API_URL ?? import.meta.env.VITE_API_BASE_URL);

export const useAlerts = (orgId) => {
    const [alerts, setAlerts] = useState([]);
    const [alertEvents, setAlertEvents] = useState([]);
    const [connectionStatus, setConnectionStatus] = useState("Disconnected");
    const connectionRef = useRef(null);
    const isAuthenticated = useSelector((s) => s.user.isAuthenticated);
    const dispatch = useDispatch();

    const loadData = useCallback(async () => {
        if (!isAuthenticated) return;
        try {
            const [alertsRes, eventsRes] = await Promise.all([
                fetchWithAuth(`${API_BASE_URL}/alerts`),
                fetchWithAuth(`${API_BASE_URL}/alerts/events`),
            ]);
            if (alertsRes.ok) setAlerts(await alertsRes.json());
            if (eventsRes.ok) setAlertEvents(await eventsRes.json());
        } catch (err) {
            console.error("Failed to load alerts", err);
        }
    }, [isAuthenticated]);

    useEffect(() => {
        if (!isAuthenticated || !orgId) return;
        let isMounted = true;
        let alertTriggeredHandler;
        let alertResolvedHandler;
        let alertUpdatedHandler;

        const start = async () => {
            try {
                setConnectionStatus("Connecting");
                const conn = await createSignalRConnection();
                if (!isMounted) { disconnectSignalR(conn); return; }

                connectionRef.current = conn;
                setConnectionStatus("Connected");

                // Prepend new live events; useGlobalAlerts handles beep + toast + badge
                alertTriggeredHandler = (event) => {
                    if (!isMounted) return;
                    setAlertEvents((prev) => [event, ...prev].slice(0, 50));
                    setAlerts((prev) =>
                        prev.map((a) =>
                            a.alertId === event.alertId
                                ? { ...a, isActive: true, status: "Triggered", lastTriggeredAt: event.triggeredAt }
                                : a
                            )
                    );
                };

                alertResolvedHandler = (alert) => {
                    if (!isMounted) return;
                    setAlerts((prev) =>
                        prev.map((a) =>
                            a.alertId === alert.alertId
                                ? { ...a, isActive: false, status: "Resolved", resolvedAt: alert.resolvedAt ?? new Date().toISOString() }
                                : a
                            )
                    );
                };

                alertUpdatedHandler = (alert) => {
                    if (!isMounted) return;
                    setAlerts((prev) =>
                        prev.map((a) => (a.alertId === alert.alertId ? { ...a, ...alert } : a))
                    );
                };

                conn.on("alert-triggered", alertTriggeredHandler);
                conn.on("alert-resolved", alertResolvedHandler);
                conn.on("alert-updated", alertUpdatedHandler);

                // also subscribe here as a fallback, useGlobalAlerts subscribes globally too
                try {
                    await conn.invoke("SubscribeToAlerts", orgId);
                } catch (e) {
                    console.warn("[useAlerts] SubscribeToAlerts failed:", e);
                }
            } catch (err) {
                if (isMounted && !err.message?.includes("canceled")) {
                    setConnectionStatus("Error");
                }
            }
        };

        loadData().then(() => { if (isMounted) start(); });

        return () => {
            isMounted = false;
            if (connectionRef.current) {
                if (alertTriggeredHandler) connectionRef.current.off("alert-triggered", alertTriggeredHandler);
                if (alertResolvedHandler) connectionRef.current.off("alert-resolved", alertResolvedHandler);
                if (alertUpdatedHandler) connectionRef.current.off("alert-updated", alertUpdatedHandler);
                // don't unsubscribe here, useGlobalAlerts owns the group lifetime
                disconnectSignalR(connectionRef.current);
                connectionRef.current = null;
            }
        };
    }, [isAuthenticated, orgId, loadData]);

    const createAlert = async (alertData) => {
        try {
            const res = await fetchWithAuth(`${API_BASE_URL}/alerts`, {
                method: "POST",
                body: JSON.stringify(alertData),
            });
            if (res.ok) await loadData();
        } catch (err) {
            console.error("Failed to create alert", err);
        }
    };

    const resolveAlert = async (alertId) => {
        try {
            const res = await fetchWithAuth(`${API_BASE_URL}/alerts/${alertId}/resolve`, {
                method: "POST",
            });
            if (res.ok) {
                // Optimistic local update; the global active-count is driven by the
                // alert-resolved SignalR event in useGlobalAlerts.
                setAlerts((prev) =>
                    prev.map((a) =>
                        a.alertId === alertId
                            ? { ...a, isActive: false, status: "Resolved", resolvedAt: new Date().toISOString() }
                            : a
                    )
                );
            }
        } catch (err) {
            console.error("Failed to resolve alert", err);
        }
    };

    const deleteAlert = async (alertId) => {
        try {
            const res = await fetchWithAuth(`${API_BASE_URL}/alerts/${alertId}`, {
                method: "DELETE",
            });
            if (res.ok) {
                // if the deleted alert was still triggered, clear it from the nav dot count
                const deleted = alerts.find((a) => a.alertId === alertId);
                if (deleted && (deleted.isActive || deleted.IsActive)) {
                    dispatch(decrementActiveAlerts());
                }
                setAlerts((prev) => prev.filter((a) => a.alertId !== alertId));
            }
        } catch (err) {
            console.error("Failed to delete alert", err);
        }
    };

    return { alerts, alertEvents, connectionStatus, createAlert, resolveAlert, deleteAlert, refreshAlerts: loadData };
};
