import * as signalR from "@microsoft/signalr";
import { refreshToken } from "./api";

const HUB_URL = import.meta.env.VITE_HUB_URL ?? `${import.meta.env.VITE_SIGNALR_BASE_URL ?? "http://localhost:5041"}/unifiedHub`;

let connection = null;
let startPromise = null;
const reconnectHandlers = new Set();

function isExpired(token) {
  if (!token) return true;

  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return payload.exp * 1000 < Date.now();
  } catch {
    return true;
  }
}

async function getToken() {
  let token = localStorage.getItem("accessToken");

  if (!token || isExpired(token)) {
    const refreshed = await refreshToken();

    if (!refreshed) {
      window.location.href = "/signin";
      return null;
    }

    token = localStorage.getItem("accessToken");
  }

  return token;
}

function buildConnection() {
  const conn = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: getToken,
    })
    .withAutomaticReconnect([2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  conn.on("Error", (message) => {
    console.error("SignalR server error:", message);
  });

  conn.onreconnecting(() => {
    console.log("SignalR reconnecting...");
  });

  conn.onreconnected(async () => {
    console.log("SignalR reconnected");
    await Promise.allSettled(Array.from(reconnectHandlers, (handler) => handler(conn)));
  });

  conn.onclose(() => {
    console.log("SignalR disconnected");
  });

  return conn;
}

export function onSignalRReconnected(handler) {
  reconnectHandlers.add(handler);
  return () => reconnectHandlers.delete(handler);
}

export async function getSignalRConnection() {
  if (connection?.state === signalR.HubConnectionState.Connected) {
    return connection;
  }

  if (startPromise) {
    return startPromise;
  }

  if (connection && connection.state !== signalR.HubConnectionState.Disconnected) {
    return connection;
  }

  if (!connection || connection.state === signalR.HubConnectionState.Disconnected) {
    connection = buildConnection();
  }

  startPromise = connection
    .start()
    .then(() => {
      console.log("SignalR connected");
      return connection;
    })
    .catch((error) => {
      connection = null;
      throw error;
    })
    .finally(() => {
      startPromise = null;
    });

  return startPromise;
}

export async function stopSignalR() {
  if (!connection) return;

  const conn = connection;
  connection = null;
  startPromise = null;

  try {
    conn.off("Error");
    await conn.stop();
  } catch (error) {
    console.warn("SignalR disconnect error:", error);
  }
}

export const createSignalRConnection = getSignalRConnection;

export const disconnectSignalR = () => {
  // The app shares one SignalR connection across charts and alerts.
  // Callers should detach their own handlers; the shared connection stays warm.
};
