# Credo CMS — Build Plan

**Phase 1:** Implemented and merged. See section "Phase 1 Build Plan" below.
**Phase 2:** Awaiting review. No Phase 2 implementation work has started.
See section "Phase 2 Build Plan" at the bottom of this document.

---

# Phase 1 Build Plan

**Status:** Implemented. Branch `claude/credo-cms-phase-1-06gIY` ships all
Phase 1 deliverables; 28 .NET tests + 10 SPA tests green; API boots and
serves `/api/health`; SPA builds clean. Retained here for traceability.

This plan breaks Phase 1 into ordered, concrete implementation steps. It is intentionally
sequenced so each step leaves the repository in a buildable state and so downstream steps
can layer on prior work without rework.

---

## 0. Confirmation of Understanding

I have read all 22 sections of the Phase 1 prompt plus the Out-of-Scope and Final Checklist
sections. My understanding of the foundational decisions:

- **Architecture:** four-project ASP.NET Core 10 solution (`Api`, `Application`, `Domain`,
  `Infrastructure`) with a strict reference graph: `Application` may not reference
  `Infrastructure` or EF Core. Database access is exposed via interfaces in `Application`,
  implemented in `Infrastructure`.
- **Identity:** ASP.NET Core Identity with `Guid` user IDs, custom `ApplicationUser`,
  cookie auth (not JWT), three seeded roles, 12-character password rules, lockout after
  5 failures within 15 minutes for 30 minutes, sliding 8-hour sessions.
- **Versioning:** SQL Server temporal tables driven by `OnModelCreating`, with a global
  `SaveChangesInterceptor` populating `ModifiedByUserId` / `ModifiedAt` from
  `ICurrentUserService`. Phase 1 builds the *infrastructure* — no entity is yet versioned.
- **Theming:** two coexisting themes (church + system) implemented as scoped CSS-variable
  bundles selected by layout component. Tailwind consumes the variables, so shadcn
  inherits them automatically.
- **Covert 404 pattern:** `/admin/*` and `/docs/*` (and their `/api/*` counterparts) return
  the church-themed 404 / a 404 HTTP response when the caller lacks Editor or Administrator
  role, regardless of authentication state. Member-area routes redirect to `/login` when
  anonymous and 404 when authenticated-but-wrong-role. The API enforces this; the SPA
  mirrors it for UX.
- **Phase boundaries:** no content types beyond `SiteSettings` and `ApplicationUser` are
  built. Email is logged via `LoggingEmailService` (real SendGrid is Phase 5). Logo upload
  is a URL-string stub. The version history UI shell exists but is unused.

---

## 1. Clarifications I'd Like to Surface Before Starting

These are points where I have a default plan but want to flag the decision so you can
redirect before I commit code. Where I list a default, that is what I will do unless you
override.

1. **.NET 10 SDK availability.** The prompt specifies ASP.NET Core 10 / `net10.0`. .NET 10
   is on track for general availability around the time of this build, but the dev
   container may or may not have the SDK pre-installed. **Default:** target `net10.0` and
   install the SDK in the build environment if missing. If .NET 10 is not yet available in
   the target environment, I will pause and ask rather than silently downgrade.
2. **`RequirePasswordChangeOnFirstLogin` flag.** This is not a stock Identity property.
   **Default:** add it as a `bool` column on `ApplicationUser`, enforced by a check in
   the auth flow that, after sign-in, redirects to a force-change-password screen if true.
3. **Force-logout mechanism.** **Default:** use Identity's `SecurityStamp` —
   `UserManager.UpdateSecurityStampAsync` invalidates issued cookies on the next validation
   tick. I will set `SecurityStampValidator` `ValidationInterval` to a short window
   (e.g., 1 minute) so force-logout takes effect quickly.
4. **`ProtectedRoute` modes.** The prompt names two modes (`'admin'`, `'member'`). Profile
   and other any-authenticated-user routes need a third behavior (redirect-to-login but no
   role requirement). **Default:** add a third mode `'auth'` that redirects on anonymous
   and never 404s. I'll document this in `IMPLEMENTATION_NOTES.md`.
5. **Session-expiry modal — how does the SPA know when the session expires?** **Default:**
   have `GET /api/auth/me` return `expiresAtUtc` alongside user info. The hook computes
   "now + remaining" and sets a timer for `expires - 5m`. On any 2xx response from
   authenticated endpoints, the API also returns `expiresAtUtc` via a custom response
   header so the SPA can re-arm the timer without an extra round-trip.
6. **Hard-delete cascade for users.** AuditLog rows reference `UserId`. **Default:**
   `UserDisplayNameSnapshot` is captured on write, the FK is nullable, and the relationship
   is configured `OnDelete(SetNull)` so historical entries survive a hard delete with name
   intact but `UserId = null`.
7. **Treat-warnings-as-errors scope.** **Default:** apply
   `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to all four API projects and the
   four test projects via a shared `Directory.Build.props`. I will exclude generated EF
   migration files via `<NoWarn>` if they emit nuisance warnings.
8. **CORS in development.** SPA is served from the API's `wwwroot` in production, but Vite
   dev server runs on a separate port. **Default:** enable a permissive CORS policy
   *only* in `Development` for `http://localhost:5173`, with credentials allowed.
9. **Cookie name.** **Default:** `.CredoCms.Auth` rather than the Identity default name.
   Documented in `appsettings.template.json`.
10. **Tests against temporal tables.** EF Core's in-memory provider does not support
    temporal tables, which complicates testing the interceptor's interaction with history.
    **Default:** the interceptor unit test mocks `ICurrentUserService` and verifies the
    property setter against a non-temporal entity using the in-memory provider. A separate
    integration test against SQL Server LocalDB exercises actual temporal-table writes.
    If LocalDB isn't available in the test environment, the LocalDB-dependent tests are
    skipped via `[Fact(Skip=...)]` with an explanatory message rather than failing.

If any of these defaults are wrong, please correct them in the review and I'll adjust
before starting.

---

## 2. Ordered Implementation Steps

Each step ends with a verifiable state. Effort buckets are rough: **S** ≈ <1h focused,
**M** ≈ 1–3h, **L** ≈ 3–6h, **XL** ≈ 6h+. Estimates are informational, not commitments.

### Stage A — Repository Scaffolding & Docs

| # | Step | Effort |
|---|---|---|
| A1 | Create top-level directory structure (`/api`, `/spa`, `/deploy`, `/docs`, `/.github/workflows`). | S |
| A2 | Write comprehensive `.gitignore` covering .NET, Node, VS, VS Code, OS, env, LocalDB, secret files. | S |
| A3 | Write `README.md` (overview, prerequisites, quick-start, architecture summary, doc links). | S |
| A4 | Write `VERSIONING.md` (temporal tables, `IVersionedEntity`, `ICurrentUserService`, restore semantics, destructive-migration warning, blob-replace pairing). | M |
| A5 | Write `MULTI_TENANCY.md` (what's already designed for it, what's deferred, safe-incremental refactor notes). | M |
| A6 | Write `ROADMAP.md` (full deferred-feature list verbatim from prompt section 21). | S |
| A7 | Create `IMPLEMENTATION_NOTES.md` skeleton — running log to be appended throughout. | S |
| A8 | Write `appsettings.template.json` documenting every config key with example values and notes. | S |

**Verifiable state:** all docs render; `git status` clean; nothing builds yet but the
foundation for navigation is in place.

### Stage B — API Solution & Project Skeleton

| # | Step | Effort |
|---|---|---|
| B1 | Create `CredoCms.sln` with the four production projects + four test projects. Wire reference graph. Add `Directory.Build.props` (target framework, nullable, treat-warnings-as-errors, common analyzers). | M |
| B2 | Add NuGet package references: EF Core SQL Server, Identity EF Core, Serilog (+ console + file + AppInsights sinks), FluentValidation, Swashbuckle, SignalR, Azure SignalR, xUnit/FluentAssertions/Moq for tests. | S |
| B3 | Verify `dotnet build` succeeds with zero warnings. | S |

**Verifiable state:** solution opens in Visual Studio, `dotnet build` is clean.

### Stage C — Domain & Application Layers

| # | Step | Effort |
|---|---|---|
| C1 | `Domain`: `ApplicationUser : IdentityUser<Guid>`, `ApplicationRole : IdentityRole<Guid>`, `IVersionedEntity`, `SiteSettings`, `AuditLogEntry`, `SystemConstants` (fixed System User Guid, fixed SiteSettings Guid). | M |
| C2 | `Application`: interfaces — `IApplicationDbContext`, `ICurrentUserService`, `IAuditLogger`, `IEmailService`. | S |
| C3 | `Application`: DTOs (auth, user-management, site-settings, audit-log) + FluentValidation validators. | M |
| C4 | `Application`: services — `UserManagementService`, `SiteSettingsService`, `AuditLogService` (read-side queries only; writes go through `IAuditLogger`). | M |

### Stage D — Infrastructure Layer

| # | Step | Effort |
|---|---|---|
| D1 | `ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` implementing `IApplicationDbContext`. Configure temporal-table convention helper for future versioned entities; apply `IsTemporal()` only where appropriate (none in Phase 1, but the helper exists). | M |
| D2 | EF entity configurations for `SiteSettings`, `AuditLogEntry`. Configure `RowVersion`, indexes, FK behaviors (User → AuditLog = `SetNull`). | M |
| D3 | `SaveChangesInterceptor` populating `ModifiedByUserId`/`ModifiedAt` for any `IVersionedEntity` writes. Unit-tested. | M |
| D4 | `CurrentUserService` reading from `IHttpContextAccessor`. | S |
| D5 | `AuditLogger` writing structured JSON `Details` and capturing `UserDisplayNameSnapshot` + `IpAddress`. | S |
| D6 | `LoggingEmailService` writing email content to Serilog. | S |
| D7 | `DataSeeder`: idempotent first-run seeding of roles, default admin user, system user, and Site Settings row. | M |
| D8 | Initial EF migration `Initial` covering Identity tables + `SiteSettings` + `AuditLogEntry`. | S |
| D9 | Versioning trim `BackgroundService` registered but inert (no versioned entities yet). | S |

**Verifiable state:** `dotnet ef migrations add` succeeds; `dotnet ef database update`
creates the schema cleanly.

### Stage E — API Layer

