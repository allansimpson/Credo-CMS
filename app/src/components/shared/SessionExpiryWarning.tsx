import { useEffect, useState } from "react";
import { useAuth } from "@/hooks/useAuth";
import { ApiEvents } from "@/lib/apiClient";

const WARNING_BEFORE_EXPIRY_MS = 5 * 60 * 1000;

/**
 * Five-minute warning before the cookie ticket would expire. Polls /api/auth/me
 * to refresh the sliding session when the user clicks Continue. Listens for the
 * X-Session-Expires-At header on every authenticated 2xx response so the timer
 * is always armed off the latest known expiry.
 */
export function SessionExpiryWarning() {
  const { isAuthenticated, expiresAtUtc, refresh, logout } = useAuth();
  const [warning, setWarning] = useState(false);

  useEffect(() => {
    if (!isAuthenticated || !expiresAtUtc) {
      setWarning(false);
      return;
    }

    const expiresAt = new Date(expiresAtUtc).getTime();
    const showAt = expiresAt - WARNING_BEFORE_EXPIRY_MS;
    const now = Date.now();
    const delay = showAt - now;

    if (delay <= 0) {
      setWarning(true);
      return;
    }

    const timer = window.setTimeout(() => setWarning(true), delay);
    return () => window.clearTimeout(timer);
  }, [isAuthenticated, expiresAtUtc]);

  // Reset the warning whenever a fresher header arrives.
  useEffect(() => {
    const handler = () => setWarning(false);
    window.addEventListener(ApiEvents.SessionExpiryHeader, handler);
    return () => window.removeEventListener(ApiEvents.SessionExpiryHeader, handler);
  }, []);

  if (!warning) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-foreground/40 p-4">
      <div className="w-full max-w-sm rounded-lg bg-background p-6 shadow-lg">
        <h2 className="text-lg font-semibold">Your session is about to expire</h2>
        <p className="mt-2 text-sm text-muted">
          Your session will expire in 5 minutes. Continue to stay signed in.
        </p>
        <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={() => {
              setWarning(false);
              logout();
            }}
            className="inline-flex h-10 items-center justify-center rounded-md border bg-background px-4 text-sm"
          >
            Log out
          </button>
          <button
            type="button"
            onClick={() => {
              setWarning(false);
              refresh();
            }}
            className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
          >
            Continue
          </button>
        </div>
      </div>
    </div>
  );
}
