import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { loginRequest, loginSuccess, loginFailure } from "../redux/user/userSlice";
import api from "../api/axios";

export default function Signin() {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { loading, error } = useSelector((s) => s.user);
  const [form, setForm] = useState({ username: "", password: "" });

  const handleChange = (e) => setForm((p) => ({ ...p, [e.target.name]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    dispatch(loginRequest());
    try {
      const { data } = await api.post("/auth/login", form);
      dispatch(loginSuccess(data));
      navigate("/");
    } catch (err) {
      dispatch(loginFailure(err.response?.data?.message ?? "Login failed"));
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
            Sign in to Denki
          </h2>
          <p style={{ fontSize: "0.875rem", color: "var(--text-muted)", marginTop: "0.375rem" }}>
            Monitor your energy in real time
          </p>
        </div>

        {error && (
          <div
            style={{
              background: "rgba(239,68,68,0.08)",
              border: "1px solid rgba(239,68,68,0.2)",
              borderRadius: "var(--radius-sm)",
              padding: "0.625rem 0.875rem",
              marginBottom: "1.25rem",
              fontSize: "0.875rem",
              color: "var(--red)",
            }}
          >
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
              Username
            </label>
            <input
              type="text" name="username" value={form.username}
              onChange={handleChange} placeholder="Enter your username"
              className="form-input" required
            />
          </div>

          <div>
            <label style={{ fontSize: "0.8125rem", color: "var(--text-secondary)", display: "block", marginBottom: "0.375rem" }}>
              Password
            </label>
            <input
              type="password" name="password" value={form.password}
              onChange={handleChange} placeholder="Enter your password"
              className="form-input" required
            />
          </div>

          <button
            type="submit" disabled={loading}
            className="btn-primary w-full justify-center"
            style={{ marginTop: "0.5rem", opacity: loading ? 0.6 : 1, cursor: loading ? "not-allowed" : "pointer" }}
          >
            {loading ? "Signing in..." : "Sign in"}
          </button>
        </form>

        <p style={{ fontSize: "0.8125rem", color: "var(--text-muted)", textAlign: "center", marginTop: "1.5rem" }}>
          Don't have an account?{" "}
          <Link to="/signup" style={{ color: "var(--text-secondary)" }} className="hover:text-white transition-colors">
            Sign up
          </Link>
        </p>
      </div>
    </div>
  );
}
