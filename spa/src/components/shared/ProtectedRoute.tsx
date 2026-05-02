import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "@/hooks/useAuth";
import { NotFoundPage } from "@/pages/NotFoundPage";
import type { Role } from "@/types/api";

export type ProtectedRouteMode = "admin" | "member" | "auth";

interface ProtectedRouteProps {
  /**
   * - 'admin': covert 404 for anonymous and authenticated-but-wrong-role.
   * - 'member': redirect to /login for anonymous, covert 404 for wrong-role.
   * - 'auth': redirect to /login for anonymous, no role requirement.
   */
  mode: ProtectedRouteMode;
  /** Required roles. Empty array allows any authenticated user. */
  roles?: Role[];
  children: ReactNode;
}

export function ProtectedRoute({ mode, roles = [], children }: ProtectedRouteProps) {
  const { status, hasAnyRole } = useAuth();
  const location = useLocation();

  if (status === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center text-muted-foreground">
        Loading…
      </div>
    );
  }

  const isAnonymous = status === "anonymous";
  const matchesRole = roles.length === 0 || hasAnyRole(roles);

  if (mode === "admin") {
    if (isAnonymous || !matchesRole) {
      return <NotFoundPage />;
    }
    return <>{children}</>;
  }

  if (mode === "member") {
    if (isAnonymous) {
      const next = `${location.pathname}${location.search}`;
      return <Navigate to={`/login?return=${encodeURIComponent(next)}`} replace />;
    }
    if (!matchesRole) {
      return <NotFoundPage />;
    }
    return <>{children}</>;
  }

  // mode === 'auth'
  if (isAnonymous) {
    const next = `${location.pathname}${location.search}`;
    return <Navigate to={`/login?return=${encodeURIComponent(next)}`} replace />;
  }
  return <>{children}</>;
}
