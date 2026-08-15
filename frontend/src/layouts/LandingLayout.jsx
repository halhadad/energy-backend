import { Link, Outlet, useLocation } from "react-router-dom";

export default function LandingLayout() {
  const { pathname } = useLocation();
  const isAuthPage = pathname === "/signin" || pathname === "/signup";

  return (
    <div
      style={{ background: "var(--bg-base)", minHeight: "100vh" }}
      className="flex flex-col items-center justify-center py-6 px-4 relative"
    >
      <div className="max-w-5xl w-full z-10">
        <header className="flex justify-between items-center mb-12">
          <Link to="/" className="flex items-center gap-3">
            <div
              style={{
                background: "var(--bg-surface)",
                border: "1px solid var(--border)",
                borderRadius: "var(--radius-md)",
              }}
              className="w-9 h-9 flex items-center justify-center"
            >
              <img src="https://i.ibb.co/9SQCrH7/Volty2.png" className="w-5 h-5" alt="Denki" />
            </div>
            <span style={{ color: "var(--text-primary)", fontWeight: 600, fontSize: "1.0625rem", letterSpacing: "-0.01em" }}>
              Denki
            </span>
          </Link>

          {!isAuthPage && (
            <nav className="flex items-center gap-6">
              <a href="#features" style={{ color: "var(--text-secondary)", fontSize: "0.875rem" }}
                className="hover:text-white transition-colors">
                Features
              </a>
              <Link to="/signin" className="btn-primary" style={{ padding: "0.5rem 1.125rem", fontSize: "0.875rem" }}>
                Get started
              </Link>
            </nav>
          )}
        </header>

        <Outlet />
      </div>
    </div>
  );
}