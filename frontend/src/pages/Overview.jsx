import { useCallback, useEffect, useRef, useState } from "react";
import { useSelector } from "react-redux";
import { Link } from "react-router-dom";
import { FaBolt, FaChartBar, FaPlug, FaTachometerAlt } from "react-icons/fa";

import ConsumptionChart from "../components/ConsumptionChart";
import Features from "../components/Features";
import Hero from "../components/Hero";
import Loading from "../components/Loading";
import StatsCard from "../components/StatsCard";
import api from "../api/axios";
import { getSignalRConnection, onSignalRReconnected } from "../utils/signalr";

const WINDOW_MS = 30 * 60 * 1000; // 30-minute rolling window
const VIEW_OPTIONS = [{ value: "30m", label: "30 min" }];

function toSeries(readings, selector) {
  return readings.map((r) => ({
    timestamp: r.timestamp,
    value: selector(r) ?? 0,
  }));
}

export default function Overview() {
  const isAuthenticated = useSelector((s) => s.user.isAuthenticated);
  const selectedOrg = useSelector((s) => s.user.selectedOrgId ?? null);
  const organisations = useSelector((s) => s.user.organisations ?? []);

  const [snapshot, setSnapshot] = useState(null);
  const [connected, setConnected] = useState(false);
  const [loading, setLoading] = useState(true);
  const [latestTick, setLatestTick] = useState(null);
  const [readings, setReadings] = useState([]);

  const connectionRef = useRef(null);
  const mountedRef = useRef(false);

  const applyTick = useCallback((tick) => {
    setLatestTick(tick);
    const cutoff = new Date(tick.timestamp).getTime() - WINDOW_MS;
    setReadings((prev) => [
      ...prev.filter((r) => new Date(r.timestamp).getTime() > cutoff),
      tick,
    ]);
  }, []);

  const loadSnapshot = useCallback(async () => {
    if (!selectedOrg) return;
    const res = await api.get(`/Organisation/live/${selectedOrg}`);
    const data = res.data;
    if (!mountedRef.current) return;
    setSnapshot(data);
    const cutoff = Date.now() - WINDOW_MS;
    const snapshotReadings = (data.readings ?? []).filter(
      (r) => new Date(r.timestamp).getTime() > cutoff
    );
    setReadings(snapshotReadings);
    if (snapshotReadings.length > 0)
      setLatestTick(snapshotReadings[snapshotReadings.length - 1]);
  }, [selectedOrg]);

  const attachLiveListeners = useCallback(
    async (conn) => {
      conn.off("ReceiveLiveTick");
      conn.on("ReceiveLiveTick", applyTick);
      await conn.invoke("SubscribeToLive", selectedOrg);
      // Also subscribe to alerts so useGlobalAlerts receives events on this page
      try { await conn.invoke("SubscribeToAlerts", selectedOrg); } catch { /* non-fatal */ }
    },
    [applyTick, selectedOrg]
  );

  useEffect(() => {
    if (!isAuthenticated || !selectedOrg) return;

    mountedRef.current = true;
    setLoading(true);
    setConnected(false);
    setLatestTick(null);
    setReadings([]);

    let disposed = false;
    let removeReconnectHandler = () => {};

    async function init() {
      try {
        await loadSnapshot();
        if (disposed) return;

        const conn = await getSignalRConnection();
        if (disposed) return;

        connectionRef.current = conn;
        await attachLiveListeners(conn);
        if (disposed) return;

        setConnected(true);
        removeReconnectHandler = onSignalRReconnected(async (reconnectedConn) => {
          if (!mountedRef.current || disposed) return;
          await loadSnapshot();
          await attachLiveListeners(reconnectedConn);
          if (mountedRef.current && !disposed) setConnected(true);
        });
      } catch (error) {
        if (!disposed) {
          console.error("[Overview] Init error:", error);
          setConnected(false);
        }
      } finally {
        if (!disposed) setLoading(false);
      }
    }

    init();

    return () => {
      disposed = true;
      mountedRef.current = false;
      removeReconnectHandler();

      const conn = connectionRef.current;
      if (conn) {
        conn.off("ReceiveLiveTick");
        if (conn.state === "Connected")
          conn.invoke("UnsubscribeFromLive", selectedOrg).catch(() => {});
      }
      connectionRef.current = null;
    };
  }, [attachLiveListeners, isAuthenticated, loadSnapshot, selectedOrg]);

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex flex-col items-center justify-center py-12 px-4">
        <div className="max-w-6xl w-full space-y-12 z-10">
          <Hero />
          <Features />
        </div>
      </div>
    );
  }

  if (!selectedOrg || organisations.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-64 gap-3">
        <p style={{ color: "var(--text-secondary)" }}>No organisations found.</p>
        <p style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
          Create one in Organisations to start monitoring.
        </p>
        <Link
          to="/organisations"
          className="btn-primary"
          style={{ padding: "0.45rem 0.9rem", fontSize: "0.8125rem" }}
        >
          Create an organisation
        </Link>
      </div>
    );
  }

  if (loading && !snapshot) return <Loading />;

  const liveWatts = latestTick?.activePowerWatts ?? null;
  const liveVolts = latestTick?.voltageVolts ?? null;
  const liveAmps = latestTick?.currentAmps ?? null;
  const livePF = latestTick?.powerFactor ?? null;

  const powerSeries = { "30m": toSeries(readings, (r) => r.activePowerWatts) };
  const voltageSeries = { "30m": toSeries(readings, (r) => r.voltageVolts) };
  const currentSeries = { "30m": toSeries(readings, (r) => r.currentAmps) };
  const costSeries = { "30m": toSeries(readings, (r) => r.costRatePerHour) };

  const pfColor =
    livePF !== null ? (livePF >= 0.9 ? "green" : livePF >= 0.8 ? "amber" : "red") : "white";

  return (
    <div
      className="fade-in"
      style={{
        height: "calc(100vh - 57px)",
        display: "flex",
        flexDirection: "column",
        overflowY: "auto",
        overflowX: "hidden",
      }}
    >
      {/* Status bar */}
      <div
        className="flex items-center justify-between px-5 py-2"
        style={{ borderBottom: "1px solid var(--border)", flexShrink: 0 }}
      >
        <div className="flex items-center gap-2">
          <span
            style={{
              width: 6,
              height: 6,
              borderRadius: "50%",
              background: connected ? "var(--green)" : "var(--amber)",
              display: "inline-block",
            }}
            className={connected ? "animate-pulse-slow" : ""}
          />
          <span
            style={{
              fontSize: "0.75rem",
              color: "var(--text-muted)",
              textTransform: "uppercase",
              letterSpacing: "0.05em",
            }}
          >
            {connected ? "Live · updates every 5 s" : "Connecting..."}
          </span>
        </div>
        <span style={{ fontSize: "0.75rem", color: "var(--text-muted)" }}>
          Rolling 30-minute window
        </span>
      </div>

      {/* KPI cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-2 p-4 pb-2" style={{ flexShrink: 0 }}>
        <StatsCard
          compact
          title="Active Power"
          value={
            !connected
              ? "Connecting..."
              : liveWatts === null
              ? "Waiting..."
              : `${liveWatts.toFixed(2)} W`
          }
          icon={FaBolt}
          color="white"
          isRealTime={connected && liveWatts !== null}
        />
        <StatsCard
          compact
          title="Line Voltage"
          value={
            !connected
              ? "Connecting..."
              : liveVolts === null
              ? "Waiting..."
              : `${liveVolts.toFixed(2)} V`
          }
          icon={FaPlug}
          color="blue"
          isRealTime={connected && liveVolts !== null}
        />
        <StatsCard
          compact
          title="Total Current"
          value={
            !connected
              ? "Connecting..."
              : liveAmps === null
              ? "Waiting..."
              : `${liveAmps.toFixed(3)} A`
          }
          icon={FaTachometerAlt}
          color="amber"
          isRealTime={connected && liveAmps !== null}
        />
        <StatsCard
          compact
          title="Power Factor"
          value={
            !connected
              ? "Connecting..."
              : livePF === null
              ? "Waiting..."
              : livePF.toFixed(4)
          }
          icon={FaChartBar}
          color={pfColor}
          isRealTime={connected && livePF !== null}
        />
      </div>

      {/* line charts, 30 minutes of raw readings */}
      <div className="px-4 pb-4 space-y-3" style={{ flexShrink: 0 }}>
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-3">
          <ConsumptionChart
            title="Active Power"
            unit="W"
            lineColor="#f59e0b"
            viewOptions={VIEW_OPTIONS}
            defaultView="30m"
            datasetsByView={powerSeries}
          />
          <ConsumptionChart
            title="Line Voltage"
            unit="V"
            lineColor="#0ea5e9"
            viewOptions={VIEW_OPTIONS}
            defaultView="30m"
            datasetsByView={voltageSeries}
          />
        </div>
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-3">
          <ConsumptionChart
            title="Total Current"
            unit="A"
            lineColor="#a78bfa"
            viewOptions={VIEW_OPTIONS}
            defaultView="30m"
            datasetsByView={currentSeries}
          />
          <ConsumptionChart
            title="Cost Rate"
            unit="$/hr"
            lineColor="#22c55e"
            viewOptions={VIEW_OPTIONS}
            defaultView="30m"
            datasetsByView={costSeries}
          />
        </div>
      </div>
    </div>
  );
}
