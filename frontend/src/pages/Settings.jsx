import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import { FaBuilding, FaBell } from "react-icons/fa";
import { toast } from "react-hot-toast";
import api from "../api/axios";
import Loading from "../components/Loading";

export default function Settings() {
  const organisations = useSelector((s) => s.user.organisations ?? []);
  const selectedOrgId = useSelector((s) => s.user.selectedOrgId ?? null);
  const selectedOrg = organisations.find((org) => org.organisationId === selectedOrgId);

  const [email, setEmail] = useState("");
  const [requireEmail, setRequireEmail] = useState(true);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const res = await api.get("/settings");
        if (!mounted) return;
        setEmail(res.data?.email ?? "");
        setRequireEmail(res.data?.requireEmail ?? true);
      } catch {
        /* leave defaults */
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => { mounted = false; };
  }, []);

  const handleSave = async () => {
    setSaving(true);
    try {
      const res = await api.patch("/settings", { email, requireEmail });
      setEmail(res.data?.email ?? email);
      setRequireEmail(res.data?.requireEmail ?? requireEmail);
      toast.success("Settings saved");
    } catch (err) {
      toast.error(err.response?.data ?? "Could not save settings");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fade-in p-6 max-w-2xl mx-auto space-y-4">
      <h1 style={{ fontSize: "1.25rem", fontWeight: 600 }}>Settings</h1>

      {/* Notifications */}
      <section className="stat-card">
        <div className="flex items-center gap-2 mb-3">
          <FaBell style={{ color: "var(--amber)", fontSize: "0.875rem" }} />
          <span style={{ fontWeight: 600, fontSize: "0.9375rem" }}>Alert Notifications</span>
        </div>

        {loading ? (
          <Loading compact label="" />
        ) : (
          <div className="space-y-4">
            <div>
              <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
                Notification email
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                className="form-input w-full"
              />
              <p style={{ fontSize: "0.6875rem", color: "var(--text-muted)", marginTop: "0.25rem" }}>
                Where alert emails are sent. Leave blank to disable email entirely.
              </p>
            </div>

            <label className="flex items-center gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={requireEmail}
                onChange={(e) => setRequireEmail(e.target.checked)}
                style={{ width: 16, height: 16, accentColor: "var(--blue)" }}
              />
              <span style={{ fontSize: "0.875rem", color: "var(--text-primary)" }}>
                Email me when an alert fires
              </span>
            </label>
            <p style={{ fontSize: "0.6875rem", color: "var(--text-muted)" }}>
              Master switch. Each alert also has its own email toggle. Emails are sent at most once
              per 24 h per alert.
            </p>

            <button
              onClick={handleSave}
              disabled={saving}
              className="btn-primary"
              style={{ fontSize: "0.8125rem", padding: "0.45rem 0.9rem", opacity: saving ? 0.6 : 1 }}
            >
              {saving ? "Saving…" : "Save changes"}
            </button>
          </div>
        )}
      </section>

      {/* Organisation billing */}
      <section className="stat-card">
        <div className="flex items-center gap-2 mb-3">
          <FaBuilding style={{ color: "var(--blue)", fontSize: "0.875rem" }} />
          <span style={{ fontWeight: 600, fontSize: "0.9375rem" }}>Organisation Billing</span>
        </div>

        {selectedOrg ? (
          <div className="space-y-2">
            <p style={{ fontSize: "0.875rem", color: "var(--text-primary)", fontWeight: 600 }}>
              {selectedOrg.name}
            </p>
            <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>
              Electricity rate is configured per organisation.
            </p>
            <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>
              Current rate: $
              {(selectedOrg.electricityCostPerKwh ?? selectedOrg.costPerKwh ?? 0).toFixed(4)}/kWh
            </p>
          </div>
        ) : (
          <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>
            Create an organisation to configure energy budget and electricity rate.
          </p>
        )}
      </section>
    </div>
  );
}