| # | Step | Effort |
|---|---|---|
| E1 | `Program.cs`: Serilog bootstrap, configuration, DI composition (DbContext, Identity, services, interceptor, hosted services), cookie auth options, rate limiting policies, SignalR + optional Azure SignalR, Swagger, FluentValidation auto-validation, controllers, static files for SPA, fallback to `index.html`. | L |
| E2 | Middleware: `ForbiddenToNotFoundMiddleware` mapping 403 → 404 for `/api/admin/*` and `/api/docs/*`. Order it after authorization. | S |
| E3 | `AuthController`: `login`, `logout`, `me` (returns user + roles + `expiresAtUtc`), `forgot-password`, `reset-password`, `accept-invitation`. Wire rate-limit policies. Update `LastLoginAt` on success. Audit-log login success/failure, logout. | L |
| E4 | `UsersController` (Administrator, under `/api/admin/users`): list + filter + search, create (with invitation), update, soft-deactivate, reactivate, hard-delete (with confirmation token), force-logout (rotate `SecurityStamp`), trigger password-reset email. Audit-log every mutation. | L |
| E5 | `AuditLogController` (Administrator, under `/api/admin/audit-log`): paginated list with filters, detail fetch. | M |
| E6 | `SiteSettingsController` (Administrator, under `/api/admin/site-settings`): get, update (optimistic concurrency via `RowVersion`). Public `GET /api/site-settings/public` returns only the brand/contact subset for unauthenticated SPA bootstrap. Audit-log updates. | M |
| E7 | `NotificationHub` mapped at `/hubs/notifications`, `[Authorize]`, no methods yet. | S |
| E8 | Response header middleware emitting `X-Session-Expires-At` on authenticated 2xx responses. | S |

### Stage F — API Tests

| # | Step | Effort |
|---|---|---|
| F1 | `Domain.Tests`: example test asserting `ApplicationUser.DisplayName` composition. | S |
| F2 | `Application.Tests`: test for `UserManagementService` create-with-invitation flow using mocked `IApplicationDbContext`, `IEmailService`, etc. | M |
| F3 | `Infrastructure.Tests`: interceptor sets `ModifiedByUserId`/`ModifiedAt`. Optional LocalDB-backed temporal-table integration test, gated on environment. | M |
| F4 | `Api.Tests`: integration tests using `WebApplicationFactory<Program>` for `/api/auth/login` (success, lockout, bad-creds), `/api/auth/me` (anonymous → 401), `/api/admin/users` (anonymous → 404 via the covert-404 middleware). | L |

**Verifiable state:** `dotnet test` green at the API tier; covert-404 behavior verified.

### Stage G — SPA Foundation

| # | Step | Effort |
|---|---|---|
| G1 | `npm create vite` (React + TS), strict `tsconfig.json`, ESLint + Prettier, Tailwind init, shadcn init, install `lucide-react`, `react-router-dom`, `@microsoft/signalr`. | M |
| G2 | Theming: `church-theme.css`, `system-theme.css`, `<ChurchThemeLayout>`, `<SystemThemeLayout>` setting `data-theme`; Tailwind config consuming CSS variables. | M |
| G3 | Foundational utilities: `useBreakpoint` hook, `<ResponsiveTable>` component (table on desktop, cards on mobile, built-in pagination + sort + search slot). | L |
| G4 | API client: `apiClient.ts` (`apiGet/Post/Put/Delete`, 401 event), per-feature wrappers in `/lib/api/*`, types in `/types/api.ts`. | M |
| G5 | Auth: `AuthContext` + `useAuth`, login page, forgot-password page, reset-password page, accept-invitation page, force-change-password page, session-expiry modal, form-state preservation in `sessionStorage`. | L |
| G6 | Routing: route tree, `<ProtectedRoute mode='admin'\|'member'\|'auth' roles?>` component, `<NotFoundPage>` (church-themed), 404 fallback for unmatched routes. | M |
| G7 | SignalR: `useNotificationHub` hook with auto-reconnect, on/off subscription API, graceful failure logging. | M |

### Stage H — SPA Application Shells

| # | Step | Effort |
|---|---|---|
| H1 | Public site shell: top navigation (logo + name from Site Settings, hamburger on mobile), homepage placeholder, footer with social links rendered only when configured, placeholder `/privacy` and `/terms` pages. | M |
| H2 | Admin shell: top bar (logo, subtitle, user dropdown), collapsible sidebar with role-filtered nav (Dashboard, Users, Audit Log, Site Settings), dashboard placeholder. | M |
| H3 | `/admin/users`: list (ResponsiveTable), filters, create-user dialog (with invitation), edit dialog, deactivate/reactivate, hard-delete confirmation, force-logout, send-password-reset. | L |
| H4 | `/admin/audit-log`: paginated list with filters (date range, user, action, entity type), detail drawer. | M |
| H5 | `/admin/settings`: tabbed page; Branding tab fully functional (church name, tagline, logo URL string, primary/accent colors with picker, contact, social links, footer text); other tabs render explanatory placeholders. | L |
| H6 | Profile page (any authenticated user): view + edit own name + change password. | M |
| H7 | Version history shell: `<VersionHistoryPanel>` component scaffolded with prop contract documented but no entity wiring (none exist yet). | S |

### Stage I — SPA Tests

| # | Step | Effort |
|---|---|---|
| I1 | Vitest + RTL setup, `jsdom` env, test utility for routing + auth context. | S |
| I2 | `useBreakpoint` test (matchMedia mock, SSR safety). | S |
| I3 | `<ResponsiveTable>` test (renders table on desktop, cards on mobile). | M |
| I4 | `<ProtectedRoute>` test: anonymous on admin route → 404, anonymous on member route → redirect to `/login`. | M |

### Stage J — Deployment Infrastructure

| # | Step | Effort |
|---|---|---|
| J1 | `/deploy/main.bicep`: App Service Plan (Linux), App Service, Azure SQL Server + DB, Storage Account + `images` container, Azure SignalR Service, Application Insights, all settings wired as App Service application settings. | L |
| J2 | `/deploy/parameters.example.json` with placeholder values + comments. | S |
| J3 | `/deploy/README.md`: prerequisites, RG creation, deploy command, custom domain + TLS, sizing, cost estimate, Static Web Apps alternative. | M |
| J4 | `/.github/workflows/deploy.yml`: build API + SPA, copy SPA into `wwwroot/`, publish, deploy via federated creds, gated migration step. | M |

### Stage K — Final Integration & Verification

| # | Step | Effort |
|---|---|---|
| K1 | End-to-end smoke: `dotnet ef database update` against LocalDB → app starts → admin can log in → SPA loads under church theme → admin can navigate to `/admin` (system theme) → covert 404 verified for anonymous on `/admin/users`. | M |
| K2 | Mobile-width verification at 375px for every Phase 1 page; document the standing rule in `IMPLEMENTATION_NOTES.md`. | M |
| K3 | Final build with treat-warnings-as-errors active: `dotnet build`, `dotnet test`, `npm run build`, `npm run test` all green. | S |
| K4 | Update `IMPLEMENTATION_NOTES.md` with final state, deviations, and any open `// TODO:` items surfaced during the build. | S |

**Verifiable state at end of Phase 1:**
- `dotnet build` clean, `dotnet test` green, `npm run build` clean, `npm run test` green.
- `dotnet ef database update` produces a usable schema; running the API on an empty DB
  seeds roles, default admin, system user, and Site Settings row.
- SPA serves from `/wwwroot` and routes correctly between church-themed public pages and
  system-themed admin pages.
- All Phase 1 documentation files are present at the repo root.

---

## 3. Dependencies & Critical-Path Notes

- **A → B → C/D → E → F:** API stack must layer bottom-up. Domain before Application
  before Infrastructure before API.
- **G is independent of E** for development purposes (SPA can mock the API), but **H
  depends on E** because admin shells call real endpoints.
- **D8 (initial migration)** must precede any deploy attempts (J/K).
- **K1 (smoke test)** can only happen after both stacks are wired and the SPA build is
  copied into `wwwroot/`. This is the single most important Phase 1 verification.

---

## 4. Risks I'm Watching

| Risk | Mitigation |
|---|---|
| .NET 10 SDK not installed in environment. | Pause and ask; do not silently downgrade. |
| Temporal-table support in EF Core 10 differs from older versions. | Verify with a tiny spike before committing the convention helper. |
| Azure SignalR Free tier hub-method limits. | Document in `deploy/README.md`; in-process fallback covers dev. |
| shadcn component API drift. | Pin shadcn version at init, capture in `IMPLEMENTATION_NOTES.md`. |
| Covert-404 middleware ordering bugs (must run after auth/authz). | Add a dedicated integration test in F4 that asserts 404 (not 403) for a forbidden admin endpoint with a valid Member-role cookie. |

---

## 5. What I Will NOT Do in Phase 1

