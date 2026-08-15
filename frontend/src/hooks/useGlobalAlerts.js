import { useEffect, useRef } from "react";
import { useDispatch, useSelector } from "react-redux";
import { toast } from "react-hot-toast";
import {
  incrementUnseenAlerts,
  setActiveAlertCount,
  incrementActiveAlerts,
  decrementActiveAlerts,
} from "../redux/user/userSlice";
import { getSignalRConnection, onSignalRReconnected } from "../utils/signalr";
import { primeAudio, playAlertBeep } from "../utils/sound";
import api from "../api/axios";

// one beep when an alert fires, then a reminder beep every 2 minutes until it is resolved
const REMINDER_INTERVAL_MS = 2 * 60 * 1000;

export function useGlobalAlerts() {
  const dispatch = useDispatch();
  const isAuthenticated = useSelector((s) => s.user.isAuthenticated);
  const activeAlertCount = useSelector((s) => s.user.activeAlertCount ?? 0);

  const connRef = useRef(null);
  const orgsRef = useRef([]);
  const triggeredHandlerRef = useRef(null);
  const resolvedHandlerRef = useRef(null);

  // let the browser play sound once the user has clicked or typed
  useEffect(() => {
    primeAudio();
  }, []);

  useEffect(() => {
    if (!isAuthenticated) return;

    let isMounted = true;
    let removeReconnect = () => {};

    const subscribeAll = async (conn) => {
      for (const org of orgsRef.current) {
        try {
          await conn.invoke("SubscribeToAlerts", org.organisationId);
        } catch (e) {
          console.warn("[GlobalAlerts] SubscribeToAlerts failed for", org.organisationId, e);
        }
      }
    };

    const start = async () => {
      try {
        // seed the active count so the nav dot and reminder beep survive a page refresh
        try {
          const alertsRes = await api.get("/alerts");
          if (!isMounted) return;
          const active = (alertsRes.data ?? []).filter((a) => a.isActive || a.IsActive).length;
          dispatch(setActiveAlertCount(active));
        } catch {
          // not fatal, the live events still update the count
        }

        const res = await api.get("/Organisation");
        if (!isMounted) return;

        orgsRef.current = res.data ?? [];
        if (!orgsRef.current.length) return;

        const conn = await getSignalRConnection();
        if (!isMounted) return;

        connRef.current = conn;

        const onTriggered = (event) => {
          if (!isMounted) return;
          playAlertBeep();
          toast.error(
            `⚡ ${event.name ?? "Alert"} — ${(event.triggeredValueWatts ?? 0).toFixed(1)} W`,
            { duration: 7000, id: `alert-${event.alertEventId ?? Date.now()}` }
          );
          dispatch(incrementUnseenAlerts());
          dispatch(incrementActiveAlerts());
        };

        const onResolved = (alert) => {
          if (!isMounted) return;
          toast.success(`Alert resolved: ${alert.name ?? alert.Name ?? ""}`, { duration: 4000 });
          dispatch(decrementActiveAlerts());
        };

        triggeredHandlerRef.current = onTriggered;
        resolvedHandlerRef.current = onResolved;

        conn.on("alert-triggered", onTriggered);
        conn.on("alert-resolved", onResolved);

        await subscribeAll(conn);
        removeReconnect = onSignalRReconnected(subscribeAll);
      } catch (err) {
        if (isMounted) console.error("[GlobalAlerts] start failed:", err);
      }
    };

    start();

    return () => {
      isMounted = false;
      removeReconnect();
      const conn = connRef.current;
      if (conn) {
        if (triggeredHandlerRef.current) conn.off("alert-triggered", triggeredHandlerRef.current);
        if (resolvedHandlerRef.current) conn.off("alert-resolved", resolvedHandlerRef.current);
      }
      connRef.current = null;
    };
  }, [isAuthenticated, dispatch]);

  // Recurring reminder beep while at least one alert is unresolved.
  useEffect(() => {
    if (activeAlertCount <= 0) return;
    const id = setInterval(() => playAlertBeep(), REMINDER_INTERVAL_MS);
    return () => clearInterval(id);
  }, [activeAlertCount]);
}
