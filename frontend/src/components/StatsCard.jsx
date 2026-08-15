import { createElement } from "react";

const ICON_COLORS = {
  white: "var(--text-primary)",
  green: "var(--green)",
  amber: "var(--amber)",
  red: "var(--red)",
  purple: "var(--purple)",
  blue: "var(--blue)",
};

function progressColor(pct, ratedPct) {
  if (!ratedPct || ratedPct === 0) {
    // no rated reference, use a neutral fill
    return "var(--text-muted)";
  }
  const ratio = pct / ratedPct; // 1.0 means exactly at rated

  if (ratio <= 1.0) return "var(--green)";
  if (ratio <= 1.30) return "var(--amber)";
  return "var(--red)";
}

export default function StatsCard({
  title,
  value,
  icon,
  color = "white",
  progress,
  ratedProgress,
  isRealTime = false,
  limit,
  compact = false,
  showRatedMarker = true,
  overLimitOnly = false,
}) {
  const showBar = progress !== undefined;
  const fillColor = showBar
    ? (overLimitOnly
      ? (progress > 100 ? "var(--red)" : "var(--green)")
      : progressColor(progress, ratedProgress))
    : null;
  const isOverHigh = showBar && progress > 100;

  return (
    <div
      className="stat-card flex flex-col h-full"
      style={{
        gap: compact ? "0.45rem" : "0.75rem",
        padding: compact ? "0.6rem 0.85rem" : undefined,
      }}
    >
      {/* Header row */}
      <div className="flex items-start justify-between">
        <p style={{ fontSize: "0.75rem", color: "var(--text-muted)", fontWeight: 500, textTransform: "uppercase", letterSpacing: "0.05em" }}>
          {title}
        </p>

        <div
          style={{
            background: "var(--bg-elevated)",
            border: "1px solid var(--border)",
            color: ICON_COLORS[color] ?? "var(--text-primary)",
            borderRadius: "var(--radius-sm)",
            height: compact ? 28 : 32,
            width: compact ? 28 : 32,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexShrink: 0,
          }}
        >
          {icon ? createElement(icon, { style: { fontSize: "0.875rem" } }) : null}
        </div>
      </div>

      <div className="flex-1 flex items-start" style={{ paddingTop: compact ? "0.1rem" : "0.2rem" }}>
        <h3
          style={{
            fontSize: compact ? "1.05rem" : "1.375rem",
            fontWeight: 600,
            letterSpacing: "-0.02em",
            marginTop: compact ? "0.15rem" : "0.25rem",
            color: isRealTime ? "var(--text-primary)" : "var(--text-primary)",
          }}
          className={isRealTime ? "animate-pulse-slow" : ""}
        >
          {value}
          {limit && (
            <span style={{ fontSize: "0.8125rem", fontWeight: 400, color: "var(--text-muted)", marginLeft: "0.375rem" }}>
              / {limit}
            </span>
          )}
        </h3>
      </div>

      {/* Progress bar */}
      {showBar && (
        <div style={{ position: "relative", marginTop: "auto" }}>
          <div className="progress-track" style={{ height: compact ? 5 : 6 }}>
            <div
              style={{
                width: `${Math.min(progress, 100)}%`,
                background: fillColor,
                boxShadow: isOverHigh ? "0 0 12px rgba(239,68,68,0.8)" : "none",
              }}
              className="progress-fill-green"  /* class just for the transition */
            />
          </div>

          {/* Rated midpoint marker */}
          {showRatedMarker && ratedProgress !== undefined && ratedProgress > 0 && ratedProgress <= 100 && (
            <div
              style={{
                position: "absolute",
                left: `${ratedProgress}%`,
                top: "50%",
                transform: "translate(-50%, -50%)",
                width: 2,
                height: 10,
                background: "var(--text-muted)",
                borderRadius: 1,
              }}
              title="Rated power"
            />
          )}

        </div>
      )}
    </div>
  );
}
