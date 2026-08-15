import { useMemo, useState } from "react";
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

const DEFAULT_VIEW_OPTIONS = [
  { value: "30m", label: "30m" },
  { value: "day", label: "Day" },
  { value: "week", label: "Week" },
  { value: "month", label: "Month" },
];

const TOOLTIP_STYLE = {
  background: "var(--bg-card)",
  border: "1px solid var(--border)",
  borderRadius: "0.5rem",
  fontSize: "0.75rem",
  color: "var(--text-primary)",
};

function formatTick(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";

  return date.toLocaleString([], {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default function ConsumptionChart({
  datasetsByView,
  title = "Energy Consumption",
  unit = "kWh",
  lineColor = "#3b82f6",
  viewOptions = DEFAULT_VIEW_OPTIONS,
  defaultView = "30m",
}) {
  const [activeView, setActiveView] = useState(defaultView);

  const chartData = useMemo(() => {
    return (datasetsByView?.[activeView] ?? []).map((point) => ({
      timestamp: point.timestamp,
      value: Number(point.value ?? 0),
    }));
  }, [datasetsByView, activeView]);

  const isEmpty = chartData.length === 0 || chartData.every((d) => d.value === 0);

  return (
    <div className="stat-card flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <span style={{ fontWeight: 600, fontSize: "0.9375rem" }}>{title}</span>
        <div
          className="flex"
          style={{
            background: "var(--bg-elevated)",
            borderRadius: "var(--radius-sm)",
            border: "1px solid var(--border)",
            overflow: "hidden",
          }}
        >
          {viewOptions.map(({ value, label }) => (
            <button
              key={value}
              onClick={() => setActiveView(value)}
              style={{
                fontSize: "0.75rem",
                padding: "0.25rem 0.625rem",
                background: activeView === value ? "var(--border)" : "transparent",
                color: activeView === value ? "var(--text-primary)" : "var(--text-muted)",
                border: "none",
                cursor: "pointer",
                transition: "all 0.15s",
              }}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      {isEmpty ? (
        <div
          className="flex items-center justify-center"
          style={{ flex: 1, minHeight: 180, color: "var(--text-muted)", fontSize: "0.8125rem" }}
        >
          No data yet
        </div>
      ) : (
        <ResponsiveContainer width="100%" height={200}>
          <LineChart data={chartData} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
            <XAxis
              dataKey="timestamp"
              tick={{ fontSize: 10, fill: "var(--text-muted)" }}
              tickLine={false}
              axisLine={false}
              interval="preserveStartEnd"
              tickFormatter={formatTick}
            />
            <YAxis
              tick={{ fontSize: 10, fill: "var(--text-muted)" }}
              tickLine={false}
              axisLine={false}
              tickFormatter={(v) => Number(v).toFixed(3)}
            />
            <Tooltip
              contentStyle={TOOLTIP_STYLE}
              formatter={(value) => [`${Number(value).toFixed(4)} ${unit}`, title]}
              labelFormatter={(label) => new Date(label).toLocaleString()}
              labelStyle={{ color: "var(--text-muted)", marginBottom: "0.25rem" }}
            />
            <Line
              type="monotone"
              dataKey="value"
              stroke={lineColor}
              strokeWidth={2}
              dot={false}
              activeDot={{ r: 3, fill: lineColor }}
              isAnimationActive={false}
            />
          </LineChart>
        </ResponsiveContainer>
      )}

      <p style={{ fontSize: "0.6875rem", color: "var(--text-muted)", textAlign: "right", marginTop: "-0.5rem" }}>
        {unit}
      </p>
    </div>
  );
}
