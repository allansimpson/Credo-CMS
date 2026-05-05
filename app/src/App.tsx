import { Suspense, lazy } from "react";
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

// Pages that pull in TipTap (~340 KB unzipped) are lazy-loaded so the
// public bundle stays small. DynamicPagePage uses TipTap read-only,
// so it's also code-split.
const SettingsPage = lazy(() =>
  import("@/pages/admin/SettingsPage").then((m) => ({ default: m.SettingsPage }))
);
const PagesListPage = lazy(() =>
  import("@/pages/admin/PagesListPage").then((m) => ({ default: m.PagesListPage }))
);
const PageEditorPage = lazy(() =>
  import("@/pages/admin/PageEditorPage").then((m) => ({ default: m.PageEditorPage }))
);
const DynamicPagePage = lazy(() =>
  import("@/pages/public/DynamicPagePage").then((m) => ({ default: m.DynamicPagePage }))
);
const NewsListPage = lazy(() =>
  import("@/pages/admin/NewsListPage").then((m) => ({ default: m.NewsListPage }))
);
const NewsEditorPage = lazy(() =>
  import("@/pages/admin/NewsEditorPage").then((m) => ({ default: m.NewsEditorPage }))
);
const PublicNewsListPage = lazy(() =>
  import("@/pages/public/NewsListPage").then((m) => ({ default: m.PublicNewsListPage }))
);
const NewsDetailPage = lazy(() =>
  import("@/pages/public/NewsDetailPage").then((m) => ({ default: m.NewsDetailPage }))
);
const ServiceTimesAdminPage = lazy(() =>
  import("@/pages/admin/ServiceTimesPage").then((m) => ({ default: m.ServiceTimesPage }))
);
const PublicServiceTimesPage = lazy(() =>
  import("@/pages/public/ServiceTimesPage").then((m) => ({ default: m.PublicServiceTimesPage }))
);
const LeadersAdminPage = lazy(() =>
  import("@/pages/admin/LeadersPage").then((m) => ({ default: m.LeadersPage }))
);
const PublicLeadersPage = lazy(() =>
  import("@/pages/public/LeadersPage").then((m) => ({ default: m.PublicLeadersPage }))
);
const LeaderDetailPage = lazy(() =>
  import("@/pages/public/LeaderDetailPage").then((m) => ({ default: m.LeaderDetailPage }))
);
const DocumentsAdminPage = lazy(() =>
  import("@/pages/admin/DocumentsPage").then((m) => ({ default: m.DocumentsPage }))
);
const PublicDocumentsListPage = lazy(() =>
  import("@/pages/public/DocumentsPage").then((m) => ({ default: m.PublicDocumentsListPage }))
);
const DocumentDetailPage = lazy(() =>
  import("@/pages/public/DocumentDetailPage").then((m) => ({ default: m.DocumentDetailPage }))
);
const AnnouncementAdminPage = lazy(() =>
  import("@/pages/admin/AnnouncementPage").then((m) => ({ default: m.AnnouncementPage }))
);
const SearchPage = lazy(() =>
  import("@/pages/public/SearchPage").then((m) => ({ default: m.SearchPage }))
);
const SermonSeriesAdminListPage = lazy(() =>
  import("@/pages/admin/SermonSeriesListPage").then((m) => ({ default: m.SermonSeriesListPage }))
);
const SermonSeriesAdminEditorPage = lazy(() =>
  import("@/pages/admin/SermonSeriesEditorPage").then((m) => ({ default: m.SermonSeriesEditorPage }))
);
const SermonSeriesPublicListPage = lazy(() =>
  import("@/pages/public/SermonSeriesListPage").then((m) => ({ default: m.SermonSeriesListPublicPage }))
);
const SermonSeriesPublicDetailPage = lazy(() =>
  import("@/pages/public/SermonSeriesDetailPage").then((m) => ({ default: m.SermonSeriesDetailPage }))
);
const SermonsListPage = lazy(() =>
  import("@/pages/admin/SermonsListPage").then((m) => ({ default: m.SermonsListPage }))
);
const SermonEditorPage = lazy(() =>
  import("@/pages/admin/SermonEditorPage").then((m) => ({ default: m.SermonEditorPage }))
);

