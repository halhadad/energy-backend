// compatibility shim, the real http work goes through src/api/axios.js.
// refreshToken and fetchWithAuth stay here so existing callers keep working,
// they just delegate to the axios instance now
import api from "../api/axios";

function normalizeApiUrl(url) {
  const trimmed = (url ?? "http://localhost:5041").replace(/\/$/, "");
  return trimmed.endsWith("/api") ? trimmed : `${trimmed}/api`;
}

const BASE_URL = normalizeApiUrl(import.meta.env.VITE_API_URL ?? import.meta.env.VITE_API_BASE_URL);

/**
 * Refresh the access token using the stored refresh token.
 * Returns true on success, false on failure.
 */
export const refreshToken = async () => {
  const storedRefresh = localStorage.getItem("refreshToken");
  const user = JSON.parse(localStorage.getItem("user"));

  if (!storedRefresh || !user) return false;

  try {
    const res = await api.post("/auth/refresh-token", {
      userId: user.userId ?? user.id,
      refreshToken: storedRefresh,
    });

    if (res.status === 200) {
      localStorage.setItem("accessToken", res.data.accessToken);
      localStorage.setItem("refreshToken", res.data.refreshToken);
      return true;
    }
  } catch {
    return false;
  }

  return false;
};

/**
 * Authenticated fetch wrapper (legacy shim).
 * Prefer using the axios instance directly for new code.
 */
export const fetchWithAuth = async (url, options = {}) => {
  const method = (options.method || "GET").toLowerCase();
  const body = options.body ? JSON.parse(options.body) : undefined;

  try {
    const res = await api({
      method,
      url,
      data: body,
      headers: options.headers,
    });

    // Return a Response-like object so existing callers using res.ok / res.json() work
    return {
      ok: res.status >= 200 && res.status < 300,
      status: res.status,
      json: async () => res.data,
    };
  } catch (err) {
    const status = err.response?.status ?? 0;
    return {
      ok: false,
      status,
      json: async () => err.response?.data ?? {},
    };
  }
};
