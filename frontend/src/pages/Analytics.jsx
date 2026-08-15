import { useCallback, useEffect, useRef, useState } from "react";
import Loading from "../components/Loading";
import { useSelector } from "react-redux";
import {
  AreaChart, Area, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from "recharts";
import {
  FaBolt, FaDollarSign, FaTachometerAlt,
  FaArrowUp, FaArrowDown,
} from "react-icons/fa";

import StatsCard from "../components/StatsCard";
import api from "../api/axios";
import { getSignalRConnection, onSignalRReconnected } from "../utils/signalr";

const PRESET_OPTIONS = [
  { key: "24h", label: "24 h" },
  { key: "7d",  label: "7 d"  },
  { key: "30d", label: "30 d" },
];

const TOOLTIP_STYLE = {
  background: "var(--bg-card)",
  border: "1px solid var(--border)",
  borderRadius: "0.5rem",
  fontSize: "0.75rem",
  color: "var(--text-primary)",
};

const PIE_COLORS = ["#3b82f6", "#f59e0b", "#22c55e", "#ef4444", "#a78bfa", "#ec4899"];

// small helpers
function Toggle({ options, value, onChange }) {
  return (
    <div
      className="flex"
      style={{
        background: "var(--bg-elevated)",
        borderRadius: "var(--radius-sm)",
        border: "1px solid var(--border)",
        overflow: "hidden",
      }}
    >
      {options.map(({ key, label }) => (
        <button
          key={key}
          onClick={() => onChange(key)}
          style={{
            fontSize: "0.6875rem",
            padding: "0.25rem 0.75rem",
            background: value === key ? "var(--border)" : "transparent",
            color: value === key ? "var(--text-primary)" : "var(--text-muted)",
            border: "none",
            cursor: "pointer",
          }}
        >
          {label}
        </button>
      ))}
    </div>
  );
}

// chart components
function EnergyBarChart({ data }) {
  const isEmpty = !data?.length || data.every((d) => (d.energy ?? 0) === 0);
  return (
    <div className="stat-card flex flex-col gap-3">
      <span style={{ fontWeight: 600, fontSize: "0.875rem" }}>Energy Consumption (kWh)</span>
      {isEmpty ? (
        <div
          className="flex items-center justify-center"
          style={{ height: 200, color: "var(--text-muted)", fontSize: "0.8125rem" }}
        >
          No data
        </div>
      ) : (
        <ResponsiveContainer width="100%" height={200}>
          <BarChart data={data} margin={{ top: 4, right: 4, left: -18, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
            <XAxis
              dataKey="label"
              tick={{ fontSize: 9, fill: "var(--text-muted)" }}
              tickLine={false}
              axisLine={false}
              interval="preserveStartEnd"
            />
            <YAxis
              tick={{ fontSize: 9, fill: "var(--text-muted)" }}
              tickLine={false}
              axisLine={false}
            />
            <Tooltip
              contentStyle={TOOLTIP_STYLE}
              cursor={{ fill: "rgba(255,255,255,0.04)" }}
              formatter={(v) => [`${Number(v).toFixed(3)} kWh`, "Energy"]}
              itemStyle={{ color: "#22c55e" }}
              labelStyle={{ color: "var(--text-muted)" }}
            />
            <Bar dataKey="energy" fill="#3b82f6" radius={[3, 3, 0, 0]} maxBarSize={20} isAnimationActive={false} />
          </BarChart>
        </ResponsiveContainer>
      )}
    </div>
  );
}

function AreaChartCard({ title, data, color, unit, gradientId }) {
  const isEmpty = !data?.length || data.every((d) => (d.value ?? 0) === 0);
  return (
    <div className="stat-card flex flex-col gap-3">
      <span style={{ fontWeight: 600, fontSize: "0.875rem" }}>{title}</span>
      {isEmpty ? (
        <div
          className="flex items-center justify-center"
          style={{ height: 160, color: "var(--text-muted)", fontSize: "0.8125rem" }}
        >
          No data
        </div>
      ) : (
        <ResponsiveContainer width="100%" height={160}>
          <AreaChart data={data} margin={{ top: 4, right: 4, left: -18, bottom: 0 }}>
            <defs>
              <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%"  stopColor={color} stopOpacity={0.25} />
                <stop offset="95%" stopColor={color} stopOpacity={0}    />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
            <XAxis
              dataKey="label"
              tick={{ fontSize: 9, fill: "var(--text-muted)" }}
              tickLine={false}
              axisLine={false}
              interval="preserveStartEnd"
            />
            <YAxis
              tick={{ fontSize: 9, fill: "var(--text-muted)" }}
              tickLine={false}
              axisLine={false}
            />
            <Tooltip
              contentStyle={TOOLTIP_STYLE}
              formatter={(v) => [
                unit ? `${Number(v).toFixed(3)} ${unit}` : Number(v).toFixed(3),
                title,
              ]}
            />
            <Area
              type="monotone"
              dataKey="value"
              stroke={color}
              strokeWidth={2}
              fill={`url(#${gradientId})`}
              dot={false}
              activeDot={{ r: 3, fill: color }}
              isAnimationActive={false}
            />
          </AreaChart>
        </ResponsiveContainer>
      )}
    </div>
  );
}

function DevicePieChart({ breakdown }) {
  const isEmpty = !breakdown?.length;
  return (
    <div className="stat-card flex flex-col gap-3">
      <span style={{ fontWeight: 600, fontSize: "0.875rem" }}>Device Breakdown</span>
      {isEmpty ? (
        <div
          className="flex items-center justify-center"
          style={{ height: 200, color: "var(--text-muted)", fontSize: "0.8125rem" }}
        >
          No data
        </div>
      ) : (
        <ResponsiveContainer width="100%" height={200}>
          <PieChart>
            <Pie
              data={breakdown}
              cx="50%"
              cy="50%"
              innerRadius={50}
              outerRadius={80}
              paddingAngle={3}
              dataKey="totalEnergyKwh"
              nameKey="deviceName"
              isAnimationActive={false}
            >
              {breakdown.map((_, i) => (
                <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
              ))}
            </Pie>
            <Tooltip
              contentStyle={TOOLTIP_STYLE}
              formatter={(v, _name, props) => [
                `${Number(v).toFixed(3)} kWh (${Number(props.payload.sharePercent).toFixed(1)}%)`,
                props.payload.deviceName,
              ]}
            />
            <Legend
              wrapperStyle={{ fontSize: "0.7rem" }}
              formatter={(_value, entry) => (
                <span style={{ fontSize: "0.7rem", color: "var(--text-muted)" }}>
                  {entry.payload.deviceName}
                </span>
              )}
            />
          </PieChart>
        </ResponsiveContainer>
      )}
    </div>
  );
}

const MONTH_NAMES = ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"];

function BudgetCard({ summary }) {
  const {
    monthToDateCostUsd  = 0,
    monthlyBudgetUsd    = 0,
    percentOfBudget     = 0,
    projectedMonthCostUsd = 0,
    monthToDateKwh      = 0,
    projectedMonthKwh   = 0,
  } = summary ?? {};

  const budget = Number(monthlyBudgetUsd);
  const spent  = Number(monthToDateCostUsd);
  const pct    = Math.min(percentOfBudget, 100);
  const barColor =
    pct > 90 ? "var(--red)" : pct > 70 ? "var(--amber)" : "var(--green)";
  const monthName = MONTH_NAMES[new Date().getMonth()];

  return (
    <div className="stat-card flex flex-col gap-3">
      <span style={{ fontWeight: 600, fontSize: "0.875rem" }}>Monthly Budget</span>

      <div className="flex justify-between" style={{ fontSize: "0.8rem" }}>
        <span style={{ color: "var(--text-muted)" }}>Spent in {monthName}</span>
        <span style={{ color: "var(--text-primary)", fontWeight: 600 }}>
          ${spent.toFixed(2)}
          {budget > 0 && (
            <span style={{ fontWeight: 400, color: "var(--text-muted)" }}>
              {" "}/ ${budget.toFixed(2)}
            </span>
          )}
        </span>
      </div>

      <div
        style={{
          background: "var(--bg-elevated)",
          borderRadius: 4,
          height: 6,
          overflow: "hidden",
        }}
      >
        <div
          style={{
            width: `${pct}%`,
            height: "100%",
            background: barColor,
            borderRadius: 4,
            transition: "width 0.3s",
          }}
        />
      </div>

      <div
        className="flex justify-between"
        style={{ fontSize: "0.75rem", color: "var(--text-muted)" }}
      >
        <span>
          {budget > 0 ? `${pct.toFixed(2)}% of $${budget.toFixed(2)} budget` : "No budget set"}
        </span>
        <span>
          Projected: ${Number(projectedMonthCostUsd).toFixed(4)}
          {" "}({projectedMonthKwh.toFixed(4)} kWh)
        </span>
      </div>
    </div>
  );
}

const PRESET_LABELS = {
  "24h": { prev: "Yesterday",  vs: "vs yesterday"  },
  "7d":  { prev: "Last week",  vs: "vs last week"  },
  "30d": { prev: "Last month", vs: "vs last month" },
};

function PeriodComparisonCard({ previous, currentKwh, preset }) {
  const labels = PRESET_LABELS[preset] ?? { prev: "Previous period", vs: "vs previous period" };

  if (!previous) {
    return (
      <div className="stat-card flex flex-col gap-3">
        <span style={{ fontWeight: 600, fontSize: "0.875rem" }}>Period Comparison</span>
        <div
          className="flex items-center justify-center"
          style={{ flex: 1, color: "var(--text-muted)", fontSize: "0.8125rem", minHeight: 60 }}
        >
          No prior period data
        </div>
      </div>
    );
  }

  const change = previous.energyChangePercent ?? 0;
  const isUp   = change > 0;

  return (
    <div className="stat-card flex flex-col items-center justify-center gap-4" style={{ textAlign: "center" }}>
      <span style={{ fontWeight: 600, fontSize: "0.875rem", alignSelf: "flex-start" }}>Period Comparison</span>

      <div className="flex flex-col items-center gap-2">
        <span style={{ color: isUp ? "var(--red)" : "var(--green)", fontSize: "2.25rem" }}>
          {isUp ? <FaArrowUp /> : <FaArrowDown />}
        </span>
        <div style={{ fontWeight: 700, fontSize: "1.5rem", color: "var(--text-primary)" }}>
          {Math.abs(change).toFixed(1)}%
        </div>
        <div style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>
          {isUp ? "more" : "less"} {labels.vs}
        </div>
      </div>

      <div style={{ fontSize: "0.75rem", color: "var(--text-muted)", lineHeight: 1.6 }}>
        <div>{labels.prev}: {previous.totalEnergyKwh.toFixed(4)} kWh · ${Number(previous.totalCost).toFixed(4)}</div>
        <div>This period: {currentKwh.toFixed(4)} kWh</div>
      </div>
    </div>
  );
}

// page
export default function Analytics() {
  const selectedOrg = useSelector((s) => s.user.selectedOrgId ?? null);

  const [apiData, setApiData]       = useState(null);
  const [loading, setLoading]       = useState(true);
  const [preset, setPreset]         = useState("7d");
  const [lastUpdated, setLastUpdated] = useState(null);

  const mountedRef    = useRef(false);
  const connectionRef = useRef(null);
  const presetRef     = useRef(preset);
  presetRef.current   = preset;

  const fetchAnalytics = useCallback(
    async (orgId, currentPreset) => {
      try {
        const res = await api.get(
          `/Organisation/historical/${orgId}?preset=${currentPreset}`
        );
        if (mountedRef.current) {
          setApiData(res.data);
          setLastUpdated(new Date());
        }
      } catch (err) {
        console.error("[Analytics] fetch failed", err);
      }
    },
    []
  );

  // Reload data whenever the org or preset changes.
  useEffect(() => {
    if (!selectedOrg) return;
    mountedRef.current = true;
    setLoading(true);
    setApiData(null);

    fetchAnalytics(selectedOrg, preset).finally(() => {
      if (mountedRef.current) setLoading(false);
    });

    return () => {
      mountedRef.current = false;
    };
  }, [selectedOrg, preset, fetchAnalytics]);

  // Subscribe to the historical "data changed" signal so the page updates automatically.
  useEffect(() => {
    if (!selectedOrg) return;

    let disposed = false;
    let removeReconnect = () => {};

    async function initSignalR() {
      try {
        const conn = await getSignalRConnection();
        if (disposed) return;

        connectionRef.current = conn;

        const onUpdate = () => {
          if (!disposed) fetchAnalytics(selectedOrg, presetRef.current);
        };

        conn.off("ReceiveHistoricalUpdate");
        conn.on("ReceiveHistoricalUpdate", onUpdate);
        await conn.invoke("SubscribeToHistorical", selectedOrg);
        try { await conn.invoke("SubscribeToAlerts", selectedOrg); } catch { /* non-fatal */ }

        removeReconnect = onSignalRReconnected(async (reconnectedConn) => {
          if (disposed) return;
          reconnectedConn.off("ReceiveHistoricalUpdate");
          reconnectedConn.on("ReceiveHistoricalUpdate", onUpdate);
          await reconnectedConn.invoke("SubscribeToHistorical", selectedOrg);
          try { await reconnectedConn.invoke("SubscribeToAlerts", selectedOrg); } catch { /* non-fatal */ }
        });
      } catch (err) {
        console.error("[Analytics] SignalR init error:", err);
      }
    }

    initSignalR();

    return () => {
      disposed = true;
      removeReconnect();
      const conn = connectionRef.current;
      if (conn?.state === "Connected") {
        conn.off("ReceiveHistoricalUpdate");
        conn.invoke("UnsubscribeFromHistorical", selectedOrg).catch(() => {});
      }
      connectionRef.current = null;
    };
  }, [selectedOrg, fetchAnalytics]);

  // empty and loading states
  if (!selectedOrg) {
    return (
      <div className="flex items-center justify-center h-64">
        <span style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
          Select an organisation to view analytics.
        </span>
      </div>
    );
  }

  if (loading || !apiData) {
    return <Loading label="Loading analytics..." />;
  }

  // data extraction
  const summary   = apiData.summary ?? {};
  const breakdown = apiData.deviceBreakdown ?? [];

  // 24h shows hour rollups, 7d and 30d show day rollups
  const rollups =
    preset === "24h" ? (apiData.hourRollups ?? []) : (apiData.dayRollups ?? []);

  const barData     = rollups.map((r) => ({ label: r.label, energy: r.totalEnergyKwh }));
  const costData    = rollups.map((r) => ({ label: r.label, value: Number(r.estimatedCost) }));
  const powerData   = rollups.map((r) => ({ label: r.label, value: r.averageActivePowerWatts }));
  const voltageData = rollups.map((r) => ({ label: r.label, value: r.averageVoltageVolts }));
  const currentData = rollups.map((r) => ({ label: r.label, value: r.averageCurrentAmps }));
  const pfData      = rollups.map((r) => ({ label: r.label, value: r.averagePowerFactor }));

  return (
    <div
      className="fade-in p-5 space-y-4 overflow-y-auto"
      style={{ maxHeight: "calc(100vh - 57px)" }}
    >
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Toggle options={PRESET_OPTIONS} value={preset} onChange={setPreset} />
          {lastUpdated && (
            <span style={{ fontSize: "0.7rem", color: "var(--text-muted)" }}>
              Updated {lastUpdated.toLocaleTimeString()}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          <span
            style={{
              width: 6,
              height: 6,
              borderRadius: "50%",
              background: "var(--green)",
              display: "inline-block",
            }}
            className="animate-pulse-slow"
          />
          <span style={{ fontSize: "0.7rem", color: "var(--text-muted)" }}>
            Live updates
          </span>
        </div>
      </div>

      {/* 4-card top row: 3 KPI chips + budget card (wider) */}
      <div className="grid gap-3" style={{ gridTemplateColumns: "1fr 1fr 1fr 2fr" }}>
        <StatsCard
          title={`Energy (${preset})`}
          value={`${(summary.totalEnergyKwh ?? 0).toFixed(4)} kWh`}
          icon={FaBolt}
          color="white"
        />
        <StatsCard
          title={`Cost (${preset})`}
          value={`$${Number(summary.totalCost ?? 0).toFixed(4)}`}
          icon={FaDollarSign}
          color="green"
        />
        <StatsCard
          title="Peak Demand"
          value={`${(summary.peakPowerWatts ?? 0).toFixed(2)} W`}
          icon={FaTachometerAlt}
          color="red"
        />
        <BudgetCard summary={summary} />
      </div>

      {/* Energy bar chart (wider) + period comparison (narrower, centered) */}
      <div className="grid gap-4" style={{ gridTemplateColumns: "3fr 2fr" }}>
        <EnergyBarChart data={barData} />
        <PeriodComparisonCard
          previous={summary.previousPeriod}
          currentKwh={summary.totalEnergyKwh ?? 0}
          preset={preset}
        />
      </div>

      {/* Cost + power area charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <AreaChartCard
          title="Estimated Cost ($)"
          data={costData}
          color="#22c55e"
          unit="$"
          gradientId="grad-cost"
        />
        <AreaChartCard
          title="Avg Active Power (W)"
          data={powerData}
          color="#f59e0b"
          unit="W"
          gradientId="grad-power"
        />
      </div>

      {/* Voltage + current area charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <AreaChartCard
          title="Avg Voltage (V)"
          data={voltageData}
          color="#0ea5e9"
          unit="V"
          gradientId="grad-voltage"
        />
        <AreaChartCard
          title="Avg Current (A)"
          data={currentData}
          color="#a78bfa"
          unit="A"
          gradientId="grad-current"
        />
      </div>

      {/* Device breakdown pie + power factor */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <DevicePieChart breakdown={breakdown} />
        <AreaChartCard
          title="Power Factor"
          data={pfData}
          color="#ec4899"
          unit=""
          gradientId="grad-pf"
        />
      </div>
    </div>
  );
}
