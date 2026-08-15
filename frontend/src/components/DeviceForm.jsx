import { useState, useEffect } from "react";

const DEVICE_TYPES = [
  "HVAC",
  "Lighting",
  "EVCharger",
  "IndustrialLoad",
  "Refrigeration",
  "ITEquipment",
  "OfficeEquipment",
  "SolarPV",
  "BatteryStorage",
  "EnergyMeter",
  "Sensor",
  "Other",
];

export default function DeviceForm({ isOpen, onClose, onSubmit, initialData }) {
  const [form, setForm] = useState({ name: "", type: "Other", description: "", ratedPowerWatts: "" });

  useEffect(() => {
    setForm(
      initialData
        ? {
          name: initialData.name ?? "",
          type: initialData.type ?? "Other",
          description: initialData.description ?? "",
          ratedPowerWatts: initialData.ratedPowerWatts ?? "",
        }
        : { name: "", type: "Other", description: "", ratedPowerWatts: "" }
    );
  }, [initialData, isOpen]);

  if (!isOpen) return null;

  const handleChange = (e) =>
    setForm((p) => ({ ...p, [e.target.name]: e.target.value }));

  const handleSubmit = () => {
    if (!form.name.trim()) return;
    onSubmit({
      ...form,
      description: form.description.trim() || null,
      ratedPowerWatts: parseFloat(form.ratedPowerWatts) || 0,
    });
  };

  return (
    <div
      style={{ background: "rgba(0,0,0,0.7)", backdropFilter: "blur(4px)" }}
      className="fixed inset-0 z-50 overflow-y-auto"
    >
      <div
        className="flex items-center justify-center p-6"
        style={{ minHeight: "100vh" }}
      >
      <div
        style={{
          background: "var(--bg-elevated)",
          border: "1px solid var(--border)",
          borderRadius: "var(--radius-lg)",
          width: "100%",
          maxWidth: 420,
          padding: "1.5rem",
        }}
      >
        <h2 style={{ fontSize: "0.9375rem", fontWeight: 600, color: "var(--text-primary)", marginBottom: "1.25rem" }}>
          {initialData ? "Edit device" : "Add device"}
        </h2>

        <div className="space-y-4">
          <div>
            <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
              Name
            </label>
            <input
              type="text" name="name" value={form.name}
              onChange={handleChange} placeholder="e.g. Office AC Unit"
              className="form-input"
            />
          </div>

          <div>
            <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
              Type
            </label>
            <select
              name="type"
              value={form.type}
              onChange={handleChange}
              className="form-input"
            >
              {DEVICE_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
              Description
            </label>
            <textarea
              name="description"
              value={form.description}
              onChange={handleChange}
              placeholder="Optional device notes"
              className="form-input"
              rows={3}
              style={{ resize: "vertical" }}
            />
          </div>

          <div>
            <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
              Rated power{" "}
              <span style={{ color: "var(--text-muted)" }}>(Watts — nameplate value)</span>
            </label>
            <input
              type="number" name="ratedPowerWatts" value={form.ratedPowerWatts}
              onChange={handleChange} placeholder="e.g. 2000"
              className="form-input"
            />
            <p style={{ fontSize: "0.75rem", color: "var(--text-muted)", marginTop: "0.375rem" }}>
              Used as the simulation baseline and the "rated" marker on the power bar.
            </p>
          </div>
        </div>

        <div className="flex justify-end gap-2 mt-6">
          <button onClick={onClose} className="btn-ghost">Cancel</button>
          <button
            onClick={handleSubmit}
            className="btn-primary"
            style={{ padding: "0.5rem 1.125rem", fontSize: "0.875rem" }}
          >
            {initialData ? "Update" : "Add device"}
          </button>
        </div>
      </div>
      </div>
    </div>
  );
}
