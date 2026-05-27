import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "./ProtectedRoute";
import { AuthContext, type AuthContextValue } from "@/lib/AuthContext";
import { SiteSettingsProvider } from "@/lib/SiteSettingsContext";
import type { Role } from "@/types/api";

function fakeAuth(overrides: Partial<AuthContextValue>): AuthContextValue {
  return {
    status: "anonymous",
    user: null,
    roles: [],
    isAuthenticated: false,
    hasRole: (r: Role) => (overrides.roles ?? []).includes(r),
    hasAnyRole: (rs: Role[]) => rs.some((r) => (overrides.roles ?? []).includes(r)),
    refresh: async () => {},
    login: async () => ({ ok: true, errors: [] }),
    logout: async () => {},
    expiresAtUtc: null,
    ...overrides,
  };
}

function renderWith(auth: AuthContextValue, initialPath = "/admin/users") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <SiteSettingsProvider>
        <AuthContext.Provider value={auth}>
          <Routes>
            <Route
              path="/admin/users"
              element={
                <ProtectedRoute mode="admin" roles={["Administrator"]}>
                  <div data-testid="protected-content">protected</div>
                </ProtectedRoute>
              }
            />
            <Route
              path="/members"
              element={
                <ProtectedRoute mode="member" roles={["Member"]}>
                  <div data-testid="protected-content">protected</div>
                </ProtectedRoute>
              }
            />
            <Route path="/login" element={<div data-testid="login">login</div>} />
            <Route path="*" element={<div data-testid="other">other</div>} />
          </Routes>
        </AuthContext.Provider>
      </SiteSettingsProvider>
    </MemoryRouter>,
  );
}

describe("<ProtectedRoute>", () => {
  beforeEach(() => {
    // Silence the public site-settings fetch during tests.
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({}))));
  });

  it("renders covert 404 for anonymous on admin route", () => {
    renderWith(fakeAuth({ status: "anonymous", isAuthenticated: false }));
    expect(screen.queryByTestId("protected-content")).not.toBeInTheDocument();
    expect(screen.getByText("404")).toBeInTheDocument();
  });

  it("renders covert 404 for authenticated wrong-role on admin route", () => {
    renderWith(
      fakeAuth({
        status: "authenticated",
        isAuthenticated: true,
        roles: ["Member"],
      }),
    );
    expect(screen.queryByTestId("protected-content")).not.toBeInTheDocument();
    expect(screen.getByText("404")).toBeInTheDocument();
  });

  it("renders content for matching admin role", () => {
    renderWith(
      fakeAuth({
        status: "authenticated",
        isAuthenticated: true,
        roles: ["Administrator"],
      }),
    );
    expect(screen.getByTestId("protected-content")).toBeInTheDocument();
  });

  it("redirects to /login for anonymous on member route", () => {
    renderWith(
      fakeAuth({ status: "anonymous", isAuthenticated: false }),
      "/members",
    );
    expect(screen.getByTestId("login")).toBeInTheDocument();
  });
});
