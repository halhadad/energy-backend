import { useState, useEffect, useRef } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useLocation, useNavigate } from "react-router-dom";
import { FaUserCog, FaSignOutAlt } from "react-icons/fa";
import { logout } from "../redux/user/userSlice";

const TITLES = {
  "/": { title: "Overview", sub: "Live power consumption across your organisation" },
  "/analytics": { title: "Analytics", sub: "Historical consumption, cost and electrical data" },
  "/organisations": { title: "Organisations", sub: "Manage your organisations and devices" },
  "/alerts": { title: "Alerts", sub: "Power threshold alerts and event history" },
  "/profile": { title: "Profile", sub: "Account info and selected organisation details" },
};

export default function Header() {
  const { pathname } = useLocation();
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const username = useSelector((s) => s.user?.current_user?.username ?? "");
  const basePath = "/" + pathname.split("/")[1];
  const { title, sub } = TITLES[basePath] ?? { title: "Denki", sub: "Energy monitoring dashboard" };
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);

  useEffect(() => {
    const onDocClick = (e) => {
      if (!menuRef.current?.contains(e.target)) setMenuOpen(false);
    };
    document.addEventListener("mousedown", onDocClick);
    return () => document.removeEventListener("mousedown", onDocClick);
  }, []);

  const handleLogout = () => {
    setMenuOpen(false);
    setTimeout(() => { dispatch(logout()); navigate("/"); }, 150);
  };

  return (
    <header
      style={{ borderBottom: "1px solid var(--border)" }}
      className="flex items-center justify-between px-6 py-4"
    >
      <div>
        <h1
          style={{
            color: "var(--text-primary)",
            fontWeight: 600,
            fontSize: "0.9375rem",
            letterSpacing: "-0.01em",
          }}
        >
          {title}
        </h1>
        <p style={{ color: "var(--text-muted)", fontSize: "0.8125rem", marginTop: "0.125rem" }}>
          {sub}
        </p>
      </div>
      {username && (
        <div ref={menuRef} style={{ position: "relative" }}>
          <button
            type="button"
            onClick={() => setMenuOpen((v) => !v)}
            className="flex items-center gap-2.5 px-2 py-1 rounded-md"
            style={{ border: "1px solid transparent" }}
          >
            <span style={{ fontSize: "0.8125rem", color: "var(--text-secondary)" }}>{username}</span>
            <img
              src="https://i.ibb.co/x8HXwTM3/user.png"
              alt="avatar"
              className="w-7 h-7 rounded-full"
              style={{ border: "1px solid var(--border)" }}
            />
          </button>

          {menuOpen && (
            <div
              style={{
                position: "absolute",
                right: 0,
                top: "calc(100% + 0.35rem)",
                width: 210,
                background: "var(--bg-surface)",
                border: "1px solid var(--border)",
                borderRadius: "var(--radius-md)",
                padding: "0.25rem",
                zIndex: 80,
              }}
            >
              <button
                type="button"
                className="nav-item w-full"
                onClick={() => { setMenuOpen(false); navigate("/profile"); }}
              >
                <FaUserCog style={{ marginRight: "0.625rem", fontSize: "0.8125rem" }} />
                Profile
              </button>
              <button
                type="button"
                className="nav-item w-full"
                onClick={handleLogout}
              >
                <FaSignOutAlt style={{ marginRight: "0.625rem", fontSize: "0.8125rem" }} />
                Sign Out
              </button>
            </div>
          )}
        </div>
      )}
    </header>
  );
}
