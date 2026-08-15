import React, { useState, useEffect } from "react";

const OrganisationForm = ({ isOpen, onClose, onSubmit, initialData }) => {
  const [form, setForm] = useState({
    name: "",
    type: "",
    monthlyBudgetUsd: "",
  });

  useEffect(() => {
    if (initialData) {
      setForm({
        name: initialData.name || "",
        type: initialData.type || "",
        monthlyBudgetUsd: initialData.monthlyBudgetUsd ?? "",
      });
    } else {
      setForm({ name: "", type: "", monthlyBudgetUsd: "" });
    }
  }, [initialData, isOpen]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = () => {
    onSubmit({
      name: form.name,
      type: form.type,
      monthlyBudgetUsd: parseFloat(form.monthlyBudgetUsd) || 0,
    });
  };

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 overflow-y-auto"
      style={{ background: "rgba(0,0,0,0.72)", backdropFilter: "blur(3px)" }}
    >
      <div className="flex items-center justify-center p-6" style={{ minHeight: "100vh" }}>
      <div
        className="surface"
        style={{
          padding: "1.75rem",
          width: "100%",
          maxWidth: 460,
        }}
      >
        <div style={{ marginBottom: "1.25rem" }}>
          <h2
            style={{
              fontSize: "1rem",
              fontWeight: 600,
              color: "var(--text-primary)",
            }}
          >
            {initialData ? "Edit Organisation" : "Add New Organisation"}
          </h2>
          <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)", marginTop: "0.25rem" }}>
            Organisation details, energy budget, and electricity rate.
          </p>
        </div>

        <div className="space-y-4">
          <div>
            <label
              style={{
                fontSize: "0.8125rem",
                color: "var(--text-secondary)",
                display: "block",
                marginBottom: "0.375rem",
              }}
            >
              Name
            </label>
            <input
              name="name"
              value={form.name}
              onChange={handleChange}
              className="form-input w-full"
              placeholder="e.g. Main Office"
            />
          </div>

          <div>
            <label
              style={{
                fontSize: "0.8125rem",
                color: "var(--text-secondary)",
                display: "block",
                marginBottom: "0.375rem",
              }}
            >
              Type
            </label>
            <input
              name="type"
              value={form.type}
              onChange={handleChange}
              className="form-input w-full"
              placeholder="e.g. Office, Warehouse, Retail"
            />
          </div>

          <div>
            <label
              style={{
                fontSize: "0.8125rem",
                color: "var(--text-secondary)",
                display: "block",
                marginBottom: "0.375rem",
              }}
            >
              Monthly Budget{" "}
              <span style={{ color: "var(--text-muted)", fontSize: "0.75rem" }}>
                (USD — leave 0 to skip)
              </span>
            </label>
            <input
              name="monthlyBudgetUsd"
              type="number"
              min="0"
              step="1"
              value={form.monthlyBudgetUsd}
              onChange={handleChange}
              className="form-input w-full"
              placeholder="e.g. 150"
            />
          </div>
        </div>

        <div className="flex justify-end gap-3 mt-6">
          <button
            onClick={onClose}
            className="btn-ghost"
          >
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            className="btn-primary"
            style={{ padding: "0.5rem 1rem", fontSize: "0.875rem" }}
          >
            {initialData ? "Update" : "Add"}
          </button>
        </div>
      </div>
      </div>
    </div>
  );
};

export default OrganisationForm;