export default function App() {
  return (
    <SiteSettingsProvider>
      <AuthProvider>
        <SessionExpiryWarning />
        <Routes>
          {/* Public, church-themed */}
          <Route element={<PublicLayout />}>
            <Route index element={<HomePage />} />
            <Route
              path="service-times"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>}>
                  <PublicServiceTimesPage />
                </Suspense>
              }
            />
            {/* /about, /privacy-policy, /terms-of-service, /beliefs, etc. resolve
                via the dynamic :slug route below — they're database-backed Pages,
                not hardcoded placeholders. */}

            {/* News (public) */}
            <Route
              path="news"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>}>
                  <PublicNewsListPage />
                </Suspense>
              }
            />
            <Route
              path="news/:slug"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>}>
                  <NewsDetailPage />
                </Suspense>
              }
            />

            {/* Leaders */}
            <Route
              path="leaders"
              element={
                <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted-foreground">Loading…</p>}>
                  <PublicLeadersPage />
                </Suspense>
              }
            />
            <Route
              path="leaders/:id"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>}>
                  <LeaderDetailPage />
                </Suspense>
              }
            />

            {/* Documents */}
            <Route
              path="documents"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>}>
                  <PublicDocumentsListPage />
                </Suspense>
              }
            />
            <Route
              path="documents/:id"
              element={
                <Suspense fallback={<p className="mx-auto max-w-4xl p-8 text-muted-foreground">Loading…</p>}>
                  <DocumentDetailPage />
                </Suspense>
              }
            />

            {/* Sermon series (public) */}
            <Route
              path="sermons/series"
              element={
                <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted-foreground">Loading…</p>}>
                  <SermonSeriesPublicListPage />
                </Suspense>
              }
            />
            <Route
              path="sermons/series/:slug"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>}>
                  <SermonSeriesPublicDetailPage />
                </Suspense>
              }
            />

            {/* Search */}
            <Route
              path="search"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>}>
                  <SearchPage />
                </Suspense>
              }
            />

            {/* Dynamic content pages. The :slug param matches a single non-slash
                segment, so all database-backed Pages (about, beliefs, privacy-policy,
                terms-of-service, etc.) resolve here. Member-only filtering and 404s
                for missing/unpublished slugs happen inside DynamicPagePage. */}
            <Route
              path=":slug"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>}>
                  <DynamicPagePage />
                </Suspense>
              }
            />
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
              path="pages"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <PagesListPage />
                </Suspense>
              }
            />
            <Route
              path="pages/:id"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <PageEditorPage />
                </Suspense>
              }
            />
            <Route
              path="news"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <NewsListPage />
                </Suspense>
              }
            />
            <Route
              path="news/:id"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <NewsEditorPage />
                </Suspense>
              }
            />
            <Route
              path="service-times"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <ServiceTimesAdminPage />
                </Suspense>
              }
            />
            <Route
              path="leaders"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <LeadersAdminPage />
                </Suspense>
              }
            />
            <Route
              path="documents"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <DocumentsAdminPage />
                </Suspense>
              }
            />
            <Route
              path="announcement"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <AnnouncementAdminPage />
                </Suspense>
              }
            />
            <Route
              path="sermon-series"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <SermonSeriesAdminListPage />
                </Suspense>
              }
            />
            <Route
              path="sermon-series/:id"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <SermonSeriesAdminEditorPage />
                </Suspense>
              }
            />
            <Route
              path="sermons"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <SermonsListPage />
                </Suspense>
              }
            />
            <Route
              path="sermons/:id"
              element={
                <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                  <SermonEditorPage />
                </Suspense>
              }
            />
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
                  <Suspense fallback={<p className="text-muted-foreground">Loading…</p>}>
                    <SettingsPage />
                  </Suspense>
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
