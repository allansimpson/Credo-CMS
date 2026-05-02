import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useCallback, useEffect, useRef, useState } from "react";
import { useAuth } from "./useAuth";

const HUB_URL = "/hubs/notifications";

/**
 * Connects to the SignalR notification hub when the user is authenticated.
 * Disconnects on logout. Auto-reconnects with exponential backoff on transient
 * failure. Falls back to a logged warning if the connection cannot be
 * established (does not break the app).
 *
 * Subscribers attach via `on(eventName, handler)` and clean up via
 * `off(eventName, handler)`. Phase 1 ships the seam; the events are added
 * starting in Phase 4.
 */
export function useNotificationHub() {
  const { isAuthenticated } = useAuth();
  const connectionRef = useRef<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  const on = useCallback(
    (eventName: string, handler: (...args: unknown[]) => void) => {
      connectionRef.current?.on(eventName, handler);
    },
    [],
  );

  const off = useCallback(
    (eventName: string, handler: (...args: unknown[]) => void) => {
      connectionRef.current?.off(eventName, handler);
    },
    [],
  );

  useEffect(() => {
    if (!isAuthenticated) {
      const conn = connectionRef.current;
      if (conn && conn.state !== HubConnectionState.Disconnected) {
        conn.stop().catch(() => {/* swallow */});
      }
      connectionRef.current = null;
      setIsConnected(false);
      return;
    }

    let cancelled = false;

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.onreconnected(() => setIsConnected(true));
    connection.onreconnecting(() => setIsConnected(false));
    connection.onclose(() => setIsConnected(false));

    connection
      .start()
      .then(() => {
        if (cancelled) {
          connection.stop().catch(() => {/* swallow */});
          return;
        }
        connectionRef.current = connection;
        setIsConnected(true);
      })
      .catch((err: unknown) => {
        // Graceful fallback — log and continue without real-time.
        // eslint-disable-next-line no-console
        console.warn("SignalR hub connection failed:", err);
      });

    return () => {
      cancelled = true;
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.stop().catch(() => {/* swallow */});
      }
      connectionRef.current = null;
      setIsConnected(false);
    };
  }, [isAuthenticated]);

  return { on, off, isConnected };
}
