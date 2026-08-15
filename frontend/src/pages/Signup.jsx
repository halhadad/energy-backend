import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import api from "../api/axios";

export default function Signup() {
  const navigate = useNavigate();
  const [form, setForm] = useState({ username: "", email: "", password: "" });
  const [message, setMessage] = useState(null); // { text, isError }
  const [loading, setLoading] = useState(false);

  const handleChange = (e) => setForm((p) => ({ ...p, [e.target.name]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    try {
      await api.post("/auth/register", form);
      setMessage({ text: "Account created! Redirecting to sign in...", isError: false });
      setTimeout(() => navigate("/signin"), 1200);
    } catch (err) {
      setMessage({ text: err.response?.data?.message ?? "Registration failed. Try again.", isError: true });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex items-center justify-center">
      <div
        style={{
          background: "var(--bg-surface)",
          border: "1px solid var(--border)",
          borderRadius: "var(--radius-lg)",
          width: "100%",
          maxWidth: 400,
          padding: "2rem",
        }}
      >
        <div className="mb-8">
          <h2 style={{ fontSize: "1.25rem", fontWeight: 600, color: "var(--text-primary)", letterSpacing: "-0.02em" }}>
            Create your account
          </h2>
          <p style={{ fontSize: "0.875rem", color: "var(--text-muted)", marginTop: "0.375rem" }}>
            Start monitoring energy in minutes
          </p>
        </div>

        {message && (
          <div
            style={{
              background: message.isError ? "rgba(239,68,68,0.08)" : "rgba(34,197,94,0.08)",
              border: `1px solid ${message.isError ? "rgba(239,68,68,0.2)" : "rgba(34,197,94,0.2)"}`,
              borderRadius: "var(--radius-sm)",
              padding: "0.625rem 0.875rem",
              marginBottom: "1.25rem",
              fontSize: "0.875rem",
              color: message.isError ? "var(--red)" : "var(--green)",
            }}
          >
            {message.text}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
              Username
            </label>
            <input
              type="text" name="username" value={form.username}
              onChange={handleChange} placeholder="Choose a username"
              className="form-input" required
            />
          </div>

          <div>
            <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
              Email
            </label>
            <input
              type="email" name="email" value={form.email}
              onChange={handleChange} placeholder="you@example.com"
              className="form-input" required
            />
            <p style={{ fontSize: "0.6875rem", color: "var(--text-muted)", marginTop: "0.25rem" }}>
              Used to email you when an alert fires.
            </p>
          </div>

          <div>
            <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
              Password
            </label>
            <input
              type="password" name="password" value={form.password}
              onChange={handleChange} placeholder="Choose a password"
              className="form-input" required
            />
          </div>

          <button
            type="submit" disabled={loading}
            className="btn-primary w-full justify-center"
            style={{ marginTop: "0.5rem", opacity: loading ? 0.6 : 1, cursor: loading ? "not-allowed" : "pointer" }}
          >
            {loading ? "Creating account..." : "Create account"}
          </button>
        </form>

        <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)", textAlign: "center", marginTop: "1.5rem" }}>
          Already have an account?{" "}
          <Link to="/signin" style={{ color: "var(--text-secondary)" }} className="hover:text-white transition-colors">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
