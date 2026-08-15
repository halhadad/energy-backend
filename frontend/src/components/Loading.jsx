export default function Loading({ label = "Loading...", compact = false }) {
  return (
    <div
      className="flex flex-col items-center justify-center gap-4"
      style={{ minHeight: compact ? "8rem" : "16rem" }}
    >
      <div className="spinner" />
      {label ? (
        <span style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>{label}</span>
      ) : null}
    </div>
  );
}
