import { useState, useEffect } from "react";

const CheckRow = ({ name, checked, onChange, title, hint }) => (
  <label className="flex items-start gap-3 cursor-pointer">
    <input
      type="checkbox"
      name={name}
      checked={checked}
      onChange={onChange}
      style={{ width: 16, height: 16, marginTop: 2, accentColor: "var(--accent)", cursor: "pointer" }}
    />
    <span>
      <span style={{ display: "block", fontSize: "0.8125rem", color: "var(--text-primary)" }}>{title}</span>
      <span style={{ display: "block", fontSize: "0.75rem", color: "var(--text-muted)", marginTop: 1 }}>{hint}</span>
    </span>
  </label>
);

export default function AlertForm({ isOpen, onClose, onSubmit, initialData }) {
  const [form, setForm] = useState({
    name: "",
    threshold: "",
    inAppEnabled: true,
    emailEnabled: true,
  });

  useEffect(() => {
    setForm(
      initialData
        ? {
            name: initialData.name ?? "",
            threshold: initialData.threshold ?? "",
            inAppEnabled: initialData.inAppEnabled ?? true,
            emailEnabled: initialData.emailEnabled ?? true,
          }
        : { name: "", threshold: "", inAppEnabled: true, emailEnabled: true }
    );
  }, [initialData, isOpen]);

  if (!isOpen) return null;

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setForm((prev) => ({ ...prev, [name]: type === "checkbox" ? checked : value }));
  };

  const handleSubmit = () => {
    if (!form.threshold || !form.name) return;
    onSubmit(form);
  };

  const labelStyle = {
    fontSize: "0.8125rem",
    color: "var(--text-secondary)",
    display: "block",
    marginBottom: "0.375rem",
  };

  return (
    <div
      className="fixed inset-0 z-50 overflow-y-auto"
      style={{ background: "rgba(0,0,0,0.72)", backdropFilter: "blur(3px)" }}
    >
      <div className="flex items-center justify-center p-6" style={{ minHeight: "100vh" }}>
        <div className="surface" style={{ padding: "1.75rem", width: "100%", maxWidth: 460 }}>
          <div style={{ marginBottom: "1.25rem" }}>
            <h2 style={{ fontSize: "1rem", fontWeight: 600, color: "var(--text-primary)" }}>
              {initialData ? "Edit Alert" : "Create Alert"}
            </h2>
            <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)", marginTop: "0.25rem" }}>
              Get notified when total power crosses a threshold.
            </p>
          </div>

          <div className="space-y-4">
            <div>
              <label style={labelStyle}>Alert name</label>
              <input
                name="name"
                value={form.name}
                onChange={handleChange}
                className="form-input w-full"
                placeholder="e.g. Office Peak Warning"
              />
            </div>

            <div>
              <label style={labelStyle}>
                Threshold{" "}
                <span style={{ color: "var(--text-muted)", fontSize: "0.75rem" }}>(Watts)</span>
              </label>
              <input
                name="threshold"
                type="number"
                min="0"
                value={form.threshold}
                onChange={handleChange}
                className="form-input w-full"
                placeholder="e.g. 3000"
              />
              <p style={{ fontSize: "0.75rem", color: "var(--text-muted)", marginTop: "0.375rem" }}>
                Fires when total organisation power exceeds this value.
              </p>
            </div>

            <div>
              <span style={labelStyle}>Notify me via</span>
              <div className="space-y-3" style={{ marginTop: "0.25rem" }}>
                <CheckRow
                  name="inAppEnabled"
                  checked={form.inAppEnabled}
                  onChange={handleChange}
                  title="In-app"
                  hint="Sound and popup, repeats every 2 minutes until resolved"
                />
                <CheckRow
                  name="emailEnabled"
                  checked={form.emailEnabled}
                  onChange={handleChange}
                  title="Email"
                  hint="At most once per 24 hours, needs an address set in Settings"
                />
              </div>
            </div>
          </div>

          <div className="flex justify-end gap-3 mt-6">
            <button onClick={onClose} className="btn-ghost">
              Cancel
            </button>
            <button
              onClick={handleSubmit}
              className="btn-primary"
              style={{ padding: "0.5rem 1rem", fontSize: "0.875rem" }}
            >
              {initialData ? "Update" : "Create"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
