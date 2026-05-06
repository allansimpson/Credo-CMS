import { Suspense, lazy } from "react";
import { Route, Routes } from "react-router-dom";
import { AuthProvider } from "@/lib/AuthContext";
import { AdminNotificationsProvider } from "@/hooks/useAdminNotifications";
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
const SermonsArchivePage = lazy(() =>
  import("@/pages/public/SermonsArchivePage").then((m) => ({ default: m.SermonsArchivePage }))
);
const SermonsByBookIndexPage = lazy(() =>
  import("@/pages/public/SermonsByBookIndexPage").then((m) => ({ default: m.SermonsByBookIndexPage }))
);
const SermonsByBookPage = lazy(() =>
  import("@/pages/public/SermonsByBookPage").then((m) => ({ default: m.SermonsByBookPage }))
);
const SermonDetailPage = lazy(() =>
  import("@/pages/public/SermonDetailPage").then((m) => ({ default: m.SermonDetailPage }))
);
const EventsListPage = lazy(() =>
  import("@/pages/admin/EventsListPage").then((m) => ({ default: m.EventsListPage }))
);
const EventEditorPage = lazy(() =>
  import("@/pages/admin/EventEditorPage").then((m) => ({ default: m.EventEditorPage }))
);
const EventRegistrationsAdminPage = lazy(() =>
  import("@/pages/admin/EventRegistrationsAdminPage").then((m) => ({ default: m.EventRegistrationsAdminPage }))
);
const PublicEventsListPage = lazy(() =>
  import("@/pages/public/EventsListPage").then((m) => ({ default: m.PublicEventsListPage }))
);
const PublicEventDetailPage = lazy(() =>
  import("@/pages/public/EventDetailPage").then((m) => ({ default: m.EventDetailPage }))
);
const EventRegisterPage = lazy(() =>
  import("@/pages/public/EventRegisterPage").then((m) => ({ default: m.EventRegisterPage }))
);
const EventCancelRegistrationPage = lazy(() =>
  import("@/pages/public/EventCancelRegistrationPage").then((m) => ({ default: m.EventCancelRegistrationPage }))
);
const CalendarPage = lazy(() =>
  import("@/pages/public/CalendarPage").then((m) => ({ default: m.CalendarPage }))
);
const EventsCalendarOverviewPage = lazy(() =>
  import("@/pages/admin/EventsCalendarOverviewPage").then((m) => ({ default: m.EventsCalendarOverviewPage }))
);
const ProfileCalendarFeedPage = lazy(() =>
  import("@/pages/ProfileCalendarFeedPage").then((m) => ({ default: m.ProfileCalendarFeedPage }))
);
const ProfileRegistrationsPage = lazy(() =>
  import("@/pages/ProfileRegistrationsPage").then((m) => ({ default: m.ProfileRegistrationsPage }))
);
const MembersDirectoryPage = lazy(() =>
  import("@/pages/MembersDirectoryPage").then((m) => ({ default: m.MembersDirectoryPage }))
);
const MemberDetailPage = lazy(() =>
  import("@/pages/MemberDetailPage").then((m) => ({ default: m.MemberDetailPage }))
);
const GetInvolvedPage = lazy(() =>
  import("@/pages/public/GetInvolvedPage").then((m) => ({ default: m.GetInvolvedPage }))
);
const GroupDetailPage = lazy(() =>
  import("@/pages/public/GroupDetailPage").then((m) => ({ default: m.GroupDetailPage }))
);
const ProfileGroupsPage = lazy(() =>
  import("@/pages/ProfileGroupsPage").then((m) => ({ default: m.ProfileGroupsPage }))
);
const GroupsListPage = lazy(() =>
  import("@/pages/admin/GroupsListPage").then((m) => ({ default: m.GroupsListPage }))
);
const GroupEditorPage = lazy(() =>
  import("@/pages/admin/GroupEditorPage").then((m) => ({ default: m.GroupEditorPage }))
);
const ClassesPage = lazy(() =>
  import("@/pages/public/ClassesPage").then((m) => ({ default: m.ClassesPage }))
);
const ClassDetailPage = lazy(() =>
  import("@/pages/public/ClassDetailPage").then((m) => ({ default: m.ClassDetailPage }))
);
const ClassSlotsListPage = lazy(() =>
  import("@/pages/admin/ClassSlotsListPage").then((m) => ({ default: m.ClassSlotsListPage }))
);
const ClassSlotEditorPage = lazy(() =>
  import("@/pages/admin/ClassSlotEditorPage").then((m) => ({ default: m.ClassSlotEditorPage }))
);
const ClassOfferingsListPage = lazy(() =>
  import("@/pages/admin/ClassOfferingsListPage").then((m) => ({ default: m.ClassOfferingsListPage }))
);
const ClassOfferingEditorPage = lazy(() =>
  import("@/pages/admin/ClassOfferingEditorPage").then((m) => ({ default: m.ClassOfferingEditorPage }))
);
const PrayerRequestsListPage = lazy(() =>
  import("@/pages/PrayerRequestsListPage").then((m) => ({ default: m.PrayerRequestsListPage }))
);
const PrayerRequestSubmitPage = lazy(() =>
  import("@/pages/PrayerRequestSubmitPage").then((m) => ({ default: m.PrayerRequestSubmitPage }))
);
const PrayerRequestDetailPage = lazy(() =>
  import("@/pages/PrayerRequestDetailPage").then((m) => ({ default: m.PrayerRequestDetailPage }))
);
const PrayerRequestEditPage = lazy(() =>
  import("@/pages/PrayerRequestEditPage").then((m) => ({ default: m.PrayerRequestEditPage }))
);
const AdminPrayerRequestsPage = lazy(() =>
  import("@/pages/admin/AdminPrayerRequestsPage").then((m) => ({ default: m.AdminPrayerRequestsPage }))
);
const ConnectCardPage = lazy(() =>
  import("@/pages/public/ConnectCardPage").then((m) => ({ default: m.ConnectCardPage }))
);
const ConnectThankYouPage = lazy(() =>
  import("@/pages/public/ConnectThankYouPage").then((m) => ({ default: m.ConnectThankYouPage }))
);
const AdminConnectCardsPage = lazy(() =>
  import("@/pages/admin/AdminConnectCardsPage").then((m) => ({ default: m.AdminConnectCardsPage }))
);
const AdminConnectCardDetailPage = lazy(() =>
  import("@/pages/admin/AdminConnectCardDetailPage").then((m) => ({ default: m.AdminConnectCardDetailPage }))
);
const BlogListPage = lazy(() =>
  import("@/pages/public/BlogListPage").then((m) => ({ default: m.BlogListPage }))
);
const BlogDetailPage = lazy(() =>
  import("@/pages/public/BlogDetailPage").then((m) => ({ default: m.BlogDetailPage }))
);
const AdminBlogListPage = lazy(() =>
  import("@/pages/admin/AdminBlogListPage").then((m) => ({ default: m.AdminBlogListPage }))
);
const AdminBlogEditorPage = lazy(() =>
  import("@/pages/admin/AdminBlogEditorPage").then((m) => ({ default: m.AdminBlogEditorPage }))
);