(Restating the prompt's out-of-scope list for self-discipline.)

- No content types beyond `SiteSettings` + `ApplicationUser`.
- No Site Settings tabs other than Branding wired up.
- No real SendGrid integration.
- No real Blob upload (Logo is a URL-string field).
- No version history *consumers* (the shell exists; nothing uses it).
- No output caching.
- No Astro docs site content.
- No recurrence/calendar/sermon/etc.

If during implementation I find myself drawn into any of the above, I will stop and ask.

---

## 6. Awaiting Review

This plan is the only deliverable until you approve it. Once approved (with or without
adjustments), I will execute the stages in order, updating `IMPLEMENTATION_NOTES.md` as I
go and surfacing ambiguities via `// TODO:` comments with reasoning rather than guessing
silently.

---

# Phase 2 Build Plan

**Status:** Awaiting review. No Phase 2 implementation work has started.

This plan breaks Phase 2 into ordered stages that build on (do not rebuild) the Phase 1
foundation. It mirrors the Phase 1 plan's shape: clarifications first, then ordered
stages with effort buckets, then risks, then out-of-scope.

---

## P-0. Confirmation of Understanding

I have read all 19 sections of the Phase 2 prompt plus the Out-of-Scope and Final
Checklist sections. My understanding of the Phase 2 shape:

- **14 major deliverables.** Five new versioned content entities (Page, NewsItem,
  ServiceTime, Document, AnnouncementBanner), one non-versioned entity (Leader), the
  homepage composition endpoint, the search infrastructure, SEO surface, output cache
  with tag invalidation, version-history UI activation, real image upload via
  Azure Blob with ImageSharp/WebP, public site shell refinements, and a small SignalR
  surface (`JoinAdminGroup` + `ContentChanged`).
- **No rebuild of Phase 1.** Identity, theming, admin shell, audit log, versioning
  infrastructure, ProtectedRoute, ResponsiveTable, useBreakpoint, NotificationHub,
  CI/CD all stay. Phase 2 extends them.
- **Phase 2 is the first time `IVersionedEntity` is actually used.** The
  interceptor and trim background service from Phase 1 become live; the temporal
  tables themselves are introduced per-entity in Phase 2 migrations.
- **Public-site shell expands.** The Phase 1 placeholder homepage is replaced with the
  composed homepage; the public top nav fills out; the footer wires Privacy/Terms
  pages; the church-themed 404 page picks up the search bar.
- **Out-of-scope.** Sermons, Events, Members directory, Email broadcasts, Astro docs,
  GA4 — all stay deferred per the prompt and `ROADMAP.md`.

---

## P-1. Phase 1 Inheritance — Confirmed Intact

Before starting Phase 2, the following Phase 1 deliverables continue to work and will
not be rebuilt:

- Four-project ASP.NET Core 10 solution + reference graph + `Directory.Build.props`
  with treat-warnings-as-errors.
- ASP.NET Core Identity, three roles, cookie auth, rate limiting, lockout.
- Auth endpoints (login/logout/me/forgot/reset/accept-invitation/change-password).
- `IVersionedEntity` interface, `VersioningInterceptor` (scoped after the Phase 1 K
  smoke-test fix), `ICurrentUserService`, System User seeded, nightly trim service
  registered (currently inert).
- Audit log infrastructure + `/admin/audit-log` UI.
- Site Settings (Branding tab fully functional; other tabs as documented placeholders).
- Theming (church + system), `ChurchThemeLayout`, `SystemThemeLayout`.
- React Router with three-mode `<ProtectedRoute>` (admin / member / auth) implementing
  the covert-404 pattern.
- Admin shell with collapsible sidebar (Sheet on mobile/tablet).
- Public site shell with placeholder homepage + footer (to be rebuilt in P10/P14).
- `useBreakpoint`, `<ResponsiveTable>`, `useNotificationHub`.
- `<VersionHistoryPanel>` *shell* (props contract fixed in Phase 1; renderers wired
  in P13).
- `NotificationHub` registered + Azure SignalR fallback.
- Bicep template, GitHub Actions workflow, `.env.template`, seeders.

**Verification.** I'll re-run `dotnet build`, `dotnet test`, `npm run build`, and
`npm test` at the start of Stage P0 to confirm no regressions before adding code.

---

## P-2. Clarifications I'd Like to Surface Before Starting

These are points where I have a default plan and want to flag the decision so you can
redirect before I commit code. Defaults below are what I will do absent override.

1. **Stage P0 begins with the queued `spa/ → app/` rename.** Doing it before any new
   SPA file lands avoids massive merge conflicts when Phase 2 adds dozens of components.
   `git mv spa app`, then sweep `spa/` → `app/` in tracked files (preserving the
   acronym "SPA" in prose), update `.github/workflows/deploy.yml`, `package.json`
   `name`, and any path references. Reuses the spec already in
   `PHASE_2_BACKLOG.md`. **Default:** yes — Stage P0.

2. **Generic blob storage abstraction.** Phase 2 needs both image upload (with
   compression + WebP, used by Pages/Leaders/SiteSettings logo) and PDF upload (no
   compression, used by Documents). **Default:** introduce `IBlobStorageService`
   (Application interface) — generic, content-type-aware, returns
   `BlobUploadResult { Url, ContentType, SizeBytes, Checksum }`. Layer
   `IImageStorageService` on top for the compression+WebP pipeline.
   `IDocumentStorageService` is a thin wrapper for PDF upload + streaming.
   All Azure Blob plumbing lives in Infrastructure.

3. **ProseMirror plain-text extraction in C#.** Needed for excerpts, search-index
   body, meta descriptions, and OG description. **Default:** hand-write a small
   `ProseMirrorTextExtractor` in `CredoCms.Application/Common`. Recursively walks the
   `JsonDocument`, collects `text` fields, joins with single spaces, normalizes
   whitespace. ~50 lines. No `JsonNode` mutation; read-only. Unit-test the walker
   against a few representative documents.

4. **PDF magic-byte sniffing.** **Default:** validate first 5 bytes equal `%PDF-`
   (`0x25 0x50 0x44 0x46 0x2D`). Plus content-type check (`application/pdf`). Reject
   anything else with a 415 Unsupported Media Type and a clear error message.

5. **SQL Server Full-Text Search availability.** SQL Server LocalDB does NOT include
   FTS by default. Azure SQL Database supports FTS on most tiers (S0+ in the Phase 1
   Bicep). **Default:** configure FTS via raw SQL in a dedicated migration
   (`AddSearchIndexFullText`). Implement a runtime probe: if `sys.fulltext_catalogs`
   is empty (FTS not available), the search service falls back to `LIKE`-based search
   with a warning in startup logs. App stays working in either case. Document.

6. **Direct service calls vs. domain events.** Per the prompt section 7, **default:**
   direct calls. Content services explicitly call `ISearchIndexer` and
   `IOutputCacheInvalidator` after `SaveChangesAsync`. Domain events deferred to
   future phases when multi-subscriber scenarios surface.

7. **Slug uniqueness with soft-delete.** **Default:** filtered unique index
   (`WHERE IsDeleted = 0`) on `Slug` for both `Page` and `NewsItem`. Allows recreating
   a slug after a soft-delete; the deleted row keeps its old slug for history.

8. **Editor restrictions on system pages.** **Default:** server-side guard in
   `PageService.UpdateAsync` / `DeleteAsync` rejects edits to `IsSystemPage = true`
   pages by callers without the Administrator role (returns 403, mapped per the
   covert-404 middleware as appropriate). UI hides edit/delete affordances on these
   rows when the current user is Editor-only.

9. **TipTap image extension upload integration.** **Default:** configure TipTap's
   `Image` extension with a custom uploader that POSTs to `/api/admin/images/upload`,
   awaits the `webpBlobUrl`, inserts an `<img src=...>` node into the document. The
   alt-text input lives outside TipTap (per-image alt text is set when the image is
   chosen and stored on the node's `alt` attribute).

10. **Seed PDFs and seed images.** **Default:** include 2–3 small placeholder PDFs
    (~10 KB each) and a few placeholder images under
    `api/CredoCms.Infrastructure/SeedAssets/`. The seeder uploads them to blob storage
    on first run only when the corresponding entity table is empty (consistent with
    Phase 1's idempotent seed rule). Keeps the repo small and avoids depending on
    third-party placeholder URL services.

11. **Dev-time blob storage.** **Default:** Azurite (the Microsoft-supplied Azure
    Storage emulator). The connection string `UseDevelopmentStorage=true` is already
    in the appsettings template. Document running Azurite in `README.md`. Production
    uses the Azure Storage Account from the Phase 1 Bicep.

12. **Cache-by-auth-tier.** Some public endpoints return different responses to
    anonymous vs. authenticated-Member viewers (e.g. `/api/public/news` includes
    members-only items for Members+). **Default:** custom output-cache policy
    `MemberAuthVaryPolicy` that adds an auth-tier discriminator (`anon` vs `member`)
    to the cache key. Other endpoints use the default vary-by-host+path+query.

13. **Generic version-history controller pattern.** **Default:** `IVersionedEntityHandler`
    interface (non-generic; methods take `Guid id` + return `JsonElement` snapshot).
    Each handler registered in DI keyed by entity-type string. The controller at
    `/api/admin/{entityType}/{id}/history*` looks up the handler, invokes the
    appropriate method. New versioned entity = register one handler; controller code
    doesn't change.

14. **Tests dependent on SQL features.** Temporal tables and FTS aren't supported
    by EF Core's in-memory provider. **Default:** unit tests use mocks/in-memory;
    integration tests for those features are gated on a `LocalDb` env var, skipped
    with a clear message when absent. Mirrors the Phase 1 approach for the temporal
    interceptor test.

If any of these defaults are wrong, please redirect during review and I'll adjust
before starting Stage P0.

---

## P-3. Ordered Implementation Stages

Each stage ends with a verifiable state. Effort buckets unchanged from Phase 1
(**S** ≈ <1h, **M** ≈ 1–3h, **L** ≈ 3–6h, **XL** ≈ 6h+). Estimates are informational.

### Stage P0 — Repo Housekeeping & Phase 1 Verification

| # | Step | Effort |
|---|---|---|
| P0.1 | Run `dotnet build` / `dotnet test` / `npm run build` / `npm test`. Confirm Phase 1 still green (no regressions before adding any code). | S |
| P0.2 | Execute the queued `spa/ → app/` rename per `PHASE_2_BACKLOG.md`. `git mv`, sweep references in CI/docs/configs, rename `package.json`. Re-run all builds and tests. Single commit. | M |
| P0.3 | Add Phase 2 NuGet packages to the appropriate projects: `SixLabors.ImageSharp`, `SixLabors.ImageSharp.Web`, `Azure.Storage.Blobs`, `Microsoft.AspNetCore.OutputCaching`. | S |
| P0.4 | Add Phase 2 npm packages to `app/`: `@tiptap/react`, `@tiptap/starter-kit`, `@tiptap/extension-link`, `@tiptap/extension-image`, `@tiptap/extension-table`, `@tiptap/extension-placeholder`, `prosemirror-changeset`, `diff-match-patch`, `@types/diff-match-patch`. | S |

**Verifiable state:** `npm test` and `dotnet test` still green; folder layout is now `app/`; all CI references use `app/`.

### Stage P1 — Site Settings Extensions

| # | Step | Effort |
|---|---|---|
| P1.1 | Add fields to `SiteSettings` entity: `LeadersPageLabel`, `LeaderCategories` (JSON), `DocumentCategories` (JSON), `MaxDocumentSizeBytes`, `MembersWelcomeText` (ProseMirror JSON), `HomepageHeroCtaLabel`, `HomepageHeroCtaLink`, `ImageMaxWidth`, `ImageQuality`, `DefaultMetaDescription`. | S |
| P1.2 | Migration `AddPhase2SiteSettingsFields`. Update Phase 1 seed to set sensible defaults on first run. | S |
| P1.3 | Extend `SiteSettingsDto`/`UpdateSiteSettingsRequest`/validators in Application. | S |
| P1.4 | Wire the **Content** tab in `/admin/settings` (Phase 1 placeholder) — Leaders page label, Leader categories editor, Document categories editor, members welcome text (TipTap). | M |
| P1.5 | Wire the **Advanced** tab — image quality, image max width, default meta description, max document size, "Rebuild Search Index" button (the action stub; service wired in P9). | M |

**Verifiable state:** Site Settings has all Phase 2 fields; admin can edit them; they round-trip; existing Branding tab unchanged.

### Stage P2 — Image Upload Pipeline

| # | Step | Effort |
|---|---|---|
| P2.1 | `IBlobStorageService` interface (Application). `IImageStorageService` interface (Application) with `UploadAsync(Stream, contentType, filename) -> ImageUploadResult`. | S |
| P2.2 | `BlobStorageService` Infrastructure impl using `Azure.Storage.Blobs` + Azurite for dev. | M |
| P2.3 | `ImageStorageService` Infrastructure impl: validates content-type + magic bytes + max size, loads via ImageSharp, resizes if wider than `ImageMaxWidth`, writes optimized version + WebP, uploads both, returns URLs. | L |
| P2.4 | `POST /api/admin/images/upload` endpoint (Editor+). Multipart form, returns `{ blobUrl, webpBlobUrl, width, height, sizeBytes }`. | S |
| P2.5 | `<ImageUpload>` SPA component: drag-drop or click, preview, alt-text input, loading state, error display, accepts `onUploaded(url, webpUrl, width, height)` callback. | M |
| P2.6 | Wire `<ImageUpload>` into the Site Settings Branding tab logo field (replacing the URL-string stub). | S |
| P2.7 | `IBlobCleanupService` interface stub (Application) + logging-only impl (Infrastructure). Documented as deferred. | S |
| P2.8 | Tests: image validation rejection paths, magic-byte sniffer, ImageSharp resize math, mock blob upload. | M |

**Verifiable state:** an Editor can upload a logo from the Branding tab; the resulting URL points at Azurite locally / Azure Blob in production; WebP variant is generated.

### Stage P3 — Page Entity (Pages)

| # | Step | Effort |
|---|---|---|
| P3.1 | `Page` Domain entity implementing `IVersionedEntity`. `IsSystemPage` flag. | S |
| P3.2 | EF configuration: temporal table, filtered unique index on `Slug`, soft-delete query filter. Migration `AddPagesTable`. | M |
| P3.3 | `IPageRepository`, `IPageService` (CRUD + publish/unpublish/soft-delete + restore + hard-delete). System-page guards. | M |
| P3.4 | `PagesController` — admin CRUD at `/api/admin/pages`, public at `/api/public/pages/{slug}` and `/api/public/pages` listing. | M |
| P3.5 | SPA admin: `/admin/pages` (list, search, deleted-tab), `/admin/pages/new`, `/admin/pages/:id` (edit). TipTap editor with full toolbar. Slug auto-generation + warning on change. Hero image via `<ImageUpload>`. Excerpt auto-generated. | XL |
| P3.6 | SPA public: `/{slug}` route renders the page (centered max-width 720px, hero `<picture>` with WebP source, title H1, body via TipTap read-only mode). Members-only flag enforced. Meta description, OG, JSON-LD `Article`. | L |
| P3.7 | Tests: PageService publish flow, slug uniqueness with soft-delete, system-page guard, public route 404 for unpublished/members-only/anonymous. | M |

**Verifiable state:** Editor can create, edit, publish, soft-delete a page; public visitor sees published pages at `/{slug}`; members-only pages 404 for anonymous.

### Stage P4 — News Entity (NewsItem)

| # | Step | Effort |
|---|---|---|
| P4.1 | `NewsItem` entity with `IsMembersOnly = true` default, `ExpiresAt`, `CalendarDate`, `MetaDescription`. | S |
| P4.2 | Temporal table migration. Soft-delete filter. Slug filtered unique index. | S |
| P4.3 | `INewsService` and controller (admin CRUD + public list/detail). Members-only filter applied per auth tier. Expiration filter on listings. | M |
| P4.4 | SPA admin: `/admin/news` mirroring Pages but shorter. | M |
| P4.5 | SPA public: `/news` paginated reverse-chronological list, `/news/:slug` detail. JSON-LD `Article`. | M |
| P4.6 | Tests: members-only filtering, expiration cutoff. | S |

**Verifiable state:** News entries flow through draft → publish → public listing; members-only items are gated; expired items drop off automatically.

### Stage P5 — ServiceTime Entity

| # | Step | Effort |
|---|---|---|
| P5.1 | `ServiceTime` entity with `DayOfWeek` enum, `StartTime`/`EndTime` (`TimeOnly`), `Location`, `DisplayOrder`, `IsActive`, soft-delete. | S |
| P5.2 | Temporal table migration. | S |
| P5.3 | Service + controller. | S |
| P5.4 | SPA admin: list sortable by `DisplayOrder` (numeric input — drag-and-drop deferred per prompt). | M |
| P5.5 | SPA public: `/service-times` page grouped by `DayOfWeek`, sorted by `StartTime`. | M |
| P5.6 | Tests: ordering and active filter. | S |

### Stage P6 — Leaders (No Versioning)

| # | Step | Effort |
|---|---|---|
| P6.1 | `Leader` entity (no `IVersionedEntity`, no soft-delete, hard-delete only). Photo + WebP URL fields. | S |
| P6.2 | Migration (regular table, not temporal). | S |
| P6.3 | Service + controller. Admin-only delete (Editors create/edit, Administrators delete). | S |
| P6.4 | SPA admin: list grouped by Category from Site Settings, photo upload, TipTap bio, hard-delete confirmation modal. | M |
| P6.5 | SPA public: `/leaders` (configurable label) grouped by Category card layout, `/leaders/:id` detail with `Person` JSON-LD. | M |

### Stage P7 — Documents (PDFs)

| # | Step | Effort |
|---|---|---|
| P7.1 | `Document` entity (versioned metadata; blob is replaced not versioned). | S |
| P7.2 | Temporal-table migration. | S |
| P7.3 | `IDocumentStorageService` (PDF magic-byte validation, upload, stream-with-auth). | M |
| P7.4 | Service + controller. Streaming endpoint `GET /api/public/documents/{id}/file` enforces members-only via cookie auth. | M |
| P7.5 | SPA admin: `/admin/documents` list filterable by category, PDF upload via `<DocumentUpload>` component (sibling of `<ImageUpload>`). | M |
| P7.6 | SPA public: `/documents` list grouped by category, `/documents/:id` PDF preview via `<embed>` of streaming endpoint, mobile-fallback to download link. | M |
| P7.7 | Tests: PDF magic-byte sniffer, members-only stream auth, file-size cap. | M |

### Stage P8 — Announcement Banner

| # | Step | Effort |
|---|---|---|
| P8.1 | `AnnouncementBanner` (singleton, `IVersionedEntity`, fixed Guid). | S |
| P8.2 | Temporal-table migration + Phase 2 seed of one inactive row. | S |
| P8.3 | Service + admin endpoint at `/api/admin/announcement` + public endpoint `/api/public/banner`. | S |
| P8.4 | SPA admin: `/admin/announcement` single-page editor (Editor+). Severity radio, message, optional link, optional schedule. | M |
| P8.5 | SPA public: `<AnnouncementBar>` component rendered above the public nav, dismissible per-session via `sessionStorage`. | S |

### Stage P9 — Search Infrastructure

| # | Step | Effort |
|---|---|---|
| P9.1 | `SearchIndexEntry` entity. | S |
| P9.2 | Migration `AddSearchIndex` — table + unique `(EntityType, EntityId)` + non-clustered indexes. Separate migration `AddSearchIndexFullText` with raw SQL `CREATE FULLTEXT CATALOG` + `CREATE FULLTEXT INDEX`. Idempotent guard for non-FTS environments. | M |
| P9.3 | `ISearchIndexer` (Application) + `SearchIndexer` (Infrastructure) with FTS-or-LIKE-fallback runtime probe. | M |
| P9.4 | Wire `ISearchIndexer.UpsertAsync` into Pages/News/Leaders/Documents content services on create/update; `RemoveAsync` on hard-delete; `IsPublished` flip on soft-delete. | M |
| P9.5 | `GET /api/public/search?q={q}&page={n}` endpoint with auth-tier filter. Output-cached 60s, vary by query+auth-tier. | S |
| P9.6 | `<SearchOverlay>` SPA component: search icon in public nav, opens overlay with debounced query, results dropdown, "View all results" link to `/search?q=...` page. | L |
| P9.7 | One-time index rebuild `BackgroundService` runs on startup if `SearchIndex` is empty. | S |
| P9.8 | Admin "Rebuild Search Index" action wired to existing button stub from P1.5. | S |

**Verifiable state:** Search works across Pages/News/Leaders/Documents; auth tier respected; index rebuild action works.

### Stage P10 — Homepage Composition

| # | Step | Effort |
|---|---|---|
| P10.1 | `GET /api/public/homepage` endpoint composing Site Settings + active service times + latest 1–2 news (auth-aware) + members welcome (Members+ only) + active banner. Output-cached 5min, vary by auth tier. | M |
| P10.2 | Replace Phase 1 placeholder homepage with the composed shell: hero with CTA, service times block, "Latest Sermon coming soon" placeholder block, "Upcoming Events coming soon" placeholder block, latest news block, members-only welcome block (conditional render), footer. | L |
| P10.3 | Tests: homepage endpoint composition; auth-tier conditional payload. | S |

### Stage P11 — SEO Infrastructure

| # | Step | Effort |
|---|---|---|
| P11.1 | `GET /sitemap.xml` endpoint enumerating published, public-visible Pages/News/Leaders. Cached 1h, tag `sitemap`. | M |
| P11.2 | `/robots.txt` static file in `wwwroot/` (or generated endpoint) — disallows admin/docs/api/profile/members/documents/search; points to `sitemap.xml`. | S |
| P11.3 | OG + Twitter Card meta tags emitted from each public route via a small `<SeoTags>` component pulling from a per-route props contract. | M |
| P11.4 | JSON-LD: site-wide `Organization`/`Church` from Site Settings; per-route `Article` (Pages, News), `Person` (Leader detail), `Event`/`Schedule` (ServiceTimes). | M |
| P11.5 | `MetaDescription` field on Pages and News with fallback chain Excerpt → SiteSettings.DefaultMetaDescription. | S |

### Stage P12 — Output Caching with Tag-Based Invalidation

| # | Step | Effort |
|---|---|---|
| P12.1 | Register `AddOutputCache` in `Program.cs`. In-memory store. Default policy: do not cache; opt-in per endpoint. Block any caching on `/api/admin/*` and `/api/auth/*`. | S |
| P12.2 | `MemberAuthVaryPolicy` adding an auth-tier discriminator. | S |
| P12.3 | Apply `[OutputCache]` attributes to all Phase 2 public endpoints with the tags + durations from prompt section 11. | M |
| P12.4 | `IOutputCacheInvalidator` Application interface + Infrastructure impl wrapping `IOutputCacheStore`. Tag-eviction map per the prompt. | M |
| P12.5 | Wire `IOutputCacheInvalidator` calls into all content services and Site Settings service on writes. | M |
| P12.6 | Tests: invalidator called with correct tags on write; cache populated on read; cache evicted after invalidation. | M |

### Stage P13 — Version History UI Activation

| # | Step | Effort |
|---|---|---|
| P13.1 | `IVersionedEntityHandler` interface + DI registry keyed by entity type string. | S |
| P13.2 | Generic admin controller at `/api/admin/{entityType}/{id}/history*`. Look up handler, dispatch list/get/restore. 404 covert for unknown types or missing entities. | M |
| P13.3 | Per-entity handlers (Page, News, ServiceTime, Document, AnnouncementBanner). Use `TemporalAll`/`TemporalAsOf` against the DbContext. Restore copies scalars per `VERSIONING.md`. | L |
| P13.4 | `<ProseMirrorDiffRenderer>` SPA component using `prosemirror-changeset`. | L |
| P13.5 | `<TextDiffRenderer>` SPA component using `diff-match-patch`. | M |
| P13.6 | `<ImageDiffRenderer>` SPA component (side-by-side desktop, stacked mobile via `useBreakpoint`). | S |
| P13.7 | Wire `<VersionHistoryPanel>` into edit pages for Pages/News/ServiceTimes/Documents/AnnouncementBanner with the appropriate field arrays. | M |
| P13.8 | Restore confirmation modal with the prompt-specified copy. | S |
| P13.9 | Tests: `<ProseMirrorDiffRenderer>` against sample documents; generic version controller's covert-404 behavior. | M |

### Stage P14 — SignalR Phase 2 Surface

| # | Step | Effort |
|---|---|---|
| P14.1 | `JoinAdminGroup()` hub method (auth-only, role-checked Editor+). | S |
| P14.2 | `IRealtimeNotifier` Application interface + Infrastructure impl that broadcasts `ContentChanged { entityType, entityId, action }` to the `admins` group. | S |
| P14.3 | Wire content services to call `IRealtimeNotifier` after successful writes. | S |
| P14.4 | SPA admin list views: subscribe to `ContentChanged` for the matching entity type, show a non-intrusive toast "New changes — refresh to see" (manual refresh, not auto). | M |

### Stage P15 — Seed Data

| # | Step | Effort |
|---|---|---|
| P15.1 | Add seed assets under `api/CredoCms.Infrastructure/SeedAssets/` (placeholder PDFs, logos, hero images). | S |
| P15.2 | Phase 2 seeder upgrades: System Pages (Privacy/Terms with boilerplate body), sample Pages (About/Beliefs/Plan Your Visit), 2–3 sample News items mixing public + members-only, 2–3 ServiceTimes, 4–6 Leaders across categories, 2–3 Documents, an inactive AnnouncementBanner row, members welcome text. Seeds idempotently per existing pattern. | L |

### Stage P16 — Documentation Updates

| # | Step | Effort |
|---|---|---|
| P16.1 | `IMPLEMENTATION_NOTES.md`: ServiceTime DayOfWeek decision, direct-call-vs-events decision, generic version-history pattern, image upload pipeline, output-cache tag map, FTS fallback path, slug uniqueness pattern, system-page guards. | M |
| P16.2 | `VERSIONING.md`: replace generic examples with concrete ones now that real versioned entities exist; document the `IVersionedEntityHandler` registry pattern. | S |
| P16.3 | `README.md`: Phase 2 features, operator instructions for search index rebuild, image quality settings, Azurite for local dev. | S |

### Stage P17 — Final Integration & Verification

| # | Step | Effort |
|---|---|---|
| P17.1 | End-to-end smoke: migrations apply cleanly; seeders populate; admin login works; can create a page through TipTap; image uploads and previews; search returns indexed results; homepage renders composed content; sitemap.xml emits expected URLs; output cache hits and invalidates. | L |
| P17.2 | Mobile-width verification at 375px for every Phase 2 page. | M |
| P17.3 | Final clean build with treat-warnings-as-errors: `dotnet build`, `dotnet test`, `npm run build`, `npm test` all green. | S |
| P17.4 | Final `IMPLEMENTATION_NOTES.md` update with Phase 2 verification and any deviations. | S |

---

## P-4. Dependencies & Critical-Path Notes

- **P0 → P1 → P2** is the foundation chain. Image upload (P2) depends on Site
  Settings (`ImageMaxWidth`, `ImageQuality` from P1).
- **P2 must precede P3, P6** (Pages, Leaders both use the image upload component).
- **P3 → P4 → P5 → P6 → P7 → P8** content-types in dependency order (Pages/News
  share TipTap; ServiceTime is simpler; Leaders no versioning; Documents add PDF
  upload; Banner is a singleton — order is "easy to learn from harder").
- **P9 (search)** must come *after* P3–P8 so there's content to index.
- **P10 (homepage)** must come *after* P5 (service times), P4 (news), P8 (banner).
- **P11 (SEO)** must come *after* P3, P4, P6 so sitemap has URLs to enumerate.
- **P12 (output cache)** must come *after* P3–P11 so all the public endpoints exist
  to attribute. Wiring invalidators into existing content services is mechanical.
- **P13 (version-history activation)** can run in parallel with P10–P12 since it
  only needs P3–P8 to have shipped.
- **P14 (SignalR Phase 2)** can run last; it touches each content service to add a
  one-line `IRealtimeNotifier.NotifyContentChanged(...)` call.
- **P15 (seed)** runs after entities exist; P17 verifies everything end-to-end.

---

## P-5. Risks I'm Watching

| Risk | Mitigation |
|---|---|
| FTS not available on a given SQL deployment (LocalDB without FTS, lower-tier Azure SQL). | Runtime probe + `LIKE` fallback. App keeps working; logs a warning at startup. |
| Multiple temporal-table migrations in sequence. EF generates them; risk is migration ordering or destructive column changes mid-Phase. | Per `VERSIONING.md`, use add-then-deprecate for any column changes within a single phase. Phase 2 only adds; no drops planned. |
| TipTap + React 18 + Vite 7 + Vitest 3 compatibility surface. | Pin versions in P0.4; smoke-test the editor early in P3. If breakage, document and pick the closest compatible matrix. |
| `<ProseMirrorDiffRenderer>` complexity. `prosemirror-changeset` requires careful schema alignment with the editor schema. | Build and test against sample docs in P13.4 before wiring to live content. |
| Seed assets bloat the repo. | Keep placeholders <50 KB total across all seed binaries. Use lightweight placeholder PDFs. |
| Output cache incorrectly serving stale member content to anonymous after auth-tier flip. | `MemberAuthVaryPolicy` covers this; explicit unit tests for both directions. |
| Image proxy / streaming PDF endpoint with cookie auth in `<embed>` / `<iframe>` quirks across browsers. | Same-origin via `wwwroot` deployment shape avoids cross-origin auth issues; document mobile fallback to download link. |

---

## P-6. What I Will NOT Do in Phase 2

(Restating the prompt's out-of-scope list.)

- No Sermons, Sermon Series, Scripture, YouTube auto-sync (Phase 3).
- No Events, recurrence, registration, calendar, iCal feeds (Phase 3).
- No Blog, Members directory, Groups, Classes, Prayer Requests, Connect Card (Phase 4).
- No SendGrid, scheduled publishing, SMS stub, Volunteer Signups (Phase 5).
- No Astro docs site, GA4, cookie banner, RSS feeds, Pagefind (Phase 6).
- No public newsletter signup (intentionally not in v1).
- No Redis-backed output cache (deferred to v1.x per `ROADMAP.md`).
- No drag-and-drop reorder for Service Times (deferred per prompt).
- No deep-restore of relationships in version history (per `VERSIONING.md`).
- No `IBlobCleanupService` real implementation (interface stub only — orphan-blob
  cleanup wired in a later phase).

If during implementation I find myself drawn into any of the above, I will stop and ask.

---

## P-7. Awaiting Review

This Phase 2 plan is the only deliverable until you approve it. Once approved (with
or without adjustments), I will execute the stages in order, updating
`IMPLEMENTATION_NOTES.md` as I go and surfacing genuine ambiguities via `// TODO:`
comments rather than guessing silently. Phase 1 deliverables stay intact throughout.

---

# Phase 3 Build Plan

This plan covers the Phase 3 prompt: Sermons (with YouTube auto-sync), the
calendar/events system with recurrence, event registration, iCal feeds, and
News calendar-pinning activation. Phases 1 and 2 are complete on
`claude/credo-cms-phase-1-06gIY` (latest tip: `cf0e36d`), and Phase 3 builds
on top of that work without rebuilding any of it.

---

## Q-0. Confirmation of Understanding

I have read the entire Phase 3 prompt. I confirm:

- **Phase 1 deliverables intact:** Domain/Application/Infrastructure/Api four-
  project layered solution; ASP.NET Core Identity with three roles; theming
  bundles; admin shell with covert-404 routing; versioning infrastructure
  (`IVersionedEntity`, `VersioningInterceptor`, `ICurrentUserService`, System
  user, nightly trim job); audit log; Site Settings (Branding tab); SignalR
  `NotificationHub`; deployment infrastructure (Bicep + GH Actions); seed
  data. **All confirmed at the current branch tip.**

- **Phase 2 deliverables intact:** Pages, News, Service Times, Leaders,
  Documents, public homepage, Site Settings Content + Advanced tabs, Phase 2
  search via `SearchIndex` table with FTS-or-LIKE fallback, announcement
  banner, Privacy Policy and Terms of Service as system pages, sitemap.xml
  + robots.txt + site-wide JSON-LD, output caching foundation with tag
  invalidator + `MemberAuthVaryPolicy`, version-history server-side handler
  registry, image upload pipeline with WebP generation, public site shell
  with nav and footer. **All confirmed.**

- **Phase 2 carry-overs** (documented in `IMPLEMENTATION_NOTES.md`'s P17 log):
  output-cache-invalidator wiring on the 5 non-Page content services, the
  News/ServiceTime/Document/Banner version handlers, per-service
  `IRealtimeNotifier.NotifyContentChangedAsync` calls + the SPA admin toast,
  the three diff renderers, and `[OutputCache]` attributes on the remaining
  public endpoints. **Phase 3 will pick up the wiring carry-overs for the
  new services it ships and apply the same pattern to those — the Phase 2
  carry-overs themselves remain queued and will be batched into Stage Q15
  so cache + realtime + version-history coverage is uniform across all
  content types when Phase 3 closes.** The diff renderers stay in the
  carry-over list (independently L-effort and unrelated to Phase 3
  deliverables).

- **Process:** This BUILD_PLAN update is the only deliverable until review.
  No code changes until the plan is approved.

---

## Q-1. Phase 2 Inheritance — Things Phase 3 Touches

Phase 3 reuses (does not rebuild):

- `<ImageUpload>` → sermon-series banner, event hero image.
- `<TipTapFullEditor>` → sermon description, sermon-series description,
  event description, registration confirmation message.
- `<TipTapReadOnly>` → sermon detail page description, event detail page
  description.
- `<SeoTags>` → all new public pages.
- `IBlobStorageService` + `IImageStorageService` → YouTube thumbnail copy
  to blob storage with WebP generation.
- `IDocumentStorageService` (read-only path) → linked sermon attachments
  (PDFs only, surfaced via existing public streaming endpoint).
- `ISearchIndexer` → sermons (with transcript), sermon series, events
  added as new `EntityType` values; existing FTS-or-LIKE fallback handles
  the larger transcript bodies without code change.
- `IOutputCacheInvalidator` + `OutputCacheTags` → new tag constants for
  sermons-list / event-{id} / calendar / ical-public / etc.; same
  invalidator pattern.
- `IRealtimeNotifier` → broadcasts `ContentChanged` for sermon and event
  writes, plus a new `SermonSyncCompleted` notification.
- `IVersionedEntityHandler` registry → new handlers for SermonSeries,
  Sermon, Event register against the existing controller without
  changes.
- Audit log writer → "Sermon.AutoImported", "Event.OccurrenceCanceled",
  "EventRegistration.Submitted" / "Canceled" / "Promoted".
- `ApplicationDbContext.AsTemporal(name)` helper → new versioned tables.
- The public `:slug` Pages route is **not** affected (sermons live under
  `/sermons`, events under `/events`).

Phase 1's nightly versioning trim service automatically picks up Phase 3
versioned entities since the trim is by interface, not entity type.

---

## Q-2. Clarifications I'd Like to Surface Before Starting

Numbered so reviewers can answer or confirm by number.

1. **Tag entity introduction.** The prompt notes: "Phase 2 may not have
   introduced a Tags entity yet; if not, Phase 3 introduces it now."
   **Confirmed: Phase 2 did not introduce a Tag entity.** Phase 3 Stage
   Q1 adds the unified `Tag` table + `SermonTag` join + autocomplete
   component.

2. **Polymorphic ScriptureReference table.** Prompt explicitly says
   "Use the polymorphic single-table approach." I'll do exactly that,
   with a `(ParentEntityType, ParentEntityId)` index for parent lookups
   and a `(Book, ChapterStart)` index for "Browse by Book" queries.
   The tradeoff (no FK enforcement at the DB; integrity maintained at
   the service layer) goes in `IMPLEMENTATION_NOTES.md`. **Defaulting
   to prompt's choice unless redirected.**

3. **Event visibility no-default.** Prompt is explicit: "no default.
   Editor must explicitly choose Public or MembersOnly before
   publishing." I'll model this as a nullable `Visibility` on the
   entity (so the row can exist as a draft without one) plus a
   `Required(Visibility)` FluentValidation rule on the publish path.
   **Confirming interpretation.**

4. **Cloudflare Turnstile.** The prompt says "Cloudflare Turnstile
   (already configured for Connect Card pattern in earlier seeds;
   reuse)." **Phase 2 did NOT introduce Turnstile** (Connect Card is
   a Phase 4 feature). Two options:

   - **(a)** Add Turnstile infrastructure now (siteKey/secretKey on
     Site Settings → Privacy & Security tab + reusable
     `<TurnstileWidget>` + server-side verification) — pulls Phase
     4-adjacent work forward by about half a stage.
   - **(b)** Defer Turnstile to Phase 4 alongside Connect Card and
     rely on **honeypot field + 5-second time-to-submit + per-IP
     rate limit** for Phase 3's public registration form. The same
     three defenses are also called out in the Phase 3 prompt and
     provide reasonable cover for an event-registration use case.

   **Default: (b)**, deferred to Phase 4. I'll document the decision
   in `IMPLEMENTATION_NOTES.md` with the explicit gap noted.
   Override if you want Turnstile in Phase 3.

5. **YouTube API key and OAuth refresh-token storage.** Prompt:
   "encrypted at rest if practical — at minimum, stored in Site
   Settings as a secret-treated field." **Default: stored as plain
   text in the DB, masked in the admin UI (last 4 chars visible),
   never returned to the client in full unless the user explicitly
   clicks 'Reveal'.** Proper ASP.NET Core Data Protection
   encryption-at-rest is a clean follow-up that doesn't change the
   schema; I'll note it in `PHASE_3_BACKLOG.md` (creating that file
   as the analog of Phase 2's backlog).

6. **YouTube `timedtext` for transcripts.** That endpoint is
   unofficial and has been intermittent historically. Prompt
   acknowledges this; I'll implement it as a best-effort fetch
   (failure → `Transcript=null`, `TranscriptSource=None`) and
   document the caveat. If the endpoint stops working entirely, the
   manual paste path in the sermon edit form is the supported
   fallback.

7. **YouTube quota.** Default daily quota is 10,000 units. A sync
   run for a single channel uses ~100 units (search.list ≈ 100,
   videos.list ≈ 1 per video). Default 6-hour interval is well
   under the limit. I'll document this in `IMPLEMENTATION_NOTES.md`
   and note that the operator needs to apply for a quota increase
   if they sync many channels in the future.

8. **Drag-to-reorder vs numeric `DisplayOrder` for registration
   fields.** Prompt: "your call; recommend drag if Phase 2 didn't
   already do drag elsewhere." Phase 2 used numeric `DisplayOrder`
   everywhere (ServiceTimes, Leaders, EventRegistrationField will
   follow suit). **Default: numeric DisplayOrder for consistency
   with Phase 2.** Drag is a polish-pass candidate that doesn't
   affect the schema.

9. **`CalendarFeedToken` hashing.** Standard pattern: token is a
   256-bit cryptographically random value, base64url-encoded.
   Server stores `SHA-256(token)` and the raw token is shown to
   the user once on regenerate. SHA-256 is fine here (the token
   itself is high-entropy; the hash exists only to prevent DB-side
   disclosure). Argon2id would be overkill. **Confirming.**

10. **YouTube URL parsing.** Manual import accepts
    `https://www.youtube.com/watch?v=ID`, `https://youtu.be/ID`,
    `https://www.youtube.com/shorts/ID`, or a bare `ID`. I'll
    write a small parser with tests covering each form.

11. **`SermonAttachment` validation.** Prompt: "PDFs only, must
    have `IsMembersOnly=false`." Server-side validation rule on
    the sermon save path checks each attachment's referenced
    `Document` row. **Confirming.**

12. **Per-occurrence override + exception modeling.** Two separate
    tables as the prompt specifies (`EventRecurrenceException` for
    skips, `EventOccurrenceOverride` for edits). Calendar
    expansion logic in `EventOccurrenceExpander` applies both.
    **Confirming.**

13. **`/api/public/calendar` cache key.** Vary by `start`, `end`,
    and auth-tier. The existing `MemberAuthVaryPolicy` covers
    auth; date range is included via the request URL the cache
    already keys on. **Confirming.**

14. **Bible book slugs.** Lowercase book name with non-alphanumerics
    replaced by dashes ("1 John" → `1-john`, "Song of Solomon" →
    `song-of-solomon`). The `BibleBooks` static reference data
    ships pre-computed slugs.

15. **Phase 2 carry-over batching.** Per Q-0, the Phase 2 carry-
    overs (cache invalidator wiring on
    News/ServiceTime/Leader/Document/Banner, matching version-
    history handlers, per-service realtime notifier calls + SPA
    admin toast, remaining `[OutputCache]` attributes) will be
    cleaned up alongside the same wiring on the new Phase 3
    entities in **Stage Q15**. The diff renderers
    (`<ProseMirrorDiffRenderer>`, `<TextDiffRenderer>`,
    `<ImageDiffRenderer>`) remain in `IMPLEMENTATION_NOTES.md`'s
    carry-over list since those are independently L-effort and
    unrelated to Phase 3 deliverables. **Confirming.**

16. **Sermon series chronology.** "Part X of Y" + "ordered list in
    chronological order" → order by `Sermon.PublishedAt` ascending
    within a series.

17. **Mobile-first verification.** Same standing rule as
    Phase 1/2 (375px width). Verified per page in Stage Q18
    alongside the final integration pass.

---

## Q-3. Ordered Implementation Stages

Letter prefix `Q` for Phase 3 stages (after `A`-`K` Phase 1 and
`P0`-`P17` Phase 2). **Effort:** S = small (≤2h), M = medium (half a
day), L = large (full day), XL = multi-day. These are rough,
informational only.

### Stage Q0 — Phase 2 verification & Phase 3 packages

| # | Step | Effort |
|---|---|---|
| Q0.1 | Verify Phase 2 still green: `dotnet build`, `dotnet test`, `npm run build`, `npm test`. Confirm no regressions before adding code. | S |
| Q0.2 | Add NuGet packages to `CredoCms.Infrastructure`: `Ical.Net` (latest stable), `Google.Apis.YouTube.v3`. | S |
| Q0.3 | Add npm packages to `app/`: `@fullcalendar/react`, `@fullcalendar/core`, `@fullcalendar/daygrid`, `@fullcalendar/timegrid`, `@fullcalendar/list`, `@fullcalendar/rrule`, `rrule`. | S |
| Q0.4 | Create `PHASE_3_BACKLOG.md` placeholder file (analog of Phase 2 backlog) with section headings ready for any items that surface during implementation. | S |

**Verifiable state:** Phase 2 still green; new package references resolve; clean build.

### Stage Q1 — Tag infrastructure

| # | Step | Effort |
|---|---|---|
| Q1.1 | Domain `Tag` entity (`Id`, `Name`, `Slug`, `CreatedAt`, `UsageCount`). EF config: case-insensitive unique on `Name`, unique on `Slug`. | S |
| Q1.2 | `Application.Tags`: `ITagRepository`, `ITagService` with `NormalizeAndUpsertAsync(string name)`, `IncrementUsageAsync` / `DecrementUsageAsync`. Slug derivation: lower-case, dashes for non-alphanumerics, trimmed. | M |
| Q1.3 | Migration `AddTagsTable`. | S |
| Q1.4 | `<TagAutocomplete>` SPA component (chip input, debounced suggest endpoint, "Create new tag" affordance). API: `GET /api/admin/tags/search?q=`. | M |
| Q1.5 | Tests: tag normalization (case + whitespace), slug uniqueness, usage-count increment/decrement. | S |

### Stage Q2 — Scripture references (data + entity + UI)

| # | Step | Effort |
|---|---|---|
| Q2.1 | `Domain.Bible.BibleBooks` static reference data class: 66 books with `Name`, `Abbreviation`, `Testament` enum (Old/New), `ChapterCount`, `Slug`. Unit-tested counts. | S |
| Q2.2 | Polymorphic `ScriptureReference` entity (per Q-2 #2). EF config: `(ParentEntityType, ParentEntityId)` index, `(Book, ChapterStart)` index. **Not versioned.** | S |
| Q2.3 | Migration `AddScriptureReferencesTable`. | S |
| Q2.4 | Application service for managing references attached to a parent (replace-all-on-save semantics — simpler than diff). | S |
| Q2.5 | SPA `<ScriptureReferenceInput>` component: book select grouped by testament, chapter/verse number inputs, "Through" toggle, range validation against book metadata. Emits a value object. | M |
| Q2.6 | SPA `formatScriptureReference(ref)` utility producing canonical strings ("Matthew 5:1–7:29", "Romans 8", "1 John 2:15–17", "Psalm 23"). En-dashes for ranges. Unit-tested. | S |
| Q2.7 | Tests: book metadata sanity, polymorphic FK behavior, formatter, range validation. | S |

### Stage Q3 — SermonSeries

| # | Step | Effort |
|---|---|---|
| Q3.1 | Domain `SermonSeries` (versioned). | S |
| Q3.2 | EF config (temporal table), filtered unique slug, query filter for soft-delete. Migration `AddSermonSeriesTable`. | S |
| Q3.3 | `Application.Sermons.Series` repository + service + DTOs + validators (slug regex, optional banner image, optional Scripture reference). | M |
| Q3.4 | Admin controller `/api/admin/sermon-series` (full lifecycle); public controller `/api/public/sermons/series` and `/api/public/sermons/series/{slug}`. | M |
| Q3.5 | SPA admin: `/admin/sermon-series` list + editor with banner upload, TipTap description, optional `<ScriptureReferenceInput>`, "Sermons in this series" read-only tab (placeholder until Sermon ships in Q4). | L |
| Q3.6 | SPA public: `/sermons/series` list, `/sermons/series/{slug}` detail. | M |
| Q3.7 | Search index entry on create/update; remove on hard-delete; published-flag flip on soft-delete. | S |
| Q3.8 | Tests: slug uniqueness with soft-delete, validator. | S |

### Stage Q4 — Sermon entity scaffold

| # | Step | Effort |
|---|---|---|
| Q4.1 | Domain `Sermon` entity (versioned). All fields per prompt §4. | S |
| Q4.2 | Many-to-many tables: `SermonTag`, `SermonAttachment` (with the attachment-validation rule: PDF-only, public-only). | S |
| Q4.3 | EF temporal config, filtered unique slug, filtered unique YouTube video ID, query filter for soft-delete. Migration `AddSermonsTable`. | S |
| Q4.4 | `ISermonRepository`, `ISermonService` with full lifecycle. Speaker validation (LeaderId or free-text or neither — both null allowed). | M |
| Q4.5 | Validators (slug, title, YouTube video ID format, attachment PDFs+public, scripture references). | S |
| Q4.6 | Admin controller `/api/admin/sermons` (CRUD, soft-delete, restore, hard-delete). | M |
| Q4.7 | Tests: create flow, attachment validation, scripture reference replace-all. | M |

### Stage Q5 — YouTube integration

| # | Step | Effort |
|---|---|---|
| Q5.1 | `IYouTubeApiClient` (Application) abstraction: `Task<YouTubeVideo?> GetByIdAsync(string videoId)`, `Task<List<YouTubeVideo>> SearchChannelAsync(string channelId, DateTimeOffset? since)`. | S |
| Q5.2 | `YouTubeApiClient` (Infrastructure) implementation using `Google.Apis.YouTube.v3`. API key + OAuth refresh token from Site Settings. | M |
| Q5.3 | `IYouTubeTranscriptClient` + impl using a typed `HttpClient` named client targeting `https://www.youtube.com/api/timedtext?...`. Best-effort: failure → null. Parse the SRT/XML payload. | M |
| Q5.4 | `YouTubeUrlParser` static helper (recognizes `watch?v=`, `youtu.be/`, `shorts/`, bare ID). Tests for each form. | S |
| Q5.5 | `YouTubeSyncService : BackgroundService` per prompt §5. Reads settings from `SiteSettings`, polls every `SyncIntervalMinutes`, dedupes on `YouTubeVideoId`, copies thumbnail via `IImageStorageService`, attempts transcript, creates draft sermon with System user as writer, applies tags + default tags. | L |
| Q5.6 | Manual single-video import endpoint `POST /api/admin/sermons/import` (Editor+) accepting URL or video ID. Same code path as auto-sync. | S |
| Q5.7 | Manual sync trigger endpoint `POST /api/admin/sermons/sync` (Administrator only). Returns immediately; sync runs in the background. Audit-log "Sermon.SyncTriggeredManually". | S |
| Q5.8 | Site Settings extensions for the YouTube fields (extends Phase 2's `SiteSettings`; new migration `ExtendSiteSettingsForYouTube`). Last-sync status fields update on each run. | M |
| Q5.9 | Tests with `IYouTubeApiClient` mocked: dedupe, default-tags applied, thumbnail copied, transcript best-effort. | M |

### Stage Q6 — Sermon admin UI

| # | Step | Effort |
|---|---|---|
| Q6.1 | `/admin/sermons` list with filter sidebar (status, series, tag, speaker, date range), search, "Pending Review" tab highlighting auto-imports. | L |
| Q6.2 | `/admin/sermons/new` form (manual create from URL/ID). | M |
| Q6.3 | `/admin/sermons/{id}` editor: title, slug, TipTap description, speaker (Leader dropdown / free-text toggle), series dropdown with "Create new series" affordance, `<TagAutocomplete>`, repeatable `<ScriptureReferenceInput>` rows, attachments multi-select (filtered to public PDFs), YouTube video ID read-only with link, thumbnail preview, members-only toggle, published date picker, draft/publish toggle. | XL |
| Q6.4 | Sermon-side wiring: cache invalidator + search indexer + realtime notifier on writes (the same pattern as Phase 2). | S |

### Stage Q7 — Sermon public surface

| # | Step | Effort |
|---|---|---|
| Q7.1 | Public controllers: `/api/public/sermons` (paginated archive with filters), `/api/public/sermons/{slug}` detail, `/api/public/sermons/by-book` index, `/api/public/sermons/by-book/{bookSlug}` and `/{bookSlug}/{chapter}`. | M |
| Q7.2 | `/sermons` archive page: hero + search + filter sidebar (collapsible on mobile), card grid, Load More pagination, "Browse by:" links. | L |
| Q7.3 | `/sermons/{slug}` detail: embedded YouTube iframe, title, speaker, series link with Part X/Y, formatted scripture refs (each linked to by-book), tags, TipTap description, attachments, collapsible transcript with `?highlight=` highlighting, prev/next within series, JSON-LD `Article` + `VideoObject`. | L |
| Q7.4 | `/sermons/by-book` index: two-column OT/NT grid with sermon counts. | M |
| Q7.5 | `/sermons/by-book/{bookSlug}` (and `/{chapter}`): chapter selector, sermons grouped by chapter. | M |
| Q7.6 | `/sermons?tag=...` filter (query string handled by the archive page). | S |
| Q7.7 | Mobile (375px): card grid → single column, filter sidebar collapses, video iframe responsive. | S |

### Stage Q8 — Event entity + recurrence

| # | Step | Effort |
|---|---|---|
| Q8.1 | Domain `Event` (versioned), `EventRecurrenceException` (not versioned), `EventOccurrenceOverride` (not versioned). | S |
| Q8.2 | `Visibility` enum (`Public`, `MembersOnly`) — nullable on the entity to model "not yet chosen", required-on-publish via FluentValidation. | S |
| Q8.3 | EF temporal config for Event; standard tables for the two override/exception tables. Migration `AddEventsTables`. | M |
| Q8.4 | `EventOccurrenceExpander` service: given an event + date range, expand RRULE via `Ical.Net`, apply exceptions, apply per-occurrence overrides. Returns concrete occurrences. | L |
| Q8.5 | `IEventRepository`, `IEventService` with full lifecycle including per-occurrence skip/edit actions. | L |
| Q8.6 | Validators (slug, time order, visibility-required-on-publish, recurrence-end-condition exclusivity, registration-window exclusivity). | M |
| Q8.7 | Admin controller `/api/admin/events` (CRUD + restore + hard-delete + skip-occurrence + edit-occurrence). | M |
| Q8.8 | Tests: recurrence expansion (daily/weekly/monthly), exception applied, override applied, RRULE generation from the four UI patterns. | L |

### Stage Q9 — Event admin UI

| # | Step | Effort |
|---|---|---|
| Q9.1 | `/admin/events` list with filters (visibility, registration, has-recurrence, date range), search. | M |
| Q9.2 | Event editor with: hero upload, TipTap description, start/end pickers, all-day toggle, location, **visibility radio (no default)**, recurrence builder (4 simple patterns + end condition), registration section (collapsed when None). | XL |
| Q9.3 | Recurrence-exceptions/overrides UI: shows next 90 days of upcoming occurrences with [Edit] and [Cancel] actions per row. | L |
| Q9.4 | Event-side wiring: cache invalidator + search indexer + realtime notifier on writes. | S |

### Stage Q10 — Event registration server-side

| # | Step | Effort |
|---|---|---|
| Q10.1 | `EventRegistrationField` entity + EF config + service (CRUD per event). | S |
| Q10.2 | `EventRegistration` entity + EF config (no versioning per prompt). Status enum (Confirmed / Waitlisted / Canceled). | S |
| Q10.3 | Migration `AddEventRegistrationTables`. | S |
| Q10.4 | `IEventRegistrationService` with submit / cancel / promote-from-waitlist on cancel / resend-confirmation / list / detail / CSV export. Capacity + waitlist logic. | L |
| Q10.5 | Public controller `POST /api/public/events/{slug}/register`: standard fields + dynamic field validation per `EventRegistrationField`. Honeypot, time-to-submit ≥5s, per-IP rate limit (5/10min). Anonymous + member paths. For recurring events, the request includes `OccurrenceDate`. | L |
| Q10.6 | Cancellation: `GET /api/public/events/{slug}/register/cancel?token=...` validates token; `POST` performs cancellation. Token = HMAC-SHA256 of registration ID + timestamp signed with a server secret; expiry baked into the token. | M |
| Q10.7 | Admin controllers: `GET /api/admin/events/{id}/registrations`, `GET .../{regId}`, `POST .../{regId}/cancel`, `POST .../{regId}/resend-confirmation`, `GET .../export.csv`. | M |
| Q10.8 | Confirmation email composition (reuse Phase 1's logging email service; SendGrid wires in Phase 5). Email contains the cancel-link with the signed token. | S |
| Q10.9 | Tests: capacity, waitlist promotion on cancel, dynamic field validation, token signing/verification. | M |

### Stage Q11 — Event registration UI

| # | Step | Effort |
|---|---|---|
| Q11.1 | Public registration page `/events/{slug}/register` (a real page, not a modal — accessibility per prompt). Renders standard + dynamic fields. Auto-populates standard fields for logged-in members (editable). For recurring events: occurrence date picker with capacity-remaining indicator. | L |
| Q11.2 | Confirmation page (post-submit) renders the event's `RegistrationConfirmationMessage` (TipTap read-only). | S |
| Q11.3 | Cancellation page `/events/{slug}/register/cancel?token=...`. | S |
| Q11.4 | Admin "Registration Fields" editor (modal: pick type, label, required, options, help, length/range constraints) + "Registrations" tab on the event editor with list, detail drawer, mark-canceled, resend-confirmation, CSV export. | XL |

### Stage Q12 — Public events listing + detail

| # | Step | Effort |
|---|---|---|
| Q12.1 | `/events` listing: upcoming first, hero image cards, recurrence next-up. | M |
| Q12.2 | `/events/{slug}` detail: title, hero, full date/time, location, description, recurrence preview ("Repeats [pattern]" + next 5–10 occurrences), Register button (if applicable), "Add to calendar" dropdown (download .ics, Google Calendar link, Outlook link), JSON-LD `Event`. | L |
| Q12.3 | Single-event ICS download `GET /api/public/events/{slug}/ics`. | S |

### Stage Q13 — Calendar (FullCalendar)

| # | Step | Effort |
|---|---|---|
| Q13.1 | API: `GET /api/public/calendar?start=&end=` returns events (recurrence expanded for the range, with overrides + exceptions applied) plus News items pinned via Phase 2's `CalendarDate`. Auth-tier filtered. Output cached 1 min, varied by date range and auth state. | M |
| Q13.2 | `/calendar` public page using `@fullcalendar/react`: month default on desktop, list default on mobile (via `useBreakpoint`). View toggle. News markers visually distinct from events. Click → navigate to detail. | L |
| Q13.3 | `/admin/events/calendar` admin overview: same component, also shows drafts. (Drag-to-reschedule deferred to v1.x per prompt.) | M |
| Q13.4 | Mobile (375px): list view default, marker legibility verified. | S |

### Stage Q14 — iCal feeds

| # | Step | Effort |
|---|---|---|
| Q14.1 | `Application.Calendar.IIcalFeedBuilder` + Infrastructure impl using `Ical.Net`. Public events with full RRULE, EXDATEs for exceptions, RECURRENCE-ID separate VEVENTs for overrides. Stable UIDs from `Event.Id`. | M |
| Q14.2 | `GET /calendar/feed.ics` — public events only, no News markers, cached 5 min. | S |
| Q14.3 | `CalendarFeedToken` entity (Id, UserId FK, TokenHash, CreatedAt, LastUsedAt). Migration. | S |
| Q14.4 | Token service: `Generate`, `Validate(rawToken)`, `Revoke(userId)`. Hash via SHA-256; raw token shown to user once on regenerate. | M |
| Q14.5 | `GET /calendar/feed/{token}.ics` — public + members-only events for the token's user. Not cached. Updates `LastUsedAt`. | M |
| Q14.6 | `/profile/calendar-feed` page: feed URL display, subscription instructions for Google/Apple/Outlook, [Regenerate Token] with confirm modal. | M |
| Q14.7 | Tests: ICS generation (RRULE round-trip, EXDATE, RECURRENCE-ID), token signing/hashing/validation, regenerate invalidates the previous one. | M |

### Stage Q15 — Cross-cutting wiring + Phase 2 carry-over cleanup

This stage is a single sweep across all content services so that cache,
search, and realtime are consistent across **every** entity in the
system at the end of Phase 3.

| # | Step | Effort |
|---|---|---|
| Q15.1 | `OutputCacheTags` constants for every Phase 3 entity. `[OutputCache]` attributes per the prompt §14 schedule on every public Phase 3 endpoint. Backfill the same on Phase 2 endpoints that don't have them yet. | M |
| Q15.2 | Cache invalidator wiring on every content service (Phase 2 carry-over: News/ServiceTime/Leader/Document/Banner/SiteSettings; Phase 3: Sermon/SermonSeries/Event/Registration). News write also invalidates `calendar` if `CalendarDate` is set. | M |
| Q15.3 | `IRealtimeNotifier.NotifyContentChangedAsync` wiring on every content service (Phase 2 carry-over + Phase 3 entities). | S |
| Q15.4 | New SignalR notification: `SermonSyncCompleted` broadcast at end of `YouTubeSyncService` run with `{ status, importedCount }`. Admin sermons page subscribes and toasts. | S |
| Q15.5 | SPA admin "new content — refresh to see" toast component (the pending Phase 2 carry-over) wired to the admin shell, listens to `ContentChanged`. | M |
| Q15.6 | `IVersionedEntityHandler` implementations for News, ServiceTime, Document, Banner (Phase 2 carry-over) plus Sermon, SermonSeries, Event (Phase 3). All register against the existing generic version-history controller. | L |
| Q15.7 | Search index entries for SermonSeries, Sermon (with transcript), Event added; entity-type icon dispatch (Lucide `Video`, `BookOpen`, `Calendar`) on the search results page. | S |
| Q15.8 | Site Settings — Integrations + Content tabs wired (UI on the existing tab layout — back-end fields land in Q5 / Q8 / earlier; Q15 is the UI hookup pass). | M |

### Stage Q16 — Profile + members area additions

| # | Step | Effort |
|---|---|---|
| Q16.1 | `/profile/calendar-feed` page (overlapped with Q14.6; this stage just slots it into the profile shell). | S |
| Q16.2 | `/profile/registrations` page: list of upcoming events the member registered for, with [Cancel] action calling the existing public cancellation endpoint with a member-context shortcut (no token needed when the user is logged in and matches `EventRegistration.UserId`). | M |

### Stage Q17 — Seed data + sample content

| # | Step | Effort |
|---|---|---|
| Q17.1 | Phase 3 seeder additions: 1–2 sample SermonSeries; 4–6 sample Sermons (with public-domain CC YouTube IDs documented in IMPLEMENTATION_NOTES; mix of speakers, tags, scripture refs across Matthew + Romans + Psalms); auto-created tags ("Easter", "Christmas", "Romans", "Matthew", "Hope", "Faith", "Community"); 3–5 Events (one weekly small group, one monthly meeting, two single-occurrence; one with registration enabled and 2 sample fields). | L |
| Q17.2 | Idempotent: skip seeding entirely if any sermon or event already exists. | S |

### Stage Q18 — Documentation, mobile audit, final verification

| # | Step | Effort |
|---|---|---|
| Q18.1 | `IMPLEMENTATION_NOTES.md`: polymorphic ScriptureReference decision, YouTube quota, transcript-via-timedtext caveat, event-visibility no-default pattern, iCal token model, override-vs-exception modeling, the public-domain video IDs used for seeds, the YouTube secrets storage decision (plain in DB / masked in UI / Data-Protection encrypt-at-rest deferred). | M |
| Q18.2 | `README.md`: YouTube setup walkthrough, calendar feed URLs, recurrence builder note. | S |
| Q18.3 | `ROADMAP.md`: confirm bulk recurrence exceptions, "move to different date" override, drag-to-reschedule on admin calendar are all listed. | S |
| Q18.4 | `PHASE_3_BACKLOG.md`: any items that surfaced during implementation (e.g., Turnstile carry-over if option (b) was chosen, Data-Protection encrypt-at-rest for YouTube secrets). | S |
| Q18.5 | 375px mobile pass on every new page. | M |
| Q18.6 | Final smoke test: `dotnet build` clean / `dotnet test` green / `npm run build` clean / `npm test` green / API boots / public surface returns expected payloads. | M |
| Q18.7 | `IMPLEMENTATION_NOTES.md` closing log entry: end-of-Phase-3 state, test counts, bundle sizes, any remaining carry-overs. | S |

**Verifiable state:** Phase 3 done. Application builds and runs; sermons sync from a configured YouTube channel; manual sermon import works; events with recurrence render on the calendar; registrations work end-to-end with email confirmation + token cancellation; iCal feeds (public + member token) parse correctly in Apple Calendar / Google Calendar; News with `CalendarDate` shows on the calendar; mobile (375px) verified.

---

## Q-4. Dependencies & Critical-Path Notes

- **Q1 → Q4** Tag → Sermon (sermons reference tags).
- **Q2 → Q3, Q4** Scripture references → SermonSeries + Sermon.
- **Q3 → Q4** SermonSeries before Sermon (sermons FK to series; "Create
  new series" affordance in the sermon editor implies series exists or
  can be created from there — series-only routes work without
  sermons).
- **Q4 → Q5, Q6, Q7** Sermon entity before YouTube import; sermon
  admin UI; sermon public surface.
- **Q5 → Q6, Q15** YouTube sync independently testable; the manual
  import path in Q5.6 + the `SermonSyncCompleted` notification in
  Q15.4 close the loop in Q15.
- **Q8 → Q9, Q10, Q11, Q12, Q13, Q14** Event entity is the foundation
  for everything event-related; recurrence expander (Q8.4) is reused
  by the calendar (Q13), iCal feed (Q14), and the recurring-events
  registration occurrence picker (Q11).
- **Q10 → Q11** Server-side registration before UI.
- **Q13 + Q14** Calendar page and iCal feeds both depend on the
  `EventOccurrenceExpander` from Q8.
- **Q15** runs after the Phase 3 entities exist so it can wire them
  all uniformly; it also picks up the Phase 2 carry-overs in the same
  pass.
- **Q17** seeds run after entities exist; **Q18** verifies end-to-end.

The critical path is **Q0 → Q1 → Q2 → Q3 → Q4 → Q5 → Q6 → Q7** (sermon
slice top to bottom) and parallel **Q8 → Q9 → Q10 → Q11 → Q12 → Q13 →
Q14** (event/calendar slice). Q15 + Q17 + Q18 fold both back together.

---

## Q-5. Risks I'm Watching

| Risk | Mitigation |
|---|---|
| YouTube `timedtext` returns inconsistent payloads (XML vs SRT vs JSON; some videos return 404 even when captions exist on YouTube). | Best-effort, never fatal; failure → `Transcript=null`, `TranscriptSource=None`; manual paste path in the edit form. Documented as a known fragility. |
| YouTube Data API quota exhaustion. | Default 6-hour interval is well under the 10K-unit/day quota for a single channel. Expose `LastSyncStatus` so operators see "quota exceeded" clearly. |
| Polymorphic ScriptureReference can't have a real DB FK to its parent. | Service-layer integrity (parent existence check on insert; cascade-on-delete via the parent service's hard-delete path). Documented. |
| RRULE expansion correctness across DST boundaries. | Use `Ical.Net` for both server-side (`EventOccurrenceExpander`) and pass the same RRULE string to FullCalendar's `rrule` plugin so client and server agree. Test with events that span DST changes. |
| iCal feed format drift between Apple Calendar / Google Calendar / Outlook. | `Ical.Net` produces RFC-5545-compliant output; smoke-test against at least Apple + Google during Q18 verification. |
| Public registration endpoint abuse (spam, bots). | Honeypot + 5s time-to-submit + per-IP rate limit (5 / 10min). Turnstile deferred to Phase 4 per Q-2 #4. Documented gap. |
| Phase 2 carry-over wiring across many services in Q15 risks regressions. | Per-service tests already exist for the write paths; adding cache/realtime/search calls is mechanical and will be done one service at a time with a build between each. |
| Long sermon transcript bodies enlarge `SearchIndex.BodyText`. | FTS handles this well; `nvarchar(max)` is already the column type. The `BodyText.Length` cap of 8000 chars is a soft truncate inside `SearchIndexer`; transcripts may exceed that. **Decision:** lift the truncate to 32000 specifically for sermon transcripts so the headline feature works. Document. |

---

## Q-6. What I Will NOT Do in Phase 3

(Restating the prompt's out-of-scope list and what's deferred.)

- No Blog (long-form posts) — Phase 4.
- No Member directory, Groups, Classes — Phase 4.
- No Prayer Requests, Connect Card — Phase 4.
- No SendGrid, scheduled publishing, email broadcasts, email-on-publish — Phase 5.
- No Volunteer Signups — Phase 5.
- No Astro docs site — Phase 6.
- No bulk recurrence exceptions — v1.x.
- No "move to different date" recurrence override — v1.x.
- No drag-to-reschedule on admin calendar — v1.x.
- No Cloudflare Turnstile in Phase 3 (deferred to Phase 4 per Q-2 #4).
- No Data-Protection encrypt-at-rest for YouTube secrets in Phase 3
  (queued in `PHASE_3_BACKLOG.md` per Q-2 #5).
- No drag-to-reorder for `EventRegistrationField` (numeric
  `DisplayOrder` per Q-2 #8).
- No diff renderers (`<ProseMirrorDiffRenderer>`, text-diff,
  image-diff). These remain in `IMPLEMENTATION_NOTES.md`'s Phase 2
  carry-over list and will be addressed independently when version-
  history UX becomes the focus.

If during implementation I find myself drawn into any of the above, I
will stop and ask.

---

## Q-7. Awaiting Review

This Phase 3 plan is the only deliverable until you approve it. Once
approved (with or without adjustments), I'll execute Q0 through Q18 in
order, updating `IMPLEMENTATION_NOTES.md` as I go and surfacing genuine
ambiguities via `// TODO:` comments rather than guessing silently.
Phase 1 and Phase 2 deliverables stay intact throughout; the Phase 2
carry-overs documented in `IMPLEMENTATION_NOTES.md` get cleaned up
alongside the Phase 3 entities in **Stage Q15**.
