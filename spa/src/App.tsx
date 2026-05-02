import { Route, Routes } from "react-router-dom";
import { AuthProvider } from "@/lib/AuthContext";
import { SiteSettingsProvider } from "@/lib/SiteSettingsContext";
import { ProtectedRoute } from "@/components/shared/ProtectedRoute";
import { SessionExpiryWarning } from "@/components/shared/SessionExpiryWarning";

import { PublicLayout } from "@/pages/public/PublicLayout";
import { HomePage } from "@/pages/public/HomePage";
import { PlaceholderPage } from "@/pages/public/PlaceholderPage";
import { LoginPage } from "@/pages/auth/LoginPage";
import { ForgotPasswordPage } from "@/pages/auth/ForgotPasswordPage";
import { ResetPasswordPage } from "@/pages/auth/ResetPasswordPage";
import { AcceptInvitationPage } from "@/pages/auth/AcceptInvitationPage";
import { ProfilePage } from "@/pages/ProfilePage";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { AdminLayout } from "@/pages/admin/AdminLayout";
import { AdminDashboard } from "@/pages/admin/AdminDashboard";
import { UsersPage } from "@/pages/admin/UsersPage";
import { AuditLogPage } from "@/pages/admin/AuditLogPage";
import { SettingsPage } from "@/pages/admin/SettingsPage";

export default function App() {
  return (
    <SiteSettingsProvider>
      <AuthProvider>
        <SessionExpiryWarning />
        <Routes>
          {/* Public, church-themed */}
          <Route element={<PublicLayout />}>
            <Route index element={<HomePage />} />
            <Route path="about" element={<PlaceholderPage title="About" />} />
            <Route path="services" element={<PlaceholderPage title="Service Times" />} />
            <Route path="privacy" element={<PlaceholderPage title="Privacy Policy" />} />
            <Route path="terms" element={<PlaceholderPage title="Terms of Service" />} />
          </Route>

          {/* Auth flows (church-themed standalone) */}
          <Route path="login" element={<LoginPage />} />
          <Route path="forgot-password" element={<ForgotPasswordPage />} />
          <Route path="reset-password" element={<ResetPasswordPage />} />
          <Route path="accept-invitation" element={<AcceptInvitationPage />} />

          {/* Authenticated user pages (church-themed) */}
          <Route
            path="profile"
            element={
              <ProtectedRoute mode="auth">
                <ProfilePage />
              </ProtectedRoute>
            }
          />

          {/* Admin shell (system-themed). Wrapped in admin-mode covert-404 gate. */}
          <Route
            path="admin"
            element={
              <ProtectedRoute mode="admin" roles={["Administrator", "Editor"]}>
                <AdminLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<AdminDashboard />} />
            <Route
              path="users"
              element={
                <ProtectedRoute mode="admin" roles={["Administrator"]}>
                  <UsersPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="audit-log"
              element={
                <ProtectedRoute mode="admin" roles={["Administrator"]}>
                  <AuditLogPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="settings"
              element={
                <ProtectedRoute mode="admin" roles={["Administrator"]}>
                  <SettingsPage />
                </ProtectedRoute>
              }
            />
          </Route>

          {/* Docs placeholder (Phase 6 fills the content) */}
          <Route
            path="docs/*"
            element={
              <ProtectedRoute mode="admin" roles={["Administrator", "Editor"]}>
                <PlaceholderPage title="Documentation" body="Astro-rendered docs land in Phase 6." />
              </ProtectedRoute>
            }
          />

          {/* Catch-all */}
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </AuthProvider>
    </SiteSettingsProvider>
  );
}