export default function App() {
  return (
    <SiteSettingsProvider>
      <AuthProvider>
        <AdminNotificationsProvider>
        <SessionExpiryWarning />
        <Routes>
          {/* Public, church-themed */}
          <Route element={<PublicLayout />}>
            <Route index element={<HomePage />} />
            <Route
              path="service-times"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
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
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <PublicNewsListPage />
                </Suspense>
              }
            />
            <Route
              path="news/:slug"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <NewsDetailPage />
                </Suspense>
              }
            />

            {/* Leaders */}
            <Route
              path="leaders"
              element={
                <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted">Loading…</p>}>
                  <PublicLeadersPage />
                </Suspense>
              }
            />
            <Route
              path="leaders/:id"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <LeaderDetailPage />
                </Suspense>
              }
            />

            {/* Documents */}
            <Route
              path="documents"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <PublicDocumentsListPage />
                </Suspense>
              }
            />
            <Route
              path="documents/:id"
              element={
                <Suspense fallback={<p className="mx-auto max-w-4xl p-8 text-muted">Loading…</p>}>
                  <DocumentDetailPage />
                </Suspense>
              }
            />

            {/* Sermons (public) */}
            <Route
              path="sermons"
              element={
                <Suspense fallback={<p className="mx-auto max-w-6xl p-8 text-muted">Loading…</p>}>
                  <SermonsArchivePage />
                </Suspense>
              }
            />
            <Route
              path="sermons/series"
              element={
                <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted">Loading…</p>}>
                  <SermonSeriesPublicListPage />
                </Suspense>
              }
            />
            <Route
              path="sermons/series/:slug"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <SermonSeriesPublicDetailPage />
                </Suspense>
              }
            />
            <Route
              path="sermons/by-book"
              element={
                <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted">Loading…</p>}>
                  <SermonsByBookIndexPage />
                </Suspense>
              }
            />
            <Route
              path="sermons/by-book/:bookSlug"
              element={
                <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted">Loading…</p>}>
                  <SermonsByBookPage />
                </Suspense>
              }
            />
            <Route
              path="sermons/:slug"
              element={
                <Suspense fallback={<p className="mx-auto max-w-4xl p-8 text-muted">Loading…</p>}>
                  <SermonDetailPage />
                </Suspense>
              }
            />

            {/* Events (public) */}
            <Route
              path="events"
              element={
                <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted">Loading…</p>}>
                  <PublicEventsListPage />
                </Suspense>
              }
            />
            <Route
              path="events/:slug"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <PublicEventDetailPage />
                </Suspense>
              }
            />
            <Route
              path="events/:slug/register"
              element={
                <Suspense fallback={<p className="mx-auto max-w-2xl p-8 text-muted">Loading…</p>}>
                  <EventRegisterPage />
                </Suspense>
              }
            />
            <Route
              path="calendar"
              element={
                <Suspense fallback={<p className="mx-auto max-w-6xl p-8 text-muted">Loading…</p>}>
                  <CalendarPage />
                </Suspense>
              }
            />
            <Route
              path="events/:slug/register/cancel"
              element={
                <Suspense fallback={<p className="mx-auto max-w-xl p-8 text-muted">Loading…</p>}>
                  <EventCancelRegistrationPage />
                </Suspense>
              }
            />

            {/* Search */}
            <Route
              path="search"
              element={
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
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
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
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
          <Route
            path="profile/calendar-feed"
            element={
              <ProtectedRoute mode="auth">
                <Suspense fallback={<p className="mx-auto max-w-2xl p-8 text-muted">Loading…</p>}>
                  <ProfileCalendarFeedPage />
                </Suspense>
              </ProtectedRoute>
            }
          />
          <Route
            path="profile/registrations"
            element={
              <ProtectedRoute mode="auth">
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <ProfileRegistrationsPage />
                </Suspense>
              </ProtectedRoute>
            }
          />
          <Route
            path="members"
            element={
              <ProtectedRoute mode="auth" roles={["Member", "Editor", "Administrator"]}>
                <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted">Loading…</p>}>
                  <MembersDirectoryPage />
                </Suspense>
              </ProtectedRoute>
            }
          />
          <Route
            path="members/:userId"
            element={
              <ProtectedRoute mode="auth" roles={["Member", "Editor", "Administrator"]}>
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <MemberDetailPage />
                </Suspense>
              </ProtectedRoute>
            }
          />
          <Route
            path="get-involved"
            element={
              <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted">Loading…</p>}>
                <GetInvolvedPage />
              </Suspense>
            }
          />
          <Route
            path="groups/:slug"
            element={
              <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                <GroupDetailPage />
              </Suspense>
            }
          />
          <Route
            path="profile/groups"
            element={
              <ProtectedRoute mode="auth">
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <ProfileGroupsPage />
                </Suspense>
              </ProtectedRoute>
            }
          />
          <Route
            path="classes"
            element={
              <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted">Loading…</p>}>
                <ClassesPage />
              </Suspense>
            }
          />
          <Route
            path="classes/:slug"
            element={
              <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                <ClassDetailPage />
              </Suspense>
            }
          />
          <Route
            path="prayer-requests"
            element={
              <ProtectedRoute mode="auth" roles={["Member", "Editor", "Administrator"]}>
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <PrayerRequestsListPage />
                </Suspense>
              </ProtectedRoute>
            }
          />
          <Route
            path="prayer-requests/new"
            element={
              <ProtectedRoute mode="auth" roles={["Member", "Editor", "Administrator"]}>
                <Suspense fallback={<p className="mx-auto max-w-2xl p-8 text-muted">Loading…</p>}>
                  <PrayerRequestSubmitPage />
                </Suspense>
              </ProtectedRoute>
            }
          />
          <Route
            path="prayer-requests/:id"
            element={
              <ProtectedRoute mode="auth" roles={["Member", "Editor", "Administrator"]}>
                <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                  <PrayerRequestDetailPage />
                </Suspense>
              </ProtectedRoute>
            }
          />
          <Route
            path="prayer-requests/:id/edit"
            element={
              <ProtectedRoute mode="auth" roles={["Member", "Editor", "Administrator"]}>
                <Suspense fallback={<p className="mx-auto max-w-2xl p-8 text-muted">Loading…</p>}>
                  <PrayerRequestEditPage />
                </Suspense>
              </ProtectedRoute>
            }
          />
          <Route
            path="connect"
            element={
              <Suspense fallback={<p className="mx-auto max-w-2xl p-8 text-muted">Loading…</p>}>
                <ConnectCardPage />
              </Suspense>
            }
          />
          <Route
            path="connect/thank-you"
            element={
              <Suspense fallback={<p className="mx-auto max-w-2xl p-8 text-muted">Loading…</p>}>
                <ConnectThankYouPage />
              </Suspense>
            }
          />
          <Route
            path="blog"
            element={
              <Suspense fallback={<p className="mx-auto max-w-5xl p-8 text-muted">Loading…</p>}>
                <BlogListPage />
              </Suspense>
            }
          />
          <Route
            path="blog/:slug"
            element={
              <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
                <BlogDetailPage />
              </Suspense>
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
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <PagesListPage />
                </Suspense>
              }
            />
            <Route
              path="pages/:id"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <PageEditorPage />
                </Suspense>
              }
            />
            <Route
              path="news"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <NewsListPage />
                </Suspense>
              }
            />
            <Route
              path="news/:id"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <NewsEditorPage />
                </Suspense>
              }
            />
            <Route
              path="service-times"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <ServiceTimesAdminPage />
                </Suspense>
              }
            />
            <Route
              path="leaders"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <LeadersAdminPage />
                </Suspense>
              }
            />
            <Route
              path="documents"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <DocumentsAdminPage />
                </Suspense>
              }
            />
            <Route
              path="announcement"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <AnnouncementAdminPage />
                </Suspense>
              }
            />
            <Route
              path="sermon-series"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <SermonSeriesAdminListPage />
                </Suspense>
              }
            />
            <Route
              path="sermon-series/:id"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <SermonSeriesAdminEditorPage />
                </Suspense>
              }
            />
            <Route
              path="sermons"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <SermonsListPage />
                </Suspense>
              }
            />
            <Route
              path="sermons/:id"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <SermonEditorPage />
                </Suspense>
              }
            />
            <Route
              path="events"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <EventsListPage />
                </Suspense>
              }
            />
            <Route
              path="events/calendar"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <EventsCalendarOverviewPage />
                </Suspense>
              }
            />
            <Route
              path="events/:id"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <EventEditorPage />
                </Suspense>
              }
            />
            <Route
              path="events/:id/registrations"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <EventRegistrationsAdminPage />
                </Suspense>
              }
            />
            <Route
              path="groups"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <GroupsListPage />
                </Suspense>
              }
            />
            <Route
              path="groups/:id"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <GroupEditorPage />
                </Suspense>
              }
            />
            <Route
              path="class-slots"
              element={
                <ProtectedRoute mode="admin" roles={["Administrator"]}>
                  <Suspense fallback={<p className="text-muted">Loading…</p>}>
                    <ClassSlotsListPage />
                  </Suspense>
                </ProtectedRoute>
              }
            />
            <Route
              path="class-slots/:id"
              element={
                <ProtectedRoute mode="admin" roles={["Administrator"]}>
                  <Suspense fallback={<p className="text-muted">Loading…</p>}>
                    <ClassSlotEditorPage />
                  </Suspense>
                </ProtectedRoute>
              }
            />
            <Route
              path="class-offerings"
              element={
                <ProtectedRoute mode="admin" roles={["Administrator"]}>
                  <Suspense fallback={<p className="text-muted">Loading…</p>}>
                    <ClassOfferingsListPage />
                  </Suspense>
                </ProtectedRoute>
              }
            />
            <Route
              path="class-offerings/:id"
              element={
                <ProtectedRoute mode="admin" roles={["Administrator"]}>
                  <Suspense fallback={<p className="text-muted">Loading…</p>}>
                    <ClassOfferingEditorPage />
                  </Suspense>
                </ProtectedRoute>
              }
            />
            <Route
              path="prayer-requests"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <AdminPrayerRequestsPage />
                </Suspense>
              }
            />
            <Route
              path="connect-cards"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <AdminConnectCardsPage />
                </Suspense>
              }
            />
            <Route
              path="connect-cards/:id"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <AdminConnectCardDetailPage />
                </Suspense>
              }
            />
            <Route
              path="blog"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <AdminBlogListPage />
                </Suspense>
              }
            />
            <Route
              path="blog/:id"
              element={
                <Suspense fallback={<p className="text-muted">Loading…</p>}>
                  <AdminBlogEditorPage />
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
                  <Suspense fallback={<p className="text-muted">Loading…</p>}>
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
        </AdminNotificationsProvider>
      </AuthProvider>
    </SiteSettingsProvider>
  );
}
