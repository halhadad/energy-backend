import { useState, useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { Link, useLocation, useNavigate } from "react-router-dom";
import {
  FaChartPie, FaChartLine, FaBuilding, FaBell, FaUserCog, FaCog, FaSignOutAlt,
} from "react-icons/fa";
import { logout, clearUnseenAlerts, setSelectedOrg } from "../redux/user/userSlice";
import api from "../api/axios";

export default function Sidebar() {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const unseenAlertCount = useSelector((s) => s.user.unseenAlertCount ?? 0);
  const activeAlertCount = useSelector((s) => s.user.activeAlertCount ?? 0);
  const organisations = useSelector((s) => s.user.organisations ?? []);
  const selectedOrgId = useSelector((s) => s.user.selectedOrgId ?? null);
  const [serverStatus, setServerStatus] = useState("Connecting");

  useEffect(() => {
    if (pathname === "/alerts") dispatch(clearUnseenAlerts());
  }, [pathname, dispatch]);

  useEffect(() => {
    let mounted = true;
    const check = async () => {
      try {
        await api.get("/Organisation");
        if (mounted) setServerStatus("Online");
      } catch (err) {
        if (mounted) setServerStatus(err.response?.status === 401 ? "Online" : "Offline");
      }
    };
    check();
    const id = setInterval(check, 30_000);
    return () => { mounted = false; clearInterval(id); };
  }, []);

  const link = (to) => {
    const active = pathname === to || (to !== "/" && pathname.startsWith(to));
    return `nav-item${active ? " active" : ""}`;
  };

  const handleLogout = () => {
    setTimeout(() => { dispatch(logout()); navigate("/"); }, 200);
  };

  const statusColor = {
    Online: "var(--green)",
    Offline: "var(--red)",
    Connecting: "var(--amber)",
  }[serverStatus] ?? "var(--amber)";

  return (
    <aside
      style={{
        background: "var(--bg-surface)",
        borderRight: "1px solid var(--border)",
      }}
      className="w-64 h-screen fixed left-0 top-0 flex flex-col p-4 z-40"
    >
      {/* Logo */}
      <div className="flex items-center gap-3 mb-6 mt-2 px-1">
        <div
          style={{ background: "var(--bg-elevated)", border: "1px solid var(--border)" }}
          className="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
        >
          <img src="https://i.ibb.co/9SQCrH7/Volty2.png" className="w-5 h-5" alt="Denki" />
        </div>
        <span style={{ color: "var(--text-primary)", fontWeight: 600, fontSize: "1rem", letterSpacing: "-0.01em" }}>
          Denki
        </span>
      </div>

      {/* Organisation selector */}
      {organisations.length > 0 && (
        <div style={{ marginBottom: "1rem" }}>
          <p style={{
            fontSize: "0.6875rem", color: "var(--text-muted)", textTransform: "uppercase",
            letterSpacing: "0.06em", marginBottom: "0.375rem", paddingLeft: "0.5rem",
          }}>
            Organisation
          </p>
          <select
            value={selectedOrgId ?? ""}
            onChange={(e) => dispatch(setSelectedOrg(e.target.value))}
            className="form-input w-full"
            style={{ fontSize: "0.8125rem", padding: "0.375rem 0.625rem" }}
          >
            {organisations.map((org) => (
              <option key={org.organisationId} value={org.organisationId}>
                {org.name}
              </option>
            ))}
          </select>
        </div>
      )}

      {/* Navigation */}
      <nav className="flex-1 space-y-0.5">
        <Link to="/" className={link("/")}>
          <FaChartPie style={{ marginRight: "0.625rem", fontSize: "0.875rem" }} />
          Overview
        </Link>
        <Link to="/analytics" className={link("/analytics")}>
          <FaChartLine style={{ marginRight: "0.625rem", fontSize: "0.875rem" }} />
          Analytics
        </Link>
        <Link to="/organisations" className={link("/organisations")}>
          <FaBuilding style={{ marginRight: "0.625rem", fontSize: "0.875rem" }} />
          Organisations
        </Link>
        <Link to="/alerts" className={link("/alerts")}>
          <FaBell style={{ marginRight: "0.625rem", fontSize: "0.875rem" }} />
          Alerts
          {activeAlertCount > 0 ? (
            // Persistent pulsing dot while alerts are triggered (unresolved).
            <span
              title={`${activeAlertCount} triggered alert${activeAlertCount > 1 ? "s" : ""}`}
              style={{
                background: "var(--red)",
                width: 9,
                height: 9,
                borderRadius: "50%",
                boxShadow: "0 0 0 0 rgba(239,68,68,0.7)",
              }}
              className="ml-auto animate-ping-dot"
            />
          ) : unseenAlertCount > 0 ? (
            <span
              style={{ background: "var(--red)", color: "#fff", fontSize: "0.6rem", fontWeight: 700 }}
              className="ml-auto flex items-center justify-center h-4 w-4 rounded-full"
            >
              {unseenAlertCount > 9 ? "9+" : unseenAlertCount}
            </span>
          ) : null}
        </Link>
        <div style={{ height: "1px", background: "var(--border)", margin: "0.75rem 0" }} />
        <Link to="/profile" className={link("/profile")}>
          <FaUserCog style={{ marginRight: "0.625rem", fontSize: "0.875rem" }} />
          Profile
        </Link>
        <Link to="/settings" className={link("/settings")}>
          <FaCog style={{ marginRight: "0.625rem", fontSize: "0.875rem" }} />
          Settings
        </Link>
        <button type="button" onClick={handleLogout} className="nav-item w-full">
          <FaSignOutAlt style={{ marginRight: "0.625rem", fontSize: "0.875rem" }} />
          Sign Out
        </button>
      </nav>

      {/* Server status */}
      <div style={{ borderTop: "1px solid var(--border)", paddingTop: "0.75rem", marginTop: "0.75rem" }}>
        <div className="flex items-center justify-between px-1">
          <div className="flex items-center gap-1.5">
            <span
              style={{
                width: 6, height: 6, borderRadius: "50%",
                background: statusColor,
                display: "inline-block",
              }}
            />
            <span style={{ fontSize: "0.75rem", color: "var(--text-muted)" }}>{serverStatus}</span>
          </div>
          <span style={{ fontSize: "0.75rem", color: "var(--text-muted)" }}>v2.1.0</span>
        </div>
      </div>
    </aside>
  );
}
