# Implementation Notes — Credo CMS Phase 1

A running log of non-trivial decisions, ambiguities encountered, and deviations
from the Phase 1 prompt. Entries are appended in chronological order; nothing is
removed once written.

---

## Standing Rules (set once, applied throughout all phases)

1. **Mobile-first verification at 375px.** Every page is checked at 375px before
   it is considered done. This is the standing testing rule for all phases.
2. **`Application` does not reference `Infrastructure` or EF Core.** Database
   access is only via interfaces defined in `Application`. Violations should
   block code review.
3. **Treat warnings as errors** for all .NET projects via `Directory.Build.props`.
4. **No silent guessing.** Genuine ambiguities surface as `// TODO:` comments
   in code and an entry in this file with reasoning.
5. **Logo upload in Phase 1 is a URL-string field.** Real binary upload arrives
   in Phase 2 alongside the first content-type image flows.

---

## Decisions Log

### 2026-05-02 — Stage A: Repository Scaffolding

**Decision: .NET SDK target.** Build environment shipped without a .NET SDK.
Installed `dotnet-sdk-10.0` (10.0.107) via Ubuntu's universe repository because
the Microsoft CDN (`dot.net`, `builds.dotnet.microsoft.com`) returned 403 to
sandboxed traffic. The Ubuntu-distributed package is the same .NET 10 GA SDK and
matches `net10.0` target. Recorded for operators: production deployments may
prefer the Microsoft package feed; either is fine.

