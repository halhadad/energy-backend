import axios from "axios";

function normalizeApiUrl(url) {
  const trimmed = (url ?? "http://localhost:5041").replace(/\/$/, "");
  return trimmed.endsWith("/api") ? trimmed : `${trimmed}/api`;
}

const BASE_URL = normalizeApiUrl(import.meta.env.VITE_API_URL ?? import.meta.env.VITE_API_BASE_URL);

const api = axios.create({
  baseURL: BASE_URL,
});

// Refresh Token Helper
const refreshToken = async () => {
  const refreshToken = localStorage.getItem("refreshToken");
  const user = JSON.parse(localStorage.getItem("user"));

  if (!refreshToken || !user) return false;

  try {
    const res = await axios.post(`${BASE_URL}/auth/refresh-token`, {
      userId: user.userId ?? user.id,
      refreshToken,
    });

    if (res.status === 200) {
      const data = res.data;
      localStorage.setItem("accessToken", data.accessToken);
      localStorage.setItem("refreshToken", data.refreshToken);
      return data.accessToken;
    }
  } catch (err) {
    console.error("Refresh token failed:", err);
    return false;
  }

  return false;
};

// Request Interceptor
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("accessToken");
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor (auto refresh)
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // If 401 and not already retried
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      const newToken = await refreshToken();
      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return api(originalRequest); // retry request
      } else {
        // No refresh possible -> logout
        localStorage.clear();
        window.location.href = "/signin";
      }
    }

    return Promise.reject(error);
  }
);

export default api;
