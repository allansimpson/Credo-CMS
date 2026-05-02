import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { authApi } from "@/lib/api/auth";
import { ApiEvents } from "@/lib/apiClient";
import type { CurrentUser, Role } from "@/types/api";

export type AuthStatus = "loading" | "authenticated" | "anonymous";

export interface AuthContextValue {
  status: AuthStatus;
  user: CurrentUser | null;
  roles: Role[];
  isAuthenticated: boolean;
  hasRole: (role: Role) => boolean;
  hasAnyRole: (roles: Role[]) => boolean;
  /** Imperatively re-fetch /api/auth/me. */
  refresh: () => Promise<void>;
  login: (email: string, password: string) => Promise<{ ok: boolean; errors: string[] }>;
  logout: () => Promise<void>;
  /** ISO timestamp string (UTC) of when the cookie ticket expires, if known. */
  expiresAtUtc: string | null;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [expiresAtUtc, setExpiresAtUtc] = useState<string | null>(null);
  const navigate = useNavigate();
  const location = useLocation();

  const refreshing = useRef(false);

  const refresh = useCallback(async () => {
    if (refreshing.current) return;
    refreshing.current = true;
    try {
      const me = await authApi.me();
      if (me) {
        setUser(me);
        setStatus("authenticated");
        setExpiresAtUtc(me.expiresAtUtc);
      } else {
        setUser(null);
        setStatus("anonymous");
        setExpiresAtUtc(null);
      }
    } catch {
      setUser(null);
      setStatus("anonymous");
      setExpiresAtUtc(null);
    } finally {
      refreshing.current = false;
    }
  }, []);

  // Initial fetch.
  useEffect(() => {
    refresh();
  }, [refresh]);

  // Keep the ticket-expiry stamp fresh as authenticated requests come in.
  useEffect(() => {
    const handler = (event: Event) => {
      const detail = (event as CustomEvent<string>).detail;
      if (typeof detail === "string") setExpiresAtUtc(detail);
    };
    window.addEventListener(ApiEvents.SessionExpiryHeader, handler);
    return () => window.removeEventListener(ApiEvents.SessionExpiryHeader, handler);
  }, []);

  // Redirect to /login on 401 from any authenticated endpoint.
  useEffect(() => {
    const handler = () => {
      if (status === "authenticated") {
        setUser(null);
        setStatus("anonymous");
        setExpiresAtUtc(null);
      }
      const isPublic =
        location.pathname === "/" ||
        location.pathname.startsWith("/login") ||
        location.pathname.startsWith("/forgot-password") ||
        location.pathname.startsWith("/reset-password") ||
        location.pathname.startsWith("/accept-invitation") ||
        location.pathname.startsWith("/privacy") ||
        location.pathname.startsWith("/terms");
      if (!isPublic) {
        const next = `${location.pathname}${location.search}`;
        navigate(`/login?return=${encodeURIComponent(next)}`, { replace: true });
      }
    };
    window.addEventListener(ApiEvents.Unauthorized, handler);
    return () => window.removeEventListener(ApiEvents.Unauthorized, handler);
  }, [navigate, location.pathname, location.search, status]);

  const login = useCallback(
    async (email: string, password: string) => {
      try {
        const result = await authApi.login({ email, password });
        setUser(result.user);
        setStatus("authenticated");
        setExpiresAtUtc(result.user.expiresAtUtc);
        return { ok: true, errors: [] };
      } catch (err) {
        const messages =
          typeof err === "object" && err !== null && "getMessages" in err
            ? (err as { getMessages: () => string[] }).getMessages()
            : ["Sign-in failed. Please try again."];
        return { ok: false, errors: messages };
      }
    },
    [],
  );

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      setUser(null);
      setStatus("anonymous");
      setExpiresAtUtc(null);
      navigate("/login", { replace: true });
    }
  }, [navigate]);

  const value = useMemo<AuthContextValue>(() => {
    const roles = user?.roles ?? [];
    return {
      status,
      user,
      roles,
      isAuthenticated: status === "authenticated",
      hasRole: (role: Role) => roles.includes(role),
      hasAnyRole: (rs: Role[]) => rs.some((r) => roles.includes(r)),
      refresh,
      login,
      logout,
      expiresAtUtc,
    };
  }, [status, user, refresh, login, logout, expiresAtUtc]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
