import { useSelector } from "react-redux";
import { FaBuilding, FaShieldAlt, FaUser } from "react-icons/fa";

export default function Profile() {
  const currentUser = useSelector((s) => s.user?.current_user);
  const organisations = useSelector((s) => s.user.organisations ?? []);
  const selectedOrgId = useSelector((s) => s.user.selectedOrgId ?? null);
  const selectedOrg = organisations.find((org) => org.organisationId === selectedOrgId);

  const username = currentUser?.username ?? "Guest";
  const role = currentUser?.role ?? "User";

  return (
    <div className="fade-in p-6 max-w-2xl mx-auto space-y-5">
      <div className="stat-card flex items-center gap-5">
        <div
          style={{
            width: 56,
            height: 56,
            borderRadius: "50%",
            background: "var(--bg-elevated)",
            border: "1px solid var(--border)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexShrink: 0,
          }}
        >
          <FaUser style={{ fontSize: "1.5rem", color: "var(--text-muted)" }} />
        </div>
        <div>
          <p style={{ fontWeight: 600, fontSize: "1rem", color: "var(--text-primary)" }}>
            {username}
          </p>
          <div className="flex items-center gap-1.5 mt-1">
            <FaShieldAlt style={{ fontSize: "0.6875rem", color: "var(--text-muted)" }} />
            <span style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>{role}</span>
          </div>
        </div>
      </div>

      <div className="stat-card space-y-3">
        <div className="flex items-center gap-2">
          <FaBuilding style={{ color: "var(--blue)", fontSize: "0.875rem" }} />
          <span style={{ fontWeight: 600, fontSize: "0.9375rem" }}>Selected Organisation</span>
        </div>

        {selectedOrg ? (
          <div className="space-y-2">
            <p style={{ fontSize: "0.875rem", color: "var(--text-primary)", fontWeight: 600 }}>
              {selectedOrg.name}
            </p>
            <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>
              Type: {selectedOrg.type || "Unspecified"}
            </p>
            <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>
              Devices: {selectedOrg.deviceCount ?? 0}
            </p>
            <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>
              Monthly budget: ${(selectedOrg.monthlyBudgetUsd ?? 0).toFixed(2)}
            </p>
          </div>
        ) : (
          <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)" }}>
            Create an organisation to configure energy budget and electricity rate.
          </p>
        )}
      </div>
    </div>
  );
}
