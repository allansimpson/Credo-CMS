import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import { useNotificationHub } from "./useNotificationHub";
import { useAuth } from "./useAuth";

/**
 * Toast-style admin notification surface. Consumes the SignalR hub from
 * <see cref="useNotificationHub"/> and exposes a small list of recent
 * events to the admin shell. Phase 4 Q5 adds <c>GroupJoinRequestSubmitted</c>;
 * Q9 will add Prayer events; Q11 will add Connect-card events.
 */

export interface AdminToast {
  id: string;
  /** Action verb shown in the toast (e.g. "join request"). */
  kind: string;
  /** Short copy ("Alice asked to join Youth"). */
  message: string;
  /** Optional in-app destination so clicking the toast routes to the
   * relevant admin screen. */
  href?: string;
  receivedAt: number;
}

interface JoinRequestPayload {
  groupId: string;
  groupName: string;
  requesterUserId: string;
  requesterDisplayName: string;
}

interface AdminNotificationsContextValue {
  toasts: AdminToast[];
  dismiss: (id: string) => void;
}

const AdminNotificationsContext = createContext<AdminNotificationsContextValue>({
  toasts: [],
  dismiss: () => undefined,
});

const MAX_TOASTS = 4;
const TOAST_TTL_MS = 8000;

export function AdminNotificationsProvider({ children }: { children: ReactNode }) {
  const { hasAnyRole, isAuthenticated } = useAuth();
  const { on, off } = useNotificationHub();
  const [toasts, setToasts] = useState<AdminToast[]>([]);
  const counter = useRef(0);

  const push = useCallback((toast: Omit<AdminToast, "id" | "receivedAt">) => {
    counter.current += 1;
    const id = `t${Date.now()}-${counter.current}`;
    const next: AdminToast = { id, receivedAt: Date.now(), ...toast };
    setToasts((prev) => [next, ...prev].slice(0, MAX_TOASTS));
    window.setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id));
    }, TOAST_TTL_MS);
  }, []);

  useEffect(() => {
    if (!isAuthenticated) return;
    const onJoinRequest = (...args: unknown[]) => {
      const payload = args[0] as JoinRequestPayload | undefined;
      if (!payload) return;
      push({
        kind: "Group join request",
        message: `${payload.requesterDisplayName} asked to join ${payload.groupName}`,
        href: `/admin/groups`,
      });
    };
    const onConnectCard = (...args: unknown[]) => {
      const payload = args[0] as { id: string; name: string } | undefined;
      if (!payload) return;
      push({
        kind: "Connect card",
        message: `${payload.name} sent a connect card`,
        href: `/admin/connect-cards/${payload.id}`,
      });
    };
    on("GroupJoinRequestSubmitted", onJoinRequest);
    on("ConnectCardSubmitted", onConnectCard);
    return () => {
      off("GroupJoinRequestSubmitted", onJoinRequest);
      off("ConnectCardSubmitted", onConnectCard);
    };
  }, [isAuthenticated, on, off, push]);

  const dismiss = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const value: AdminNotificationsContextValue = { toasts, dismiss };

  // Only mount the toast surface for users who can act on the notifications.
  const showToasts = isAuthenticated && hasAnyRole(["Administrator", "Editor"]);

  return (
    <AdminNotificationsContext.Provider value={value}>
      {children}
      {showToasts && <ToastViewport />}
    </AdminNotificationsContext.Provider>
  );
}

export function useAdminNotifications(): AdminNotificationsContextValue {
  return useContext(AdminNotificationsContext);
}

function ToastViewport() {
  const { toasts, dismiss } = useAdminNotifications();
  if (toasts.length === 0) return null;

  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed bottom-4 right-4 z-50 flex w-80 flex-col gap-2"
    >
      {toasts.map((t) => (
        <a
          key={t.id}
          href={t.href ?? "#"}
          onClick={() => dismiss(t.id)}
          className="block border border-border bg-panel p-3 text-sm shadow-lg hover:bg-panel-alt"
        >
          <p className="text-[11px] font-semibold uppercase tracking-wider text-accent">{t.kind}</p>
          <p className="mt-1">{t.message}</p>
        </a>
      ))}
    </div>
  );
}