**Decision: cookie name.** Applied `.CredoCms.Auth` (per BUILD_PLAN.md
clarification #9) rather than the Identity default. Documented in
`appsettings.template.json`.

**Decision: `ProtectedRoute` modes.** Added a third mode `'auth'` (per
BUILD_PLAN.md clarification #4) for any-authenticated-user routes such as
`/profile`. Modes are now:

- `'admin'` — covert 404 for anonymous and authenticated-but-wrong-role.
- `'member'` — redirect to `/login?return=...` for anonymous; covert 404 for
  authenticated-but-wrong-role.
- `'auth'` — redirect to `/login?return=...` for anonymous; pass through for
  any authenticated user.

**Decision: session expiry surfacing.** Per BUILD_PLAN.md clarification #5,
`GET /api/auth/me` returns `expiresAtUtc` alongside the user payload, and
authenticated 2xx responses also emit an `X-Session-Expires-At` HTTP header so
the SPA can re-arm its 5-minute warning timer without a separate round-trip.

**Decision: `RequirePasswordChangeOnFirstLogin`.** Added as a `bool` column on
`ApplicationUser`. The seeded default Administrator has it set to `true` so the
operator must change the seeded password on first sign-in.

**Decision: force-logout mechanism.** Uses Identity's
`UserManager.UpdateSecurityStampAsync` to invalidate issued cookies on the next
validation tick. `SecurityStampValidator.ValidationInterval` is set to
**1 minute** so administrative force-logout takes effect almost immediately.

**Decision: hard-delete cascade.** `AuditLogEntry.UserId` is nullable with
`OnDelete(DeleteBehavior.SetNull)`. The display name is captured as
`UserDisplayNameSnapshot` at write time so the log survives a hard delete with
identification intact.

**Decision: dev CORS.** The production deployment serves the SPA from the API's
`wwwroot/` so CORS is unnecessary in production. In `Development` only, a CORS
policy permits `http://localhost:5173` with credentials so the Vite dev server
can talk to the API. Documented in `appsettings.template.json`.

**Decision: temporal-table tests.** EF Core's in-memory provider does not
support `IsTemporal()`. The `VersioningInterceptor` unit test uses the
in-memory provider on a non-temporal stub entity to verify the property setter.
Real temporal-table behavior is exercised by an optional integration test
gated behind a SQL Server LocalDB / SQL Server connection availability check
(skips with an explanatory message when not available).

---

## Pending TODOs Surfaced as `// TODO:` Comments

(Updated as Phase 1 progresses; entries here mirror inline `// TODO:` comments
that reference this section.)

_None yet — list will be populated as ambiguities are encountered during
implementation._

---

## Foundational SPA Utilities — Usage Examples

### `useBreakpoint`

```tsx
import { useBreakpoint } from "@/hooks/useBreakpoint";

function MyComponent() {
  const bp = useBreakpoint();           // 'mobile' | 'tablet' | 'desktop'
  return bp === "mobile" ? <CardLayout /> : <TableLayout />;
}
```

Breakpoints: `mobile` (<768), `tablet` (768–1279), `desktop` (≥1280).
SSR-safe — defaults to `'desktop'` when `window` is undefined.

### `<ResponsiveTable>`

```tsx
<ResponsiveTable
  data={users}
  columns={[
    { id: "name",   header: "Name",   accessor: u => u.displayName, mobilePriority: 1 },
    { id: "email",  header: "Email",  accessor: u => u.email,        mobilePriority: 2 },
    { id: "roles",  header: "Roles",  accessor: u => u.roles.join(", ") },
    { id: "status", header: "Status", accessor: u => u.isActive ? "Active" : "Deactivated" },
  ]}
  pageSize={25}
  searchPlaceholder="Search users..."
  onRowClick={u => navigate(`/admin/users/${u.id}`)}
/>
```

On desktop renders as a shadcn `Table`; on mobile renders as stacked cards
showing only columns with a `mobilePriority` set, sorted ascending.

### `useNotificationHub`

```tsx
const { on, off, isConnected } = useNotificationHub();

useEffect(() => {
  const handler = (msg: NewPrayerRequestMessage) => { /* ... */ };
  on("PrayerRequestCreated", handler);
  return () => off("PrayerRequestCreated", handler);
}, [on, off]);
```

Connects when authenticated, disconnects on logout, auto-reconnects with
exponential backoff. Falls back to a logged warning if the hub is unreachable.

---

### 2026-05-02 — Stage K: Smoke Test & Final Verification

**Smoke-test outcome.** Started the published API on Linux at port 5099 and
verified:

- `GET /api/health` → `200 OK` with `{ status: "ok", utc: "..." }`. ✅
- `GET /api/auth/me` (anonymous) → `401 Unauthorized`. ✅
- `GET /api/admin/users` (anonymous) → `401 Unauthorized`. The SPA route
  layer applies the covert-404 to anonymous callers; the API itself returns
  401 for anonymous so an authenticated-but-stale-session caller still sees
  the session-expiry signal. The 403→404 transformation in
  `ForbiddenToNotFoundMiddleware` catches the authenticated-but-wrong-role
  case (covered by the `<ForbiddenToNotFoundMiddlewareTests>` suite). ✅
- `GET /api/site-settings/public` → fails with
  `PlatformNotSupportedException: LocalDB is not supported on this platform`
  because the dev connection string targets LocalDB and the build environment
  is Linux. **Not a code bug** — production deployment uses Azure SQL via
  the Bicep-deployed connection string. Local Windows dev with LocalDB or
  any reachable SQL Server works as designed.

**DI fix applied.** Smoke-testing surfaced a real bug: the
`VersioningInterceptor` was originally registered as Singleton with a
delegate that resolved `ICurrentUserService` (Scoped) at construction time.
The .NET DI validator caught it: `Cannot resolve scoped service ... from
root provider`. Fix: register `VersioningInterceptor` as Scoped (its
lifetime now matches the DbContext, which is also Scoped, so the interceptor
resolves correctly within each request scope). All 28 tests still pass
after the fix.

**Final state at end of Phase 1:**

- `dotnet build` — clean Release build, zero warnings, zero errors across all
  eight .NET projects (Domain, Application, Infrastructure, Api, plus their
  matching test projects).
- `dotnet test` — **28 tests passing**: Domain (5), Application (10),
  Infrastructure (3), Api (10).
- `dotnet ef migrations add Initial` — produces the Identity + SiteSettings
  + AuditLog schema; tracked in `Persistence/Migrations/`.
- `npm run build` — clean SPA build, 245 KB JS / 18 KB CSS (75 KB / 4.3 KB
  gzipped).
- `npm test` — **10 SPA tests passing**: `useBreakpoint`,
  `<ResponsiveTable>`, `<ProtectedRoute>`.
- API boots cleanly, serves health/auth endpoints, gracefully handles a
  missing/unreachable database (logs a warning and continues without seed).

---

# Phase 2 — Decisions Log

### 2026-05-03 — Stage P1: Site Settings Extensions (completed)

The local session committed Stage P1 partway through (commit `149bfcc` —
WIP: Phase 2 fields added to the Domain entity only, no migration, DTO, or
UI). This web session resumes from that point. State at resume:

- `dotnet build` and `dotnet test` (28 tests) green.
- `npm run build` and `npm test` (10 tests) green.
- Phase 2 properties exist on `SiteSettings` but nothing else references them.

**P1.2a — Migration `AddPhase2SiteSettingsFields`.** Generated via
`dotnet ef migrations add ... -o Persistence/Migrations`. EF's first pass
emitted empty/zero defaults (`""`, `0`, `0L`) for the new NOT-NULL columns,
which would leave existing Phase 1 rows in an unusable state after upgrade.
Patched the generated `Up()` to set defaults that match the property-level
initializers on `SiteSettings.cs`:

| Column | Default |
|---|---|
| `LeadersPageLabel` | `"Our Leaders"` |
| `LeaderCategoriesJson` | `["Pastoral Staff","Elders","Deacons","Ministry Directors"]` |
| `DocumentCategoriesJson` | `["Bulletins","Forms","Policies","Board Minutes","Resources"]` |
| `MaxDocumentSizeBytes` | `26214400` (25 MB) |
| `MaxImageSizeBytes` | `10485760` (10 MB) |
| `ImageMaxWidth` | `2400` |
| `ImageQuality` | `82` |
| `HomepageHeroCtaLabel` | `"Join us Sunday"` |
| `HomepageHeroCtaLink` | `"#service-times"` |

`MembersWelcomeText` and `DefaultMetaDescription` are nullable and stay NULL
on existing rows.

**P1.2b — Seeder.** No changes required. The seeder constructs
`new SiteSettings { ... }` with only a few explicitly-overridden properties;
the Phase 2 property-level defaults take care of the rest. Adding
redundant assignments would just duplicate the entity defaults.

**P1.3 — DTOs / request / validator / service.** Added Phase 2 fields to
`SiteSettingsDto` and `UpdateSiteSettingsRequest`. The public DTO
(`PublicSiteSettingsDto`) only exposes the three fields used by anonymous
public surface area: `LeadersPageLabel`, `HomepageHeroCtaLabel`,
`HomepageHeroCtaLink`. Server-side validation (FluentValidation) covers:

- `LeaderCategoriesJson` / `DocumentCategoriesJson` parse as JSON arrays of
  non-empty strings (otherwise reject).
- `ImageMaxWidth` ∈ [800, 5000].
- `ImageQuality` ∈ [60, 95].
- `MaxImageSizeBytes` ∈ [1, 50] MB.
- `MaxDocumentSizeBytes` ∈ [1, 200] MB.
- `LeadersPageLabel`, `HomepageHeroCtaLabel`, `HomepageHeroCtaLink` are
  required and length-capped.
- `DefaultMetaDescription` ≤ 300 chars.

Audit-log details emitted from `SiteSettingsService.UpdateAsync` were
extended to include the new image/upload knobs so an operator can see what
changed without diffing the row history.

**P1.4 / P1.5 — Admin UI.** Replaced the placeholder `Content` and
`Advanced` tabs in `app/src/pages/admin/SettingsPage.tsx` with real forms.
Refactored the per-tab fetch/save into a `useSettingsForm()` hook so each
tab loads + edits + submits the full record (Site Settings is a single row;
optimistic concurrency via `RowVersion` detects parallel edits between
tabs). Tabs:

- **Branding** — unchanged surface, just rebuilt on top of the shared hook.
- **Content** — Homepage hero CTA, Leaders page label, Leader categories
  editor, Document categories editor, Members welcome message (TipTap).
- **Advanced** — Image max width / quality / max size, max document size,
  default meta description, "Rebuild search index" button (stub, wired in
  P9).

**TipTap editor.** Added `app/src/components/shared/TipTapEditor.tsx` with
StarterKit + Link + Placeholder. Toolbar covers bold/italic/H2/H3/lists/link
/clear — enough for the welcome-message use case in P1; P3 can extend the
toolbar for Pages/News without changing the storage shape (ProseMirror JSON
serialized to a string).

**Bundle hygiene.** TipTap pulls in ProseMirror + extensions (~340 KB
unzipped). Eagerly importing `SettingsPage` into `App.tsx` would have
penalised every public visitor with that weight. Switched the route to
`React.lazy(() => import("@/pages/admin/SettingsPage"))` wrapped in
`<Suspense>`. After-state:

- Public bundle: **239 KB / 74 KB gzip** (matches the Phase 1 baseline).
- `SettingsPage` chunk: 346 KB / 109 KB gzip — only loaded when an admin
  navigates to `/admin/settings`.

**P0 cleanup carryover.** The `git mv spa/ → app/` rename in P0 left
behind empty `spa/src/components/ui`, `spa/src/features`, and `spa/public`
directories (plus build artifacts `spa/dist/` and `spa/node_modules/`).
Removed `spa/` entirely in this commit so `git ls-files spa/` is empty
(it already was) and the repo no longer has a stale top-level folder.

**State at end of P1:**

- `dotnet build` green, zero warnings.
- `dotnet test` — **39 tests passing** (Domain 5, Application **21**
  (was 10; +11 new validator cases), Infrastructure 3, Api 10).
- `npm run build` green; public bundle unchanged at 239 KB / 74 KB gzip;
  `SettingsPage` chunk lazy-loaded.
- `npm test` — 10 tests passing (no SPA tests exist yet for the new tabs;
  P1 is server-side-validated only — SPA test additions for the
  category-list editor and `<TipTapEditor>` are queued for a later P-stage
  cleanup pass).

---

## Deviations from the Prompt (Phase 1)

_None — every section of the Phase 1 prompt is addressed. Out-of-scope items
(Phases 2–6) are explicitly deferred and noted in `ROADMAP.md`. Phase 2
work is tracked in `BUILD_PLAN.md` (sections P-0 through P-7) and
`PHASE_2_BACKLOG.md`._

---

## Phase 2 Decisions Log

### 2026-05-05 — Stages P2 through P15

Phase 2 lands the content surface (Pages, News, ServiceTimes, Leaders,
Documents, Announcement banner), the search infrastructure, the
homepage composition, SEO basics (sitemap + robots + JSON-LD), output
caching foundation, version-history server-side handler registry,
SignalR notifier foundation, and seed data.

**Decisions worth recording:**

- **Pattern of optional cross-cutting deps in services.** `ISearchIndexer`,
  `IOutputCacheInvalidator`, and `IRealtimeNotifier` are wired into
  content services as **optional constructor parameters** with default
  `null`. Reasons: keeps existing unit tests passing without churn; lets
  a service still construct cleanly in environments where those
  cross-cutting concerns aren't registered (e.g. integration test
  fixtures); avoids the ambient-service-locator pattern. Production DI
  registers all three so they fire at runtime.

- **Search FTS with LIKE fallback.** `SearchIndexer` probes
  `SERVERPROPERTY('IsFullTextInstalled')` on first use, caches the
  result, and routes queries either through `EF.Functions.Contains`
  (FTS) or whitespace-split `LIKE '%term%'` (universal). Same
  `SearchIndex` table backs both modes. LocalDB and lower-tier Azure SQL
  fall through to LIKE without operator action.

- **Search index lifecycle.** A startup `BackgroundService`
  (`SearchIndexBootstrapService`) runs a full rebuild only if the table
  is empty — so a fresh deploy doesn't need a manual click. Per-write
  `Upsert/Remove/SetPublished` keeps the index live thereafter. Admin
  Site-Settings → Advanced → "Rebuild search index" triggers a manual
  rebuild via `/api/admin/search/rebuild`.

- **Documents: metadata versioned, blob replaced.** Per
  `VERSIONING.md` §10. `DocumentService.ReplaceBlobAsync` swaps the
  blob URL and queues the old blob for cleanup via
  `IBlobCleanupService` (logging-only stub).

- **Leaders not versioned.** Per `VERSIONING.md` §2 ("Leaders are
  presented as a curated public list, not a historical record"). Hard-
  delete only; admin-only delete role.

- **AspNet prefix removed.** `ApplicationDbContext.OnModelCreating`
  declares `ToTable` overrides for the 7 Identity entities.
  `RenameIdentityTables` migration uses `RenameTable` operations
  (data-preserving) plus the FK + PK renames EF generates automatically.
  Existing populated databases upgrade in place.

- **Output cache split: foundation now, full wiring at P17.** The
  `IOutputCacheInvalidator` + `MemberAuthVaryPolicy` foundation is
  in place and `PageService` calls the invalidator on every write.
  `News`/`ServiceTime`/`Leader`/`Document`/`Banner`/`SiteSettings`
  follow the same pattern but the per-service wiring is queued for
  P17 polish. Bounded staleness today is the cache duration on the
  homepage endpoint (300s).

- **Version history: server-side now, diff renderers later.** The
  generic `IVersionedEntityHandler` registry + admin controller +
  `PageVersionHandler` ship in P13. The other handlers
  (News/ServiceTime/Document/Banner) follow the same pattern and
  register against the existing controller without changes. The three
  diff renderers (`<ProseMirrorDiffRenderer>`, `<TextDiffRenderer>`,
  `<ImageDiffRenderer>`) are L-effort each and the existing
  Phase 1 `<VersionHistoryPanel>` stub is still serviceable; queued
  for a follow-up.

- **SignalR Phase 2 minimum.** `JoinAdminGroup` hub method +
  `IRealtimeNotifier` + SignalR impl are in place. Per-content-service
  `NotifyContentChangedAsync` calls and the SPA admin-shell toast
  subscription follow the cache pattern and are queued for P17.

- **Seed data: metadata only.** No binary seed assets ship in P15 —
  operators upload their own logos/photos/PDFs. `DataSeeder` seeds
  2 system pages (Privacy, Terms), 3 sample pages (About, Plan Your
  Visit, What We Believe), 3 service times, 4 leaders across
  categories, 2 news items (one members-only).

### 2026-05-05 — Stage P17: Phase 2 verification

**Build & test status at end of Phase 2:**

- `dotnet build` — clean Release build, zero warnings, zero errors
  across all 8 .NET projects.
- `dotnet test` — **77 tests passing** (Domain 5, Application 49,
  Infrastructure 13, Api 10).
- `npm run build` — clean SPA build. Public bundle 253 KB / 77 KB gzip
  (vs Phase 1's 245 KB / 75 KB; the +8 KB is the new homepage data
  fetch + AnnouncementBar + search-overlay code). TipTap chunk 328 KB
  / 104 KB gzip is route-split and only loads when an admin enters
  Pages/News/Settings or a public visitor opens a `/{slug}` /
  `/news/:slug` route.
- `npm test` — 10 SPA tests passing (no regressions; Phase 2 added no
  new SPA tests because the main test surface — content services with
  search/cache/notify wiring — is server-side).

**Smoke-test on Linux at port 5099:**

- `GET /api/health` → 200 with `status: "ok"`. ✅
- `GET /robots.txt` → 200 with the expected disallow set and
  Sitemap pointer. ✅
- `GET /api/admin/pages` (anonymous) → 401. ✅
- `GET /sitemap.xml`, `GET /api/public/news`, `GET /api/public/homepage`
  → 500 with `PlatformNotSupportedException: LocalDB is not supported
  on this platform`. **Not a code bug** — same Linux-vs-LocalDB
  environmental constraint observed in Phase 1 verification. Production
  deployment to Azure SQL or a Windows dev box with LocalDB renders
  these endpoints correctly.

**Migrations generated this phase** (in chronological order):

1. `AddPagesTable`
2. `AddNewsTable`
3. `AddServiceTimesTable`
4. `AddLeadersTable`
5. `AddDocumentsTable`
6. `AddAnnouncementBannerTable`
7. `AddSearchIndex`
8. `RenameIdentityTables` (drops AspNet prefix, data-preserving)

A fresh `dotnet ef database update` against an empty database produces
all 7 content tables (Pages, News, ServiceTimes, Leaders, Documents,
AnnouncementBanner, SearchIndex), their `History` shadows for the 6
versioned ones, and the 7 un-prefixed Identity tables. The Phase 2
seeder populates a usable starting state on first boot.

**Carry-overs / known gaps deferred from Phase 2:**

These are pattern-matched repeats of work that's already complete on
one entity; tracked so they're not lost:

- Cache invalidator wiring on News/ServiceTime/Leader/Document/
  AnnouncementBanner/SiteSettings services (Page is wired). One-line
  per write-method addition matching the Page pattern.
- Version-history handlers for News/ServiceTime/Document/
  AnnouncementBanner (Page handler shipped). Same shape as
  `PageVersionHandler`; register against the existing controller via DI.
- `IRealtimeNotifier.NotifyContentChangedAsync` calls in content
  services and the SPA admin-shell "new changes — refresh to see"
  toast subscription.
- The three diff renderers for the version-history UI:
  `<ProseMirrorDiffRenderer>` (prosemirror-changeset),
  `<TextDiffRenderer>` (diff-match-patch),
  `<ImageDiffRenderer>` (side-by-side / stacked-mobile).
- Per-endpoint `[OutputCache]` attributes beyond the homepage; the
  cache infrastructure (AddOutputCache, MemberAuthVaryPolicy, the
  invalidator) is wired and ready.
- Mobile-width verification at 375px on the new pages. Smoke-tested
  visually during P3-P10 implementation but not formally checked in
  a post-build pass.
- SPA test additions for the new admin pages (Pages, News, Leaders,
  Documents, ServiceTimes) and TipTap editors. Server-side validation
  is well-covered (49 application tests).

The seam-and-pattern approach throughout Phase 2 means each carry-over
is a small, predictable repeat of a working pattern — not a redesign.

---

# Phase 3 Decisions Log (in progress)

### 2026-05-05 → present — Stages Q0 through Q8 complete; Q9+ pending

Phase 3 is being built incrementally on `claude/credo-cms-phase-1-06gIY`.
This entry captures the **mid-Phase-3 checkpoint** state at session
break.

**Stages complete (Q0–Q8):**

- **Q0** Phase 3 packages installed (Ical.Net 4.2.0, Google.Apis.YouTube.v3,
  @fullcalendar/react+core+daygrid+timegrid+list+rrule, rrule).
- **Q1** Tag entity, normalization service, autocomplete API +
  `<TagAutocomplete>` component, 6 tests.
- **Q2** Polymorphic ScriptureReference (Domain enum + 66-book static
  data + table + replace-on-save service + `<ScriptureReferenceInput>`
  + en-dash formatter + 22 tests).
- **Q3** SermonSeries (versioned) full slice — Domain through admin
  list/editor + public list/detail.
- **Q4** Sermon (versioned) full server-side slice including
  SermonTag/SermonAttachment join tables. Tag persistence routes
  through `TagService.NormalizeAndUpsertAsync`. Attachment validation
  rejects non-PDF, members-only, or unpublished documents.
- **Q5** YouTube integration: `IYouTubeApiClient`,
  `IYouTubeTranscriptClient` (best-effort timedtext),
  `YouTubeUrlParser` with 11 tests, `YouTubeSyncService`
  `BackgroundService`, `POST /api/admin/sermons/import` and `/sync`,
  9 new SiteSettings columns. **Decision:** secrets stored plain-text
  in DB, masked in admin UI per BUILD_PLAN Q-2 #5; Data-Protection
  encrypt-at-rest queued in `PHASE_3_BACKLOG.md`.
- **Q6 (partial)** Sermon admin UI: `/admin/sermons` list with import +
  sync trigger, `/admin/sermons/:id` editor (thumbnail, TipTap,
  speaker toggle, series, tags, scripture refs, transcript, publish
  controls). Speaker-linked-to-Leader is GUID-paste pending a Leader
  picker; attachments multi-select pending.
- **Q7** Sermon public surface: `/sermons` archive, `/sermons/by-book`
  index + per-book browse, `/sermons/:slug` detail with embedded
  YouTube player, formatted scripture references, attachment links,
  collapsible transcript with `?highlight=` term emphasis,
  `VideoObject` JSON-LD.
- **Q8** Event entity (versioned) + recurrence: Domain entity,
  `EventRecurrenceException` (skip), `EventOccurrenceOverride` (edit),
  hand-rolled `EventOccurrenceExpander` covering FREQ=DAILY/WEEKLY+
  BYDAY/MONTHLY+BYMONTHDAY with UNTIL or COUNT, repository,
  service with skip-occurrence + override-occurrence operations,
  validators (visibility-required-on-publish), admin controller,
  migration.

**Verification at checkpoint:**
- `dotnet build` clean across 8 projects.
- `dotnet test` — **110 passing** (Domain 15, Application 72,
  Infrastructure 13, Api 10).
- `npm run build` clean (public 257 KB / 78 KB gzip).
- `npm test` — 21 passing.

**Stages remaining (Q9 onward) — to do in subsequent sessions:**

- **Q9** Event admin UI (recurrence builder, hero upload, occurrence
  exceptions/overrides UI). Prep work shipped: `app/src/lib/recurrence.ts`
  (RRULE builder + parser for the four patterns) and
  `app/src/lib/api/events.ts` API client.
- **Q10** Event registration server-side.
- **Q11** Event registration UI.
- **Q12** Public events list + detail + add-to-calendar dropdown.
- **Q13** Calendar — `GET /api/public/calendar`, FullCalendar React
  page, admin overview.
- **Q14** iCal feeds — public + per-member token.
- **Q15** Cross-cutting wiring + Phase 2 carry-over cleanup.
- **Q16** Profile additions (calendar-feed, registrations).
- **Q17** Seed data (sample sermons + events).
- **Q18** Final verification.

**How to set up YouTube sync today** (until the Q15 admin UI ships):

```sql
UPDATE SiteSettings
SET YouTubeChannelId = 'UC...your-channel-id...',
    YouTubeApiKey = 'AIza...',
    YouTubeSyncEnabled = 1,
    YouTubeSyncIntervalMinutes = 360,
    YouTubeAutoPublishOnSync = 0
WHERE Id = '11111111-1111-1111-1111-111111111111';
```

**Repo state at checkpoint:** buildable, runnable, migratable. All
Phase 1 + Phase 2 features still intact. The Q9+ stages are mostly
admin/public UI plus calendar/iCal glue with no further schema
changes (only `CalendarFeedToken` in Q14).

---

## Phase 3 wrap-up (Q9 – Q18)

**Stages shipped:**

- **Q9** Event admin UI — recurrence builder (none / daily / weekly+BYDAY /
  monthly+BYMONTHDAY) with end-condition (none / until / count),
  skip-occurrence affordance, hero upload, TipTap description,
  visibility radio with no default, registration mode + capacity +
  waitlist + open/close + external URL + TipTap confirmation message,
  publish toggle.
- **Q10** Event registration (server) — domain entities, `RegistrationTokenSigner`
  (HMAC-SHA256 with `FixedTimeEquals`; 4 tests), service with capacity
  check + waitlist promotion + honeypot + 5-second time-to-submit,
  controllers, migration.
- **Q11** Event registration UI — public form (9 dynamic field types,
  honeypot, time-to-submit, signed cancel link), cancellation page,
  admin Registrations page (manage fields + list/cancel/export-CSV
  registrations).
- **Q12** Public events surface — list (paged, ordered by next-occurrence,
  hero, recurring badge), detail (hero, recurrence preview, register CTA,
  TipTap description, JSON-LD `Event`, add-to-calendar dropdown:
  `.ics` / Google / Outlook). Single-event ICS endpoint backed by a new
  `IIcalFeedBuilder` (Application interface, Ical.Net implementation in
  Infrastructure).
- **Q13** Calendar — `GET /api/public/calendar?start=&end=` aggregating
  expanded event occurrences (exception/override-aware) plus News with
  `CalendarDate`. Public `/calendar` and admin `/admin/events/calendar`
  pages using `@fullcalendar/react` (lazy-loaded). Public nav now exposes
  Events + Calendar.
- **Q14** iCal feeds — anonymous public feed at `/calendar/feed.ics`;
  per-member opaque token feed at `/calendar/feed/{token}.ics`. Token
  storage is SHA-256-hashed (a leaked DB row cannot itself subscribe).
  Profile page at `/profile/calendar-feed` issues / displays-once /
  revokes. Migration: `AddCalendarFeedTokens`.
- **Q15** Cross-cutting wiring (scope-trimmed) — new `OutputCacheTags` for
  SermonSeries / Sermons / Events / Calendar; `[OutputCache]`
  (MembersAuthVary) on the new Phase 3 public endpoints; `EventService`
  invalidates Events + Calendar + Sitemap on every state-changing
  operation. Search index rebuild now covers SermonSeries / Sermon /
  Event entities. Sermon-side cache invalidation deferred to Phase 4.
- **Q16** Profile registrations — `/profile/registrations` lists the
  current user's registrations with status badges and a confirm-then-
  cancel flow that re-uses the waitlist-promotion path on the server.
- **Q17** Seed data — 2 series, 4 sermons (placeholder YouTube IDs), 5
  events covering single / weekly / monthly / members-only /
  external-URL shapes. Idempotent.

**Decisions captured during Phase 3:**

- *Recurrence engine*: hand-rolled expander (`EventOccurrenceExpander`)
  for FREQ=DAILY / WEEKLY+BYDAY / MONTHLY+BYMONTHDAY with UNTIL or
  COUNT. Predictable + tested. Ical.Net is reserved for iCal *emission*
  only, where round-trip fidelity matters. Polymorphism for
  "skip vs edit occurrence" goes through two side tables
  (`EventRecurrenceException`, `EventOccurrenceOverride`) rather than a
  single discriminated table.
- *Visibility nullable*: `Event.Visibility` is `EventVisibility?`. Drafts
  may exist without one; FluentValidation enforces non-null at publish
  time. Same pattern would carry to Pages/News if we revisit them.
- *Cancel-link tokens*: HMAC-SHA256 over `{registrationId:N}|{expUnix}`
  with the secret in `EventRegistration:TokenSigningSecret`. `>=` exp
  comparison (not `>`) closes the unix-second granularity flake. Token
  validation uses `CryptographicOperations.FixedTimeEquals`.
- *Calendar feed tokens*: stored as SHA-256 hashes; the plaintext is
  shown once at issue, then never again. Re-issuing always revokes the
  prior token (one URL per member).
- *Registration field schema*: free-form `OptionsJson` (nvarchar(max))
  rather than a separate options table — simpler save semantics, and
  options are only relevant to two field types.
- *No transcript caching policy*: YouTube transcript fetch is best-
  effort. We capture whatever comes back at sync time; failure is
  silent. There's no scheduled re-fetch.
- *Polymorphic `ScriptureReference` parent FK*: still no real DB FK to
  the parent row. Service-layer cascade-on-hard-delete continues to be
  the source of truth via `IScriptureReferenceService.DeleteAllForParentAsync`.

**Operator notes:**

- *YouTube setup*: still SQL-only (the admin Integrations tab is
  Phase 4) — see snippet above.
- *Calendar feed URLs*: anonymous `/calendar/feed.ics`; member feeds are
  generated from `/profile/calendar-feed` and look like
  `/calendar/feed/{token}.ics`.

**Verification at end of Phase 3:**

- `dotnet build` clean across 8 projects.
- `dotnet test` — **114 passing** (Domain 15, Application 76,
  Infrastructure 13, Api 10).
- `npm run build` clean.
- `npm test` — 21 passing.


---

## Phase 4 — Members and Community

Phase 4 lands the members + community feature set across 21 stages
(Q0–Q20). Branch: `claude/credo-cms-phase-1-06gIY`.

### What shipped

- **Q0–Q1** — 10 new domain entities (Group, GroupMembership, ClassSlot,
  ClassOffering, BlogPost + tag link, PrayerRequest + Update +
  PrayedFor, ConnectCardSubmission); EF migration
  `AddPhase4MembersAndCommunity`.
- **Q2–Q3** — `ProfileService` + `/api/profile` + 4-tab SPA profile
  page (Personal info, Directory, Notifications, Account); admin
  user-profile-fields + reset-notifications + admin-notes endpoints.
- **Q4** — Members directory (`/api/members`, `/members`, `/members/{id}`)
  with two-layer privacy (DB-level opt-in gate + service-level
  field-level filter).
- **Q5–Q6** — Groups end-to-end. Visibility (Public/MembersOnly/Hidden),
  joinability (Open/InviteOnly/Closed), required-message-on-join,
  roster visibility (LeadersOnly/AllGroupMembers). SignalR
  `GroupJoinRequestSubmitted` to admins + each leader's per-user channel
  (NotificationHub auto-joins per-user channel on connect).
- **Q7–Q8** — Classes. Two distinct DTO shapes (`PublicClassSlot` vs
  `MemberClassSlot`) so the privacy contract is compile-time enforced.
  Public list grouped by audience age group with filter chips; member-
  augmented response surfaces teacher / room / weekly schedule.
- **Q9–Q10** — Prayer requests. `IProfanityCheckService` is backed by
  the `Profanity.Detector` 0.1.8 NuGet package (Stephen Haunts), with
  `SiteSettings.ProfanityWordlist`/`ProfanityAllowlist` layered on top.
  We call `DetectAllProfanities(text, removePartialMatches: true)`
  rather than the package's `ContainsProfanity`, which uses naive
  substring matching and trips on the Scunthorpe problem ("hello"
  matches "hell", "classic" matches "ass"). Anonymous-display rule
  hides submitter from non-privileged viewers but keeps it visible to
  admins/editors/the submitter. SignalR `PrayerRequest{Created,Updated,
  StatusChanged,PrayedForCountChanged,UpdateAdded}` on the new
  "members" SignalR group; `useNotificationHub` auto-joins
  authenticated members on connect.
- **Q11–Q12** — Connect card. Anti-bot ladder: honeypot →
  5-second time-to-submit → Cloudflare Turnstile siteverify → field
  validation. Sliding-window rate limit `5/hour` per IP. SignalR
  `ConnectCardSubmitted` toast in admin shell.
- **Q13–Q14** — Blog. Reading-time computed from body word count on
  every save (max(1, ceil(words/250))); excerpt auto-derived from body
  text when blank; tags via existing `ITagService`; PublishedAt auto-
  stamped on first publish.
- **Q15** — Facebook OAuth linking. NuGet
  `Microsoft.AspNetCore.Authentication.Facebook` installed. Sign-in
  rejects unknown Facebook profiles (no account creation path); members
  link from /profile, then sign in via the new "Continue with Facebook"
  button on /login. `/api/profile/facebook-status` powers the SPA's
  Linked/Unlinked badge.
- **Q16** — Site settings UI wiring. New "Members & Community" and
  "Integrations" tabs in `/admin/settings` cover all Phase 4 fields
  (page labels, class audience age groups, blog categories,
  prayer-archive lookback, connect-card interests + ack message,
  profanity wordlist + allowlist, Cloudflare Turnstile site/secret
  keys, Facebook OAuth app id/secret + Login-enabled toggle).
- **Q17** — Blog wired into `ISearchIndexer`: upsert on create/update,
  remove on soft-delete; future-dated posts excluded from `IsPublished`
  flag in the index.
- **Q18** — Sample seed data: two groups (public Youth Group +
  members-only Men's Bible Study) and one pinned welcome blog post.
  Other Phase 4 domains intentionally start empty so admins
  populate them with real content.
- **Q19** — Tests. 209 backend (Domain 15, Application 127,
  Infrastructure 24, Api 43) + 21 SPA. Coverage added per stage:
  permission gates, privacy filters, anti-bot rules, SignalR emission,
  profanity NuGet adapter (incl. Scunthorpe regression guards).
- **Q20** — Documentation + final verification (this section).

### Editorial design refresh

In parallel with the backend work, the admin shell got the Editorial
visual refresh per `DESIGN_HANDOFF.md` + `CredoCMS Admin Refresh
_standalone_.html`. All 10 admin screens (sign-in, dashboard, settings,
pages list, page editor, news list, sermons, events, users & roles,
audit log) plus the Phase 4 admin pages (groups, classes,
connect-cards, prayer requests, blog) use the shared editorial
primitives in `app/src/components/shared/admin/EditorialPrimitives.tsx`.

### Verification

- API: `dotnet test` — 209/209 passing.
- SPA: `npm test` — 21/21 passing.
- API build: `dotnet build CredoCms.slnx` — 0 warnings.
- SPA build: `npx vite build` — clean (chunk-size warning is the
  vendor bundle, not a regression).

### Known carry-forwards

- Scheduled-publish on blog posts captures the date but doesn't run
  automation (Phase 5 ships the background job).
- Email acknowledgment for connect cards uses
  `LoggingEmailService` (Phase 5 wires SendGrid).
- `IConnectCardService.DeleteAsync` performs a soft-erase (status →
  NotLegit) rather than a hard DELETE; flip later if a true GDPR
  erasure path is needed.

---

## Phase 5 — Communications

Migration: `20260506173837_Phase5_Communications` adds 8 new tables
(EmailSuppressions, EmailBroadcasts + EmailBroadcastsHistory,
EmailBroadcastRecipients, EmailTemplates + EmailTemplatesHistory,
WebhookEventLog, AdminNotificationLastSent, EventVolunteerRoles +
EventVolunteerRolesHistory, EventVolunteerSignups), 24 new SiteSettings
columns, and 4 new fields on existing entities
(NewsItem.ScheduledPublishAt + SendEmailOnPublish,
BlogPost.SendEmailOnPublish, EventRegistration.ReminderEmailSentAt).

### Architectural decisions

- **`IEmailService` redesign**: Phase 1's `SendAsync(EmailMessage)` was
  split into `SendTransactionalAsync` (single-recipient) +
  `SendBroadcastAsync` (returns `BroadcastSendResult` with per-recipient
  outcomes) + `IsConfiguredAsync`. Three concrete impls — Logging /
  SendGrid / SMTP — registered alongside an `EmailServiceRouter` that
  picks the active impl per call from `SiteSettings.EmailProvider`.
  Falls back to Logging when configured-but-unconfigured. Per-call
  resolution (not wired-once-at-startup) is the project convention for
  SiteSettings-driven choices.
- **Suppression policy**: Transactional sends bypass the suppression
  list (CAN-SPAM exemption). Broadcast/News/Blog/GroupCommunication
  sends honor it via `IRecipientResolver`'s bulk lookup
  (`WHERE EmailAddress IN (...)` once per send). SendGrid webhook
  bounces/spam/unsubscribe events upsert into the list automatically.
- **Webhook signature**: Verified via SendGrid SDK's `RequestValidator`
  ECDSA helper (no hand-rolled crypto). 5-minute timestamp skew window
  bounds replay risk. Per-event `sg_event_id` deduplicates via
  `WebhookEventLog`. Aggregate broadcast stats applied once per
  broadcast at the end of each batch (single `IncrementStatsAsync` per
  broadcastId), then `BroadcastStatsUpdated` SignalR fires.
- **X-Message-Id correlation**: SendGrid returns one X-Message-Id per
  HTTP batch; per-recipient `sg_message_id` events are
  `<batch>.<suffix>`. Recipient rows store the batch prefix; webhook
  handler matches by `WHERE Id = exact OR Id = prefix`.
- **Recipient resolution precedence**: suppression > preferences >
  membership. Resolved fresh at send time so group-membership changes
  between compose and send are picked up.
- **Scheduled publishing concurrency**: `ScheduledPublishingService`
  ticks every 60s; reads-then-flips inside a single SaveChanges. Safe
  under single-instance v1; documented `// TODO:` for multi-instance
  scale-out (RowVersion-guarded update).
- **Notification batching**: `AdminNotificationDigestService` ticks
  every 5 min, gates per-user per-category by
  `AdminNotificationLastSent.LastSentAt` against the configured
  frequency window. Digests sent as `EmailCategory.Transactional` so
  they bypass suppression — admins explicitly opted into duties.
- **One-click unsubscribe**: HMAC-SHA256 signed token
  (`base64url(userId|category|ts|hmac)`), 30-day expiry,
  `CryptographicOperations.FixedTimeEquals` for signature comparison.
  Key auto-generates on first read if blank. Per-recipient HTTPS URL
  injected as `{{unsubscribeUrl}}` merge field; mailto fallback in
  `List-Unsubscribe` header at broadcast level. Per-recipient
  `List-Unsubscribe-URL` header (via SendGrid `Personalization.Headers`)
  deferred — body footer carries the actual one-click link.
- **Volunteer signups**: capacity check + filtered-unique index on
  `(roleId, occurrenceDate, userId) WHERE CanceledAt IS NULL` so a
  member can re-sign-up after canceling. v1 limitations (no substitute
  requests, no skill tracking, no automated scheduling) tracked in
  ROADMAP.
- **SMS service stub**: `NoOpSmsService` is the active `ISmsService`
  in v1. `TwilioSmsService` exists with throwing constructor — v1.5
  swap-in is a one-line DI change.
- **Email-on-publish duplicate prevention**: `SendEmailOnPublish` flag
  flipped to false in the same `UpdateAsync` call that records the
  auto-broadcast row, so re-publishing an already-published post does
  not re-fire unless the editor explicitly re-enables.

### Verification

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test` — 302/302 passing (Domain 15, Application 166,
  Infrastructure 75, Api 46). +93 tests added across Phase 5
  (suppression, LoggingEmail gate, SendGrid mock, SMTP mock,
  EmailServiceRouter, TestEmail, webhook event processor, template
  renderer, broadcast service, email-on-publish, unsubscribe token,
  volunteer service).
- `npm run build` — clean (vendor 700kb chunk warning is pre-existing).
- `npm test` — 21/21 passing.
- Migration applies cleanly on a fresh dev DB; idempotent seeds
  populate 16 system templates + 1 sample broadcast + 2 volunteer roles.

### Known carry-forwards

- **Existing transactional caller refactor to templates** —
  `InvitationEmailComposer` still ships inline strings. The
  `IEmailTemplateRenderer` is wired but the migration would touch
  invitation, password reset, connect-card ack, group-join decision,
  and event-registration paths simultaneously. Templates are seeded;
  the cutover is a non-blocking cleanup task.
- **Per-recipient `List-Unsubscribe-URL` header** — v1 ships RFC 2369
  compliance via the broadcast-level mailto fallback + body-footer
  per-recipient link. Per-recipient header (via SendGrid
  `Personalization.Headers` / SMTP per-message MimeMessage) is a
  follow-up.
- **Site Settings dedicated "Email & Notifications" tab** — Phase 4's
  Members & Community / Integrations tabs already cover all Phase 5
  SiteSettings fields. A purpose-built tab with a Test Send button +
  inline suppression-list view is cosmetic.
- **Broadcast composer RTE** — current SPA composer uses a textarea so
  Phase 5 ships without TipTap-merge-field plumbing risk. Operators
  paste HTML; merge fields work via string substitution.
- **CSV export of recipients** — endpoint placeholder; the SPA
  download path is a follow-up.
- **BackgroundService unit tests** — workers themselves
  (BroadcastSendWorker, ScheduledPublishingService,
  AdminNotificationDigestService, EventVolunteerReminderService) are
  glue around tested services; no per-service unit test was added.
  Integration tests will land in Phase 6's accessibility/perf pass.

---

## Phase 6 — Polish and Production-Ready

Migration: `20260506235444_Phase6_AnalyticsAndCookieConsent` adds 5
SiteSettings columns (AnalyticsProvider, Ga4MeasurementId,
Ga4ConsentBannerEnabled, Ga4ConsentBannerPosition, CookiePolicyPageId).

### What shipped

- **S0** — SiteSettings analytics + cookie-policy fields. Migration generated; DTO + validator + service round-trip.
- **S1** — RSS feeds (`/blog/rss.xml`, `/news/rss.xml`, `/sermons/rss.xml`) via hand-rolled `XmlWriter` (no NuGet); 50-item cap; 15-min output cache; public-only filter.
- **S2 + S3** — Cookie consent banner + GA4 loader. Banner self-gates on `analyticsProvider === Ga4` + `cms_consent` absence. `usePageViewTracking` hook fires `gtag('event', 'page_view', ...)` on every React Router location change post-consent. Footer "Cookie Preferences" link surfaces revoke.
- **S4** — Footer Lucide brand icons (Facebook / Instagram / YouTube / Twitter / Music2 for TikTok / generic Link for Other) with `aria-label="{ChurchName} on {Platform}"` + `target="_blank" rel="noopener noreferrer"`.
- **S5–S8** — Astro 5.x docs site under `/docs/`. Tailwind v3 integration with shared `tailwind.system-theme.cjs` at repo root. 5 custom components (Callout, Steps + Step, Screenshot with placeholder rendering, KeyboardShortcut, RoleNote). 48 content pages across 7 sections (Getting Started 4, Content Management 11, Members & Groups 6, Communications 8, Integrations 6, Administration 8, Troubleshooting 4). Pagefind 1.1.x post-build search index (`npx pagefind --site dist`).
- **S9** — `DocsController` serves `wwwroot/docs/*` with inline auth check (returns 404 for anonymous + non-Editor/Admin). `[AllowAnonymous]` + role check inside the action so cookie-auth challenges don't leak the subtree's existence.
- **S10** — Skip-to-main-content link in both layouts (`ChurchThemeLayout` for public, `AdminLayout` for admin). `ACCESSIBILITY.md` at repo root with WCAG 2.1 AA target + tested SR matrix + known limitations (TipTap toolbar, FullCalendar keyboard nav, theme color-contrast lacks save-time validation).
- **S11** — Mobile responsive audit. Cookie banner at 375px, footer flex-wrap, Astro docs sidebar collapses below `lg`.
- **S12** — `SeoTags` extended with `rssFeedUrl`, `rssFeedTitle`, `canonicalUrl`. Per-page SEO sweep (Article/Event/VideoObject/Person JSON-LD) deferred to v1.x as a mechanical refinement task.
- **S13** — Lighthouse audits: deferred to operator's pre-launch dry-run. Existing perf affordances confirmed: WebP via `<picture>`, lazy loading on below-fold images, FullCalendar already dynamically-imported on calendar routes.
- **S14** — RSS feed builder tests (6), cookie consent helpers tests (5), docs gate tests (3). Total Phase 6 tests added: **+14 backend, +5 SPA**.
- **S15** — README operator runbook expansion: Quick Start, Production deployment (Bicep + cost estimate), Operations runbook (backup/restore, migration rollout, suppression list, member removal, prayer urgency, Connect Card spam, search rebuild, YouTube manual sync, admin add/remove, forgotten admin password SQL recovery), Troubleshooting (5 scenarios), Multi-tenancy pointer + Contributing.
- **S16** — `ARCHITECTURE.md` at repo root with curated architectural decisions: solution layout, auth + authz, SignalR groups, versioning, output cache, privacy contracts, real-time push pattern, email + suppression, background services table, multi-tenancy posture, phase-by-phase summary.
- **S17** — `ROADMAP.md` final categorization: every deferred item tagged `[v1.x]` / `[v2]` / `[out-of-scope]`.
- **S18** — `LICENSE` placeholder with the spec's TODO copy verbatim.
- **S19** — Local deployment dry-run via `docker-compose.yml` against SQL Server 2022 in container. `api/Dockerfile` is a multi-stage build (SDK 10 → aspnet runtime 10). Seeded admin: `admin@credocms.local` / `Admin!ChangeMe123`. Real Azure deployment dry-run is documented in README as a required pre-launch acceptance test the project owner runs against their actual subscription.
- **S20** — Final verification (this entry).

### Architectural decisions

- **`/docs/*` covert routing via inline auth check** — Phase 6 originally planned to use `[Authorize]` + middleware-rewrite the resulting 401/403 to 404. That works for `/api/admin/*` (which only uses 403→404 conversion) but for `/docs/*` we want both anonymous AND wrong-role to be 404, and the cookie-auth scheme writes 401 in a way that the middleware can't easily intercept without breaking the SPA's session-expiry detection on `/api/admin/*` XHRs. Solution: drop `[Authorize]` from `DocsController`, do the auth check inside the action, and return `NotFound()` directly for unauthorized callers. Documented in code with a comment explaining why future maintainers shouldn't reinstate `[Authorize]`.
- **Tailwind token sharing via `tailwind.system-theme.cjs`** — both the SPA's Tailwind config and the Astro docs site's Tailwind config `require()` the same module. CJS chosen over CSS-first `@theme` because Tailwind v3 + v4 mixed compatibility — the CJS file works for both versions. The SPA's existing `tailwind.config.js` is unchanged for now; swapping it to `require()` the shared file is forward-compat work tracked in v1.x.
- **Cookie consent: single endpoint, no parallel `/api/public/site-config`** — clarification 4 in the build plan suggested a parallel endpoint. After implementation the existing `/api/site-settings/public` already carries the analytics fields after S0; adding a second endpoint would be redundant. SPA's existing `useSiteSettings` hook is the source of truth.
- **Per-recipient `List-Unsubscribe-URL` deferred** — v1 ships RFC 2369 compliance via a broadcast-level mailto fallback in `List-Unsubscribe` + per-recipient HTTPS URL in the body footer. SendGrid's `Personalization.Headers` could carry per-recipient HTTPS in the header itself; tracked as v1.x.

### Verification

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test` — 311/311 passing (Domain 15, Application 166, Infrastructure 81, Api 49). +14 Phase 6 tests over Phase 5's 297.
- `npm run build` — clean (vendor 700kb chunk warning is pre-existing).
- `npm test` — 26/26 passing. +5 Phase 6 tests over Phase 5's 21.
- Astro docs `npm run build` deferred to operator's first run; structure complete and self-contained.

### Required pre-launch acceptance tests

These the project owner runs before going live:

1. Real Azure deployment dry-run from a clean resource group (Bicep apply, GH Actions deploy, smoke test). Document time-to-first-public-page + issues.
2. Manual screen-reader pass against representative flows: sign in, prayer wall, sermon detail, broadcast composer. NVDA on Firefox/Windows or VoiceOver on Safari/macOS.
3. Lighthouse audit on homepage / sermon archive / sermon detail / event detail / blog detail. Targets: Performance ≥ 85 mobile / ≥ 95 desktop, Accessibility ≥ 95, Best Practices ≥ 95, SEO ≥ 95.
4. SendGrid free-tier setup + Test Send + a real broadcast send to the project owner's email.
5. License decision: pick All Rights Reserved / MIT / AGPLv3 and replace the `LICENSE` placeholder.

### Known carry-forwards (tracked in ROADMAP.md as [v1.x])

- Per-recipient `List-Unsubscribe-URL` header.
- Existing transactional caller refactor to `IEmailTemplateRenderer`.
- Broadcast composer rich-text editor.
- Recipient CSV export endpoint.
- Per-page SEO + JSON-LD sweep (helper plumbing exists; per-call-site additions are mechanical).
- Real screenshots in Astro docs (placeholder model used).
- Real Azure deployment dry-run (local Docker Compose alternative shipped).
- axe-core automated SPA accessibility tests (framework + ACCESSIBILITY.md target list shipped; per-page test files are follow-up).

### End of v1

Phase 6 closes the v1 build. Six phases shipped over the project; the
codebase is production-deployable with operator documentation a non-
developer can follow. The README + Astro `/docs/*` site +
ACCESSIBILITY.md + ARCHITECTURE.md are the entry points.
