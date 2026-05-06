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

---

# Phase 4 Build Plan — Members and Community

**Status:** Plan drafted; awaiting review. No Phase 4 implementation has started.

---

## Q-0. Confirmation of Understanding

I have read all 16 sections of the Phase 4 prompt plus the Out-of-Scope and Final
Checklist sections. My understanding of Phase 4's scope and constraints:

- **Privacy/access-control rigor is the highest-priority constraint.** Every
  member-data endpoint must carry explicit `WHERE UserId = currentUserId` guards.
  Integration tests must prove cross-user access is rejected, not just that happy-path
  works. This requirement supersedes any "move fast" impulse.
- **Six domain slabs:** Member Profile (extensions + directory), Groups, Classes,
  Prayer Requests, Connect Card, and Blog. Facebook linking is a separate auth concern.
- **ApplicationUser already inherits `PhoneNumber` from `IdentityUser<Guid>`.** Phase 4
  must not add a duplicate column; the address / photo / bio / directory / notification
  fields are all net-new additions.
- **SearchIndexEntry already has `IsMembersOnly` (added in Phase 3 or earlier).** New
  entity types slot into the existing indexing infrastructure.
- **Tag entity (Phase 3) is shared.** `BlogPost` creates a `BlogPostTag` join table
  against the existing `Tags` table; no Tag schema changes required.
- **`IsMembersOnly` on search results:** already in place; Phase 4 populates it for
  Groups (MembersOnly visibility), PrayerRequests, and members-only BlogPosts.
- **Phase 1-3 deliverables stay intact.** Phase 4 layers on top; no rebuilding.

---

## Q-1. Clarifications to Surface Before Starting

These are genuine decision points where I have a default but want explicit approval
before committing code. Where I list a default, that is exactly what I'll do unless
overridden.

1. **`ProfanityFilter` NuGet package.** The prompt specifies "ProfanityFilter NuGet
   package (configurable wordlist with allowlist support)" without naming a specific
   package. The most-used .NET option is `ProfanityFilter` by Ben Harris (Stephen
   Haunts on older attribution); it ships a hardcoded US-English wordlist and exposes
   `ContainsProfanity(string)` but no out-of-box custom wordlist or allowlist. **Default:**
   install that package, wrap it in a custom `IProfanityCheckService` that merges the
   built-in wordlist with the admin-configured `ProfanityWordlist` from SiteSettings
   and suppresses any matches appearing in the admin-configured `ProfanityAllowlist`
   before returning a result. The "Test phrase" tool in Site Settings calls this service
   directly. If you prefer a different package (e.g., `ProfanityFilter.AspNetCore`),
   say so.

2. **`ApplicationUser.PhoneNumber` is inherited from `IdentityUser<Guid>`.** Identity's
   `PhoneNumber` property already maps to the `PhoneNumber` column in `AspNetUsers`.
   Phase 4 does NOT add a second phone column. **Default:** the personal-info profile
   API reads/writes `ApplicationUser.PhoneNumber` via the inherited property.
   `MaxLength(50)` is enforced via FluentValidation on the profile API input model, not
   via `[MaxLength]` on the entity (which would conflict with Identity's own column
   definition). Note this in `IMPLEMENTATION_NOTES.md`.

3. **Rate limiting on Connect Card.** The prompt specifies "5 submissions per IP per
   hour." ASP.NET Core 7+ ships `Microsoft.AspNetCore.RateLimiting` (in-box; no extra
   NuGet). **Default:** configure a sliding-window rate-limit policy keyed on the
   hashed IP (`IpAddressHash`) using the built-in middleware. The raw IP is hashed with
   SHA-256 before use in the limiter key and again before storage in
   `IpAddressHash`. No extra NuGet required.

4. **Mobile profile-tab pattern.** Prompt says "tabs convert to a vertical accordion or
   horizontally-scrollable tab bar — choose pattern and document." **Default:**
   horizontally-scrollable tab bar (same pattern as admin shell), consistent with the
   existing component library. Tab labels truncate gracefully at narrow widths. Document
   in `IMPLEMENTATION_NOTES.md`.

5. **User-targeted SignalR group for group membership notifications.** `GroupMembership
   Approved` and `GroupMembershipDeclined` need to reach the specific requester.
   **Default:** each authenticated client joins a SignalR group named `"user-{userId}"`
   on `OnConnectedAsync`. The group-request approval/decline handler sends to that group.
   `NotificationHub` already has the JoinAdminsGroup/JoinMembersGroup pattern from Phase 1;
   this adds `JoinUserGroup()` (called automatically on connect for authenticated users,
   not a client-callable method). Document decision.

6. **`GroupMembership.Status` when an admin adds a member directly.**  The entity spec
   says "default Pending unless added directly by admin/leader." **Default:** admin/leader
   direct-add sets `Status=Active`, `JoinedAt=now`, `ProcessedByUserId=currentUserId`,
   no join-request message. Pending flow is only triggered by member self-request.

7. **Group Leader permission to promote members to Leader.** The prompt states
   "Mark/unmark as Leader (Administrator only for security; Editors and Group Leaders
   cannot promote)." **Default:** enforce this at the service layer: only users with
   the `Administrator` role can call `SetLeaderAsync`. Editors and Group Leaders get
   `403 Forbidden` if they attempt it. Tests verify this.

8. **Cloudflare Turnstile widget in SPA.** No official Anthropic/Cloudflare React package
   is commonly used. **Default:** load the Turnstile JS (`https://challenges.cloudflare.com/
   turnstile/v0/api.js`) via a `<script>` tag injected into the public-form page, use
   the `window.turnstile.render()` API inside a `useEffect`, and capture the token in a
   hidden field. Server-side validation calls Turnstile's `siteverify` endpoint via
   `HttpClient`. When `CloudflareTurnstileSiteKey` is null/empty in SiteSettings, the
   widget is skipped entirely (dev mode or unconfigured). Tests mock the `HttpClient`.

9. **`ScheduledPublishAt` for Blog Posts.** The prompt captures the field in Phase 4 but
   defers the scheduling implementation to Phase 5. **Default:** the field is stored and
   returned in the API; the admin UI shows a date-time picker for it; but no background
   job or any publish-on-schedule logic runs in Phase 4. The editor sees an informational
   note: "Scheduled publishing activates in a future release." No `// TODO:` needed in
   code — just a ROADMAP entry.

10. **Author archive (`/blog/by/{userId}`) — public or members-only?** The prompt says
    "members and anonymous viewers both see this." **Default:** author archive is fully
    public. It only shows the author's `PublicAuthorBio` and published, non-members-only
    posts. Members-only blog posts are excluded from the public author archive.

11. **`BlogPost.ReadingTimeMinutes` calculation race.** If body is empty on create, ceil
    of 0 words = 0 min, but the formula is `max(1, ceil(wordCount / 250))`. **Default:**
    `max(1, ceil(wordCount / 250))` always; even a blank body gets ReadingTimeMinutes=1.

12. **Turnstile in Connect Card vs Phase 4 scope.** Phase 3 deferred Turnstile to Phase 4
    (documented). **Default:** Connect Card gets full Turnstile integration in Phase 4.
    Event registration (Phase 3) does NOT get Turnstile retroactively added in Phase 4
    (that would be a Phase 3 backlog item; flag in IMPLEMENTATION_NOTES if desired).

If any of these defaults are wrong, redirect before I write code.

---

## Q-2. Ordered Implementation Steps

Effort key: **S** = small (<2h), **M** = medium (2-4h), **L** = large (4-6h), **XL** = extra large (6h+).

### Stage Q0 — Domain Entities

| # | Step | Effort |
|---|---|---|
| Q0.1 | `ApplicationUser` extension: add `AddressLine1`, `AddressLine2`, `City`, `StateOrRegion`, `PostalCode`, `Country`, `PhotoBlobUrl`, `PhotoWebpBlobUrl`, `PhotoAltText`, `PublicAuthorBio`, directory opt-in fields (`IsListedInDirectory`, `ShowEmailInDirectory`, `ShowPhoneInDirectory`, `ShowAddressInDirectory`, `ShowPhotoInDirectory`), and notification preference fields (`ReceiveNewsEmails`, `ReceiveBroadcastEmails`, `ReceiveBlogEmails`, `ReceiveGroupEmailsGlobal`). | S |
| Q0.2 | `Group` + `GroupMembership` entities in `CredoCms.Domain.Groups`. Enums: `GroupVisibility`, `GroupJoinability`, `MessageOnJoinRequest`, `RosterVisibility`, `GroupMembershipStatus`. | M |
| Q0.3 | `ClassSlot` + `ClassOffering` entities in `CredoCms.Domain.Classes`. | S |
| Q0.4 | `PrayerRequest`, `PrayerRequestUpdate`, `PrayerRequestPrayedFor` entities in `CredoCms.Domain.Prayer`. Enums: `PrayerRequestStatus`. | S |
| Q0.5 | `ConnectCardSubmission` entity in `CredoCms.Domain.ConnectCard`. Enum: `ConnectCardStatus`. | S |
| Q0.6 | `BlogPost` entity + `BlogPostTag` join entity in `CredoCms.Domain.Blog`. | S |
| Q0.7 | Repository interfaces in `CredoCms.Application`: `IGroupRepository`, `IGroupMembershipRepository`, `IClassSlotRepository`, `IClassOfferingRepository`, `IPrayerRequestRepository`, `IConnectCardRepository`, `IBlogPostRepository`. Extend `IApplicationUserRepository` with directory-query methods. | M |

### Stage Q1 — EF Core Configuration + Migration

| # | Step | Effort |
|---|---|---|
| Q1.1 | EF configurations for all new entities. Temporal tables on: `Groups`, `ClassSlots`, `ClassOfferings`, `PrayerRequests`, `PrayerRequestUpdates`, `BlogPosts`, `ConnectCardSubmissions`. (GroupMembership, PrayerRequestPrayedFor: no temporal.) | M |
| Q1.2 | Indexes: `Groups.Slug` (unique), `BlogPosts.Slug` (unique), `BlogPosts.AuthorUserId`, `BlogPosts.PublishedAt`, `BlogPosts.Category`, `BlogPosts.IsPublished`, `PrayerRequests.SubmittedByUserId`, `PrayerRequestPrayedFor` unique `(PrayerRequestId, UserId)`, `ConnectCardSubmissions.SubmittedAt`, `GroupMembership.(GroupId, UserId)` unique-active index, `ClassSlots.Slug` (unique), `ClassOfferings.ClassSlotId`. | S |
| Q1.3 | `BlogPostTag` join table configuration (FK to `Tags.Id`, FK to `BlogPosts.Id`, composite PK). | S |
| Q1.4 | EF migration: `Phase4_MembersAndCommunity`. Verify migration generates correctly; apply to dev DB. | M |
| Q1.5 | Extend `SiteSettings` domain record with all Phase 4 keys: `GetInvolvedPageLabel`, `ClassesPageLabel`, `ClassAudienceAgeGroups`, `ShowRecentPastOnPublicClasses`, `RecentPastClassesLookbackDays`, `BlogCategories`, `BlogPageLabel`, `ProfanityWordlist`, `ProfanityAllowlist`, `PrayerRequestArchiveDays`, `PrayerRequestRequireApproval`, `ConnectCardInterests`, `ConnectCardAcknowledgmentMessage`, `ConnectCardPageLabel`, `CloudflareTurnstileSiteKey`, `CloudflareTurnstileSecretKey`, `FacebookOAuthAppId`, `FacebookOAuthAppSecret`, `FacebookLoginEnabled`. Update `SiteSettingsService` defaults. | M |

### Stage Q2 — Profile API + Admin User Management Extension

| # | Step | Effort |
|---|---|---|
| Q2.1 | `ProfileService` in Application layer: `GetProfileAsync`, `UpdatePersonalInfoAsync`, `UpdateDirectoryAsync`, `UpdateNotificationsAsync`. All guard `UserId == currentUserId` at the service layer before touching any data. | M |
| Q2.2 | `ProfileController` (`/api/profile`): `GET /api/profile`, `PUT /api/profile/personal`, `PUT /api/profile/directory`, `PUT /api/profile/notifications`. All `[Authorize]`. FluentValidation input models. | M |
| Q2.3 | Extend admin `UsersController` with profile field editing (admin can update any user's profile fields, not just role/status). Add `AdminNotesDto` aggregate: group memberships, prayer request count, registration history counts. | M |
| Q2.4 | `PUT /api/admin/users/{id}/reset-notifications` — resets notification fields to defaults. Administrator only. | S |
| Q2.5 | Integration tests: verify `PUT /api/profile/personal` with `userId A` cannot modify `userId B`'s data (401 or 403). Verify admin can modify any user's profile. | M |

### Stage Q3 — Profile SPA Page (4 tabs)

| # | Step | Effort |
|---|---|---|
| Q3.1 | `/profile` redesign as a 4-tab shell (horizontally-scrollable on mobile): Personal Info, Directory, Notifications, Account. Shared header with user name + avatar preview. | M |
| Q3.2 | Personal Info tab: name (read-only), email (read-only), phone (editable), address fields (editable), photo upload (reusing `<ImageUpload>` from Phase 2), alt text (required if photo present), Public Author Bio (TipTap, optional). Save button. | L |
| Q3.3 | Directory tab: "Include me in directory" toggle (default off), sub-toggles for Email/Phone/Address/Photo (visible when listed=true), preview card showing what other members see. Save button. | M |
| Q3.4 | Notifications tab: per-category checkboxes. Below global Group toggle: per-group override toggles (list each membership; only visible when global group toggle is on). Save button. | M |
| Q3.5 | Account tab: Connected Accounts (Facebook link/unlink — stubbed until Q15 lands), Calendar Feed section (Phase 3 feed manager inline), Change Password link, My Event Registrations link, My Groups link. | M |
| Q3.6 | 375px mobile audit: tab bar scrolls horizontally; all form fields usable on small screen. | S |

### Stage Q4 — Members Directory

| # | Step | Effort |
|---|---|---|
| Q4.1 | `MembersDirectoryService` in Application layer: `ListAsync(search, page, pageSize, ct)` returns only `IsListedInDirectory && IsActive` users; `GetByIdAsync(userId, ct)` with same gate plus field-level privacy filtering. Throws 404 if user not listed. | M |
| Q4.2 | `MembersController` (`/api/members`): `GET /api/members` and `GET /api/members/{userId}` — both `[Authorize(Roles="Member,Editor,Administrator")]`. Server strips unlisted members and non-opted-in fields before serialization. | M |
| Q4.3 | `/members` SPA list page: `<ResponsiveTable>` (table on desktop, cards on mobile), name search box, pagination, click-through to detail. | M |
| Q4.4 | `/members/{userId}` SPA detail page: photo, name, opted-in fields, group memberships (public/members-only groups only), `mailto:` button. | M |
| Q4.5 | Integration tests proving: (a) unauthenticated requests to `/api/members` get 401; (b) requesting a non-listed member's ID returns 404; (c) a listed member's non-opted-in fields are absent from the response body. | M |

### Stage Q5 — Groups — Backend

| # | Step | Effort |
|---|---|---|
| Q5.1 | `GroupRepository` + `GroupMembershipRepository` in Infrastructure layer. Key queries: `ListPublicAsync(visibility, ct)`, `GetBySlugAsync(slug, ct)`, `ListMembershipsForGroupAsync(groupId, status?, ct)`, `GetMembershipAsync(groupId, userId, ct)`. | M |
| Q5.2 | `GroupService` in Application layer: create/edit/delete (Administrator only), roster management, join request submit (member-callable), approval/decline (Leader/Editor/Admin), leave. All permission checks at the service layer with explicit role/membership-leader lookups. | L |
| Q5.3 | `AdminGroupsController` (`/api/admin/groups`): CRUD, roster CRUD, pending requests list, approve, decline, promote-to-leader (Administrator only route). | L |
| Q5.4 | `PublicGroupsController` (`/api/public/groups`): `GET /api/public/groups` (visibility-filtered), `GET /api/public/groups/{slug}` (visibility-gated, roster conditional on RosterVisibility + membership), `POST /api/public/groups/{slug}/request-join` (auth required). | M |
| Q5.5 | `ProfileGroupsController` (`/api/profile/groups`): `GET` (list own memberships), `POST /leave/{groupId}`. | S |
| Q5.6 | SignalR: emit `GroupJoinRequestSubmitted` to "admins" group + "user-{leaderId}" for each leader of the group; emit `GroupMembershipApproved` / `GroupMembershipDeclined` to "user-{requesterId}". | M |
| Q5.7 | Output caching on public groups list (1 min, tag `groups`). Cache invalidation on group create/edit/delete. | S |
| Q5.8 | Integration tests: join-request flow end-to-end; editor cannot promote to leader; leader can approve but not promote; cross-group membership leak test (member can't see Hidden group roster via API). | L |

### Stage Q6 — Groups — SPA

| # | Step | Effort |
|---|---|---|
| Q6.1 | `/get-involved` public page: visibility-filtered group cards (image, name, excerpt, meeting info, badge), "Request to Join" or "Sign in to join" CTA. Sort alphabetical. | M |
| Q6.2 | `/groups/{slug}` public group detail: image, full description, meeting info, contact email, join request modal (with conditional message field per `RequiresMessageOnJoinRequest`), roster section (conditional on `RosterVisibility` + viewer membership). | L |
| Q6.3 | `/admin/groups` list with filters. `/admin/groups/new` create form. `/admin/groups/{id}` edit form with tabbed sub-sections: Roster, Pending Requests, History. | XL |
| Q6.4 | Roster tab: member list, direct-add search, promote-to-leader (admin-only action guarded in UI), remove with confirmation. | M |
| Q6.5 | Pending Requests tab: pending list with request message, Approve / Decline buttons. | M |
| Q6.6 | `/profile/groups` page: own memberships list with [Leave] button. | S |
| Q6.7 | `useAdminNotifications()` hook extension: subscribe to `GroupJoinRequestSubmitted` SignalR event, toast in admin shell with link to requests page. | S |
| Q6.8 | 375px mobile audit on all group pages. | S |

### Stage Q7 — Classes — Backend

| # | Step | Effort |
|---|---|---|
| Q7.1 | `ClassSlotRepository` + `ClassOfferingRepository` in Infrastructure. Key queries: `ListPublicAsync(ct)` (IsActive slots + current offering per slot), `GetBySlugAsync(slug, ct)`, `ListOfferingsForSlotAsync(slotId, ct)`. | M |
| Q7.2 | `ClassService` in Application layer: slot CRUD (Admin only), offering CRUD (Admin only), public-facing query helpers (current/upcoming/recent-past per slot). Member-only fields stripped from public DTOs at service layer. | M |
| Q7.3 | `AdminClassSlotsController` (`/api/admin/class-slots`): CRUD + soft-delete. | M |
| Q7.4 | `AdminClassOfferingsController` (`/api/admin/class-offerings`): CRUD + soft-delete, filter by slot/date range/status. | M |
| Q7.5 | `PublicClassesController` (`/api/public/classes`): `GET /api/public/classes` (slot list with current/upcoming offering per slot; member-only fields present only when authenticated Member+), `GET /api/public/classes/{slug}`. | M |
| Q7.6 | SiteSettings keys for classes (`ClassAudienceAgeGroups`, `ShowRecentPastOnPublicClasses`, etc.) read in controller to drive public behavior. | S |
| Q7.7 | Integration tests: verify `TeacherLeaderId`, `DetailedSchedule`, `MaterialsNeeded`, `DefaultRoom` are absent from anonymous-caller responses. | M |

### Stage Q8 — Classes — SPA

| # | Step | Effort |
|---|---|---|
| Q8.1 | `/classes` public page: slots grouped by audience age group, per-slot card showing current/upcoming offering. Members see additional fields (teacher, room, schedule). Age-group filter chips at top. | L |
| Q8.2 | `/classes/{slug}` detail page (slot-level view with current + upcoming offering detail). | M |
| Q8.3 | `/admin/class-slots` list + create/edit form (with offerings sub-tab listing all offerings for the slot). | M |
| Q8.4 | `/admin/class-offerings` list + create/edit form (slot picker, date range, teacher leader vs. free-text toggle). | M |
| Q8.5 | 375px mobile audit. | S |

### Stage Q9 — Prayer Requests — Backend

| # | Step | Effort |
|---|---|---|
| Q9.1 | `ProfanityFilter` NuGet install. `IProfanityCheckService` / `ProfanityCheckService` in Infrastructure: merges built-in wordlist + SiteSettings `ProfanityWordlist`, strips `ProfanityAllowlist` matches, exposes `ContainsProfanity(string)`. Registered as scoped; wordlist loaded from SiteSettings on each check (reads current in-memory setting). | M |
| Q9.2 | `PrayerRequestRepository` in Infrastructure: queries for active/answered lists, member-visible filter, prayed-for count, per-user prayed status. | M |
| Q9.3 | `PrayerRequestService` in Application layer: submit (with ProfanityFilter check), edit-own or editor-edit, status change, `MarkPrayedForAsync` / `UnmarkPrayedForAsync` (idempotent toggle), add update (Editor+ only), delete (submitter own or Editor+). Anonymous requests hide submitter name in DTOs when `IsAnonymous=true`. | L |
| Q9.4 | `MemberPrayerRequestsController` (`/api/prayer-requests`, `[Authorize]`): submit, list (active + answered within archive window), detail, mark/unmark prayed, edit/delete own. | M |
| Q9.5 | `AdminPrayerRequestsController` (`/api/admin/prayer-requests`, `[Authorize(Roles=...)]`): full list with filters, detail, post update, change status, bulk archive, delete. | M |
| Q9.6 | SignalR: emit `PrayerRequestCreated`, `PrayerRequestUpdated`, `PrayerRequestStatusChanged`, `PrayerRequestPrayedForCountChanged`, `PrayerRequestUpdateAdded` to "members" group (Editors/Admins are in "members" group too). | M |
| Q9.7 | Integration tests: cross-user edit denied; anonymous-display privacy (submitter name hidden); ProfanityFilter blocks on submit; Editor can post update but Member cannot; PrayedFor unique constraint honored. | L |

### Stage Q10 — Prayer Requests — SPA

| # | Step | Effort |
|---|---|---|
| Q10.1 | `usePrayerRequestUpdates(prayerRequestId?)` SignalR hook: subscribes to all Prayer-related events; updates list/detail state in real-time (new requests at top, prayed-for counts, new updates). | M |
| Q10.2 | `/prayer-requests` member list page: active + answered cards, prayed-for toggle button, real-time new-request highlight (subtle fade-in). | M |
| Q10.3 | `/prayer-requests/new` submit form: title, TipTap body, anonymous checkbox. Error display for profanity rejection. | M |
| Q10.4 | `/prayer-requests/{id}` detail page: full content, prayed-for button + count, updates list. Editor/Admin sections: "Post Update" (TipTap) + "Change Status" + action buttons. Submitter own-request section: Edit / Mark Answered / Archive. | L |
| Q10.5 | `/admin/prayer-requests` moderation view: full list with filters, bulk actions. | M |
| Q10.6 | 375px mobile audit on prayer request pages. | S |

### Stage Q11 — Connect Card — Backend

| # | Step | Effort |
|---|---|---|
| Q11.1 | `ITurnstileValidationService` / `TurnstileValidationService` in Infrastructure: POSTs to Cloudflare's `siteverify` endpoint, returns `isSuccess`. Skips validation when `CloudflareTurnstileSiteKey` is not configured (dev mode). Registered as transient (stateless HTTP call). | M |
| Q11.2 | `ConnectCardRepository` in Infrastructure. `ConnectCardService` in Application: submit (runs Turnstile validation, honeypot check, 5s time-to-submit check; persists submission; sends ack email via `IEmailService`; emits SignalR event), status update, notes update, resend ack. | M |
| Q11.3 | Rate-limiting middleware (ASP.NET Core built-in): sliding-window policy `5 per hour`, key = SHA-256 of remote IP, applied only to `POST /api/public/connect-card`. Reject with `429 Too Many Requests`. | M |
| Q11.4 | `PublicConnectCardController` (`/api/public/connect-card`, anonymous): `POST` for submission. FluentValidation: Name required, at least one of Email or Phone required, HowDidYouHear required, max-length checks. | M |
| Q11.5 | `AdminConnectCardsController` (`/api/admin/connect-cards`, Editor+): list with filters, detail, status update, notes update, resend, bulk actions. | M |
| Q11.6 | SignalR: emit `ConnectCardSubmitted` to "admins" group with summary DTO (name, email, phone, submitted time). | S |
| Q11.7 | Integration tests: Turnstile validation mocked (pass/fail paths); honeypot rejection; time-to-submit rejection; rate-limit integration (mock clock). | M |

### Stage Q12 — Connect Card — SPA

| # | Step | Effort |
|---|---|---|
| Q12.1 | `/connect` public form: all fields per spec. Cloudflare Turnstile widget (JS injected; widget rendered; token captured in hidden field). Honeypot hidden field. 5s elapsed-time check via `Date.now()` at load vs. submit. | L |
| Q12.2 | `/connect/thank-you` confirmation page: thank-you message (from SiteSettings). Navigation back to home. | S |
| Q12.3 | `/admin/connect-cards` list page: filterable by status, searchable by name/email, sortable by date. | M |
| Q12.4 | `/admin/connect-cards/{id}` detail page: full submission, status controls (dropdown + save), internal notes textarea, Resend Acknowledgment button, hard-delete (Admin only with confirmation). | M |
| Q12.5 | `useAdminNotifications()` hook extension: subscribe to `ConnectCardSubmitted` event, toast in admin shell with link to queue. | S |
| Q12.6 | 375px mobile audit on public connect card form. | S |

### Stage Q13 — Blog — Backend

| # | Step | Effort |
|---|---|---|
| Q13.1 | `BlogPostRepository` in Infrastructure: list (paginated, published, non-future-dated, category filter, author filter, pinned-first), get by slug, get by author, full admin list with draft/status/date filters. | M |
| Q13.2 | `BlogService` in Application layer: create/edit/publish/unpublish (Editor+ only, can author on behalf of another user), delete (submitter own or Admin), reading-time calc `max(1, ceil(wordCount/250))` from ProseMirror JSON plain-text extraction, tag upsert (reuse TagService normalize pattern), `RelatedSermonId` FK validation. | L |
| Q13.3 | `PublicBlogController` (`/api/public/blog`): list, by-slug, by-category, by-author. Members-only posts excluded for anonymous callers. Output caching: list = 5 min (`blog-list` tag), slug = 5 min (`blog-{slug}`), category = 5 min (`blog-category-{cat}`), by-author = 5 min (`blog-by-{userId}`). | M |
| Q13.4 | `AdminBlogController` (`/api/admin/blog`): full CRUD + publish/unpublish. Cache invalidation on write: evict `blog-{slug}`, `blog-list`, `blog-category-{cat}`, `blog-by-{authorId}`, `homepage`, `sitemap`, `search`. | M |
| Q13.5 | OG + JSON-LD `Article` schema generation in public blog detail endpoint: `og:title`, `og:description`, `og:image`, `og:type=article`, `article:author`, JSON-LD with `@type: Article`, `author.name`, `datePublished`. Return as part of the detail DTO (same pattern as news items). | M |
| Q13.6 | Search index: bootstrap for BlogPost on first run; invalidate `search` on blog write. | S |
| Q13.7 | Integration tests: reading-time calc (0, 250, 251, 500 words); members-only post excluded from anonymous list; related sermon FK validation (non-existent sermon rejected). | M |

### Stage Q14 — Blog — SPA

| # | Step | Effort |
|---|---|---|
| Q14.1 | `/blog` public index: pinned posts always first, category filter chips, paginated list with "Load More", post cards (hero image, title, excerpt, author, date, category, reading time, tags). | L |
| Q14.2 | `/blog/category/{category}` page: category-filtered list, reuses card component. | S |
| Q14.3 | `/blog/by/{userId}` author archive: author photo + name + bio (`PublicAuthorBio`), post list below. Public page (no auth required). | M |
| Q14.4 | `/blog/{slug}` detail page: hero, title (H1), author byline (photo + name, linked to author archive), date, category, reading time, TipTap body render, tags, Related Posts (same category, 3), Companion Sermon callout (if `RelatedSermonId`), social share buttons (Facebook, X, copy link), OG + JSON-LD tags injected into `<head>` (using existing SPA meta-tag pattern). | XL |
| Q14.5 | `/admin/blog` list: draft/published/all filter, category filter, author filter, date range. Create/Edit form: TipTap body, title + slug, author dropdown (Editor+ can pick any author), category, tags chip input (autocomplete from TagService), hero image upload, excerpt with manual override, related sermon picker, members-only toggle, pinned toggle, draft/publish, scheduled publish date (captured, no automation), version history tab. | XL |
| Q14.6 | 375px mobile audit: post list cards stack, reading view comfortable. | S |

### Stage Q15 — Facebook OAuth Account Linking

| # | Step | Effort |
|---|---|---|
| Q15.1 | Install `Microsoft.AspNetCore.Authentication.Facebook` NuGet. Configure the handler in `Program.cs` conditional on `FacebookLoginEnabled` SiteSettings key (injected from appsettings/environment; see note below). | M |
| Q15.2 | Facebook OAuth callback handler: look up `AspNetUserLogins` by provider + providerKey. If found → sign in and redirect. If NOT found → **reject** with error message "No member account is linked to this Facebook profile. Please log in with your password first, then link Facebook from your profile." Absolutely no account creation path. | M |
| Q15.3 | `FacebookLinkController` (`/api/auth/facebook`): `GET /challenge` (initiate OAuth for an already-authenticated user), `GET /callback` (complete link for authenticated user → store in `AspNetUserLogins`), `POST /unlink` (remove from `AspNetUserLogins`). All require `[Authorize]` except the OAuth redirect itself. | M |
| Q15.4 | `/login` page update: "Continue with Facebook" button shown when `FacebookLoginEnabled=true` (from SiteSettings API). | S |
| Q15.5 | Profile Account tab: Facebook section renders link/unlink based on `GET /api/profile/facebook-status`. | S |
| Q15.6 | **Configuration note:** `FacebookOAuthAppId` and `FacebookOAuthAppSecret` are stored in SiteSettings (DB). Because ASP.NET Core authentication middleware is configured at startup, and SiteSettings are in DB, the Facebook handler is configured dynamically via `IOptionsMonitor<FacebookOptions>` backed by a custom `IConfigureNamedOptions` that reads the DB at configuration time. If either key is null, the handler is a no-op. Document this pattern in IMPLEMENTATION_NOTES. | M |
| Q15.7 | Integration tests: unlinked Facebook ID does not create account; link requires authenticated session; unlink succeeds; post-unlink login requires password. | M |

### Stage Q16 — Site Settings UI Wiring

| # | Step | Effort |
|---|---|---|
| Q16.1 | Privacy & Security tab (Administrator only): Profanity Filter section (wordlist + allowlist textareas, "Test phrase" input that calls a new `POST /api/admin/profanity/test` endpoint and shows result inline). | M |
| Q16.2 | Privacy & Security tab: Connect Card section (interests as chip input, acknowledgment message TipTap editor, Turnstile keys). Prayer Requests section (archive days, require-approval flag). | M |
| Q16.3 | Content tab additions: Blog categories (chip input), Class audience age groups (chip input), class display settings (show recent past toggle, lookback days number input). | M |
| Q16.4 | Integrations tab additions: Facebook OAuth app ID + app secret (masked/reveal-on-click, same pattern as YouTube secret from Phase 3), Facebook Login enabled toggle. | S |
| Q16.5 | `GET /api/admin/profanity/test` endpoint (Administrator only): accepts `{ phrase: string }`, returns `{ containsProfanity: bool }`. | S |

### Stage Q17 — Search Index Integration for Phase 4 Entities

| # | Step | Effort |
|---|---|---|
| Q17.1 | `SearchIndexer.BootstrapAsync` additions: Groups (Public and MembersOnly with flag), ClassSlots + active ClassOfferings (public fields only), BlogPosts (published; members-only with flag), PrayerRequests (Active + Answered, always `IsMembersOnly=true`). | M |
| Q17.2 | Content-write invalidation: all new entity write-paths call `_cache.InvalidateAsync(["search"])` and schedule reindex of affected entries. | S |
| Q17.3 | Search results page: add entity-type icons for Group (Lucide `Users`), Class (`BookOpen` already used for sermons — use `GraduationCap` or `Library`), Blog (`FileText`), PrayerRequest (`Heart`). | S |
| Q17.4 | Permission filtering in search: anonymous users see results where `IsMembersOnly=false` only. Members see all. | S |

### Stage Q18 — Seed Data

| # | Step | Effort |
|---|---|---|
| Q18.1 | 4-6 sample Member-role users (realistic names, varied directory opt-in settings). Idempotent (skip if any seeded member users exist). | M |
| Q18.2 | 3-4 sample Groups across visibility levels ("Men's Bible Study" Public/Open, "Worship Team" MembersOnly/InviteOnly, "Senior Citizens Outing" MembersOnly/Open, "Pastoral Care Recipients" Hidden/InviteOnly). Group memberships assigning seeded members + designating at least one Leader per group. | M |
| Q18.3 | 2-3 ClassSlots + current + upcoming ClassOffering per slot (Adult Class, Youth Class, Children's Class). | S |
| Q18.4 | 2-3 PrayerRequests (mix of anonymous and named), one with a PrayerRequestUpdate from an admin. | S |
| Q18.5 | 2 BlogPosts (one Devotional, one Sermon Notes; Sermon Notes post linked to a seeded Phase 3 sermon; both published). | S |
| Q18.6 | 1-2 ConnectCardSubmissions in different statuses (New, FollowedUp). | S |

### Stage Q19 — Tests

| # | Step | Effort |
|---|---|---|
| Q19.1 | **Application tests:** Group permission rules (leader cannot promote, editor can approve, admin can do all); ProfanityFilter integration (blocked word returns true; allowlist word suppresses); Member directory privacy filter (unlisted user not returned); PrayerRequest anonymity DTO (IsAnonymous=true → submitter name null in DTO); Blog reading-time calc (edge cases). | L |
| Q19.2 | **Infrastructure tests:** ProfanityFilter wordlist load from SiteSettings; Turnstile validation mock (success + failure); `GroupMembershipRepository` queries for leader/pending/active status. | M |
| Q19.3 | **Api integration tests:** Cross-user access denied on profile endpoints; Members directory non-listed user 404; Group join-request end-to-end; PrayerRequest Editor-only update post (Member gets 403); Connect Card submission with Turnstile mock; Blog members-only post excluded from anonymous API response; Facebook callback for unlinked ID rejected. | XL |
| Q19.4 | **SPA tests:** Profile tab save interactions; Members directory renders with privacy; Group join modal; Prayer request live-update via SignalR mock; Connect Card form field validation; Blog category filter. | L |

### Stage Q20 — Documentation, Mobile Audit, Final Verification

| # | Step | Effort |
|---|---|---|
| Q20.1 | `IMPLEMENTATION_NOTES.md` additions: privacy enforcement pattern (server-side field filtering); ProfanityFilter integration and wordlist/allowlist merge strategy; SignalR "user-{userId}" group pattern; Facebook linking flow and no-account-creation enforcement; Profile tab mobile pattern decision (horizontal scroll); `PhoneNumber` Identity-inherited field decision; `FacebookOptions` dynamic configuration via `IConfigureNamedOptions`; `ScheduledPublishAt` captured-but-inert decision. | M |
| Q20.2 | `README.md` additions: Facebook OAuth setup (App ID + Secret in SiteSettings, `FacebookLoginEnabled` flag), ProfanityFilter management (wordlist + allowlist in Site Settings → Privacy & Security), Cloudflare Turnstile setup, group leader management guide, connect card workflow. | M |
| Q20.3 | `ROADMAP.md` deferred items: member-to-member messaging, class signup/RSVP, class series cross-linking to Sermons/Blog, bulk group membership operations, Prayer Request comments by members, Blog comments, group categories/tagging. | S |
| Q20.4 | 375px mobile pass on every new Phase 4 page not already audited inline. | M |
| Q20.5 | Final smoke: `dotnet build` clean (no warnings), `dotnet test` green, `npm run build` clean, `npm test` green, API boots, spot-check public endpoints, spot-check admin endpoints, spot-check member-only endpoints. | M |
| Q20.6 | `IMPLEMENTATION_NOTES.md` closing entry: end-of-Phase-4 state (test counts, new entity count, migration name, bundle delta). | S |

---

## Q-3. Dependencies & Critical-Path Notes

- **Q0 → Q1** Domain entities must exist before EF configuration and migration.
- **Q0, Q1 → Q2–Q19** All subsequent stages depend on the migration having run.
- **Q1.5 → Q16** SiteSettings keys must be defined before the Settings UI wires them.
- **Q2 → Q3** Profile API before profile SPA.
- **Q2 → Q4** `IApplicationUserRepository` directory-query methods before Directory API.
- **Q5 → Q6** Groups backend before groups SPA; Q5.6 (SignalR) also feeds Q6.7.
- **Q7 → Q8** Classes backend before classes SPA.
- **Q9 → Q10** Prayer Requests backend (including SignalR events) before SPA.
- **Q9.1 (ProfanityFilter NuGet)** before Q9.3 (service that uses it) and Q16.1 (test-phrase UI).
- **Q11 → Q12** Connect Card backend before SPA.
- **Q13 → Q14** Blog backend before SPA.
- **Q15.1 (NuGet)** can be done any time after Q1; Q15.2-Q15.7 depend on it.
- **Q17** after all entity backends exist (Q5, Q7, Q9, Q13).
- **Q18** after all entity backends and seedable repositories exist.
- **Q19** after all backends complete (Q2–Q18); SPA tests (Q19.4) after all SPA stages.
- **Q20** final; depends on everything else.

Critical path: **Q0 → Q1 → Q2 → Q5 → Q9 → Q13** (the deepest data-layer chain),
with **Q3, Q4, Q6, Q7, Q8, Q10, Q11, Q12, Q14** layering on their respective backends.
**Q15** (Facebook) and **Q16** (Settings UI) are independent once Q1 is done.
**Q17 → Q18 → Q19 → Q20** close the phase.

---

## Q-4. Risks

| Risk | Mitigation |
|---|---|
| `IdentityUser<Guid>.PhoneNumber` conflict with spec's PhoneNumber field. | Use the inherited property; enforce max-length via FluentValidation rather than `[MaxLength]` on entity. Document. |
| `Microsoft.AspNetCore.Authentication.Facebook` configured at startup but secrets are in the DB (runtime). | `IConfigureNamedOptions<FacebookOptions>` backed by `IOptionsMonitor<SiteSettingsService>` so changes take effect on next request without restart. Document pattern. |
| Cloudflare Turnstile widget loads from an external CDN — Connect Card fails in offline CI. | `TurnstileValidationService` is skipped (returns success) when `CloudflareTurnstileSiteKey` is null. Tests mock the HTTP call. SPA conditionally renders the widget only when key is configured. |
| ProfanityFilter built-in wordlist too aggressive or too lenient for a church context. | Site Settings allows admin to add words (wordlist) and suppress false positives (allowlist). Test-phrase tool lets admin verify before deploying new words. |
| Blog's TipTap word-count / reading-time extraction: ProseMirror JSON structure may nest text across complex node types. | Extract all `text` leaf nodes recursively from the JSON tree (same utility already used for iCal DESCRIPTION plain-text in Phase 3 — reuse `StripHtml`-equivalent). |
| SignalR "user-{userId}" group flooding: every online member gets individual group entry. | Negligible at church scale; SignalR in-memory transport handles thousands of groups. Document. |
| Cross-member access bugs — the highest-risk category for privacy. | Service-layer guard (`throw ForbiddenException if UserId != currentUserId`) is the primary defense. Integration tests that authenticate as User A and attempt to read/write User B's data are mandatory, not optional, for profile, directory, and prayer-request endpoints. |
| EF temporal table on `ConnectCardSubmission` storing PII indefinitely. | The temporal table retains PII in history. Document that GDPR data-removal requests require manual history purge (note in IMPLEMENTATION_NOTES + ROADMAP as an operator task). |

---

## Q-5. What I Will NOT Do in Phase 4

- No email broadcasts (SendGrid) — Phase 5.
- No scheduled-publishing background job — Phase 5 (field captured, no logic runs).
- No email-on-publish for News or Blog — Phase 5.
- No Volunteer Signups — Phase 5.
- No SMS service stub — Phase 5.
- No Astro docs site — Phase 6.
- No GA4 + cookie banner — Phase 6.
- No RSS feeds — Phase 6.
- No member-to-member messaging — ROADMAP.
- No class signup/RSVP — ROADMAP.
- No cross-linking of Class series to Sermons/Blog — ROADMAP.
- No Prayer Request comments by members — ROADMAP.
- No Blog comments — ROADMAP.
- No group categories/tagging — ROADMAP.
- No "Duplicate for next term" button on ClassOfferings — per prompt.
- No Turnstile retroactively on event registration — Phase 3 backlog; not Phase 4 scope.

If during implementation I find myself drawn into any of the above, I will stop and ask.

---

## Q-6. Awaiting Review

This Phase 4 plan is the only deliverable until you approve it.  Once approved
(with or without adjustments), I'll execute Q0 through Q20 in order, updating
`IMPLEMENTATION_NOTES.md` as I go and surfacing genuine ambiguities via `// TODO:`
comments rather than guessing silently. Phases 1, 2, and 3 deliverables stay intact
throughout.

---

# Phase 5 Build Plan — Communications

**Status:** Plan drafted; awaiting review. No Phase 5 implementation has started.

---

## R-0. Confirmation of Understanding

I have read all 17 sections of the Phase 5 prompt plus Out-of-Scope and Final
Checklist. My understanding:

- **Email deliverability rigor is the highest-priority constraint.** Every
  non-transactional send must check the suppression list, every broadcast email
  must carry RFC 2369 + RFC 8058 unsubscribe headers, one-click unsubscribe must
  resolve immediately (not within 24h), and transactional vs. broadcast must be
  cleanly separated. Failure = real domain reputation damage.
- **Phase 5 introduces eleven feature slabs:** real email delivery (SendGrid /
  SMTP / no-op), suppression list, SendGrid webhooks, broadcast composer, email
  templates, email-on-publish, scheduled publishing, notification batching,
  one-click unsubscribe, SMS stub, volunteer signups.
- **`IEmailService` is being redesigned (breaking change).** Phase 1's
  `SendAsync(EmailMessage)` becomes `SendTransactionalAsync` +
  `SendBroadcastAsync` + `IsConfiguredAsync`. The `EmailMessage` record gains
  `Category`, `UserId`, `ToName`, `Attachments`. ~6 existing callers
  (`ConnectCardService`, `UserAdminService`, password reset, group membership
  notifications, etc.) must be updated in lockstep with the interface change.
- **`NotificationHub`'s `admins` / `members` / `user-{userId:N}` groups already
  exist** (Phase 4) and are auto-joined on connect. Phase 5 adds new event
  payloads but no new group plumbing.
- **`BlogPost.ScheduledPublishAt` was captured but inert in Phase 4.** Phase 5
  acts on it AND adds the matching field to `NewsItem` (retroactive Phase 4
  schema extension, called out in the prompt).
- **`ApplicationUser` notification preferences already exist:**
  `ReceiveNewsEmails`, `ReceiveBlogEmails`, `ReceiveBroadcastEmails`,
  `ReceiveGroupEmailsGlobal`, plus per-membership `GroupMembership.ReceiveGroupEmails`.
  Phase 5 wires them into the recipient resolver — no new preference fields.
- **Phase 1–4 deliverables stay intact.** Phase 5 layers on top; no rebuilding.

---

## R-1. Phase 4 Inheritance — Things Phase 5 Touches

| Touch point | Change |
|---|---|
| `IEmailService` (Phase 1) | Surface redesigned. Existing `SendAsync` removed; `SendTransactionalAsync` / `SendBroadcastAsync` / `IsConfiguredAsync` added. All callers refactored. |
| `LoggingEmailService` (Phase 1) | Updated to satisfy new interface; kept as default when provider=None or unconfigured. |
| `BlogPost.ScheduledPublishAt` (Phase 4) | Now actively published by `ScheduledPublishingService`. Field unchanged. |
| `NewsItem` (Phase 2/4) | Add `ScheduledPublishAt` and `SendEmailOnPublish` fields + migration. |
| `BlogPost` (Phase 4) | `SendEmailOnPublish` already on entity; verify default OFF; wire into publish flow. |
| `ConnectCardService` (Phase 4) | Acknowledgment email refactored to use `EmailTemplate` / `IEmailTemplateService`. |
| `UserAdminService` (Phase 1) | Invitation + reset flows refactored to use templates. |
| `GroupMembershipService` (Phase 4) | Approve/decline emails refactored to use templates. |
| `EventRegistrationService` (Phase 3) | Confirmation/cancellation/waitlist-promotion emails refactored to use templates; new reminder emails added (24-48h before event). Add `ReminderEmailSentAt` column on `EventRegistration`. |
| User hard-delete flow (Phase 1/4) | Extended to NULL `EmailBroadcastRecipient.UserId` references while preserving snapshot fields. |
| `SiteSettings` | Adds Phase 5 keys (provider config, From/Reply-To, SendGrid key+webhook secret, SMTP host/port/creds, EmailEnabled, TestEmailRecipient, News/Blog email target modes, admin-digest defaults, SMS provider stub fields). |
| `Event` (Phase 3) | New `EventVolunteerRole` child collection (FK relationship). |

---

## R-2. Clarifications to Surface Before Starting

Where I list a default, that's exactly what I'll do unless overridden.

1. **`IEmailService` redesign is a breaking change.** I'll do it as one
   coordinated migration in Stage R1: the interface, the `EmailMessage` /
   `BroadcastEmailMessage` / `EmailCategory` types, and all ~6 callers update in
   the same commit. No backward-compat shim. This avoids a half-migrated state
   where some sends bypass the new category/suppression checks. **Default:
   coordinated breaking refactor; document in IMPLEMENTATION_NOTES.**

2. **SendGrid webhook signature verification.** SendGrid uses ECDSA-signed
   payloads via `X-Twilio-Email-Event-Webhook-Signature` and
   `-Timestamp` headers. **Default:** use SendGrid's official `EventWebhook`
   helper from the `SendGrid` SDK to verify. Reject 401 on signature mismatch
   or missing/expired timestamp (>5min skew). Tests cover signed and unsigned.

3. **Suppression list — efficient lookup.** Per-recipient query would be N
   round-trips. **Default:** bulk-load suppressions for the resolved recipient
   batch via `WHERE EmailAddress IN (...)` once per send, then filter in memory.
   For broadcasts of N≤500 (church scale), this is one query.

4. **`SendGridEmailService` batching.** SendGrid's `mail/send` accepts up to
   1,000 personalizations per request. **Default:** chunk recipients into
   batches of 500 (conservative); each batch is one HTTP call with one
   personalization per recipient (so per-recipient merge fields work).
   Batch responses give per-message IDs for the recipient rows.

5. **One-click unsubscribe token.** **Default:** HMAC-SHA256 over
   `userId|category|timestamp` with a server-side `UnsubscribeSigningKey`
   secret stored in `SiteSettings` (auto-generated on first run if blank).
   Token format: `base64url(userId|category|timestamp|hmac)`. 30-day expiry.
   Single-use is NOT enforced (per RFC 8058 — operator may click link multiple
   times). Tokens expose userId — that's per-design, since the unsubscribe
   page must identify the recipient.

6. **`List-Unsubscribe-Post` header.** RFC 8058 requires the One-Click
   variant. **Default:** every broadcast email gets BOTH:
   `List-Unsubscribe: <https://...>, <mailto:unsubscribe@churchdomain>`
   and `List-Unsubscribe-Post: List-Unsubscribe=One-Click`. The HTTPS endpoint
   accepts POST with empty body and unsubscribes immediately (no confirmation
   page when called via this method).

7. **Broadcast worker concurrency.** **Default:** single-instance
   App Service deployment, single hosted-service worker (mirrors
   `VersioningTrimBackgroundService`). Status transitions guarded by
   `RowVersion` so a future scale-out doesn't double-send.

8. **Scheduled publishing tick interval.** Prompt says "every minute,
   configurable; default 60 seconds." **Default:** 60s, exposed via
   `Communications:ScheduledPublishingIntervalSeconds` in configuration (no
   Site Settings UI — operator-level setting).

9. **`TwilioSmsService` stub class.** Prompt: "constructor throws
   `NotImplementedException`." **Default:** the class exists in
   `CredoCms.Infrastructure.Sms` with the throwing ctor as documented; DI
   registers `NoOpSmsService` as the active `ISmsService` always. Twilio SDK
   NuGet IS added (per prompt) so v1.5 implementation is a one-file change.

10. **Volunteer roles per-occurrence overrides.** Prompt: "Roles apply across
    all occurrences of recurring events (no per-occurrence role definitions in
    v1)." **Default:** `EventVolunteerRole.EventId` is the only association;
    no per-occurrence row. Document as v1 limitation.

11. **Event reminder emails — new in Phase 5.** Phase 3 didn't ship reminders.
    Adding `ReminderEmailSentAt` to `EventRegistration` as part of Stage R0
    schema work (alongside the volunteer-signup analog field). Daily worker
    sends at the configurable time, idempotent via the new column.

12. **Email-on-publish duplicate prevention.** Prompt says
    `SendEmailOnPublish=false` is set after send. **Default:** flip the flag
    inside the same transaction that creates the corresponding
    `EmailBroadcast` row. Re-publishing a previously-sent post does not
    re-fire unless the editor explicitly re-checks the box.

13. **Test email send in Site Settings.** **Default:** `POST /api/admin/
    site-settings/test-email` accepts the in-flight (possibly unsaved)
    provider config in the request body, builds a transient
    `IEmailService` instance, and attempts to send. Returns success/failure
    detail. Avoids the chicken-and-egg of "save then test."

14. **`EmailTemplate` system templates immutable identity.** Prompt:
    "system templates cannot be deleted, only edited." **Default:**
    `IsSystemTemplate=true` blocks delete; subject + body editable; the
    `TemplateKey` is immutable (used as the lookup key by code paths).

---

## R-3. Ordered Implementation Stages

Effort key: **S** = small (<2h), **M** = medium (2-4h), **L** = large (4-6h), **XL** = extra large (6h+).

### Stage R0 — Domain Entities + EF Migration + SiteSettings Extensions

| # | Step | Effort |
|---|---|---|
| R0.1 | Domain entities: `EmailSuppression`, `EmailBroadcast`, `EmailBroadcastRecipient`, `EmailTemplate`, `WebhookEventLog`, `AdminNotificationLastSent`, `EventVolunteerRole`, `EventVolunteerSignup`. Enums: `EmailCategory`, `SuppressionType`, `SuppressionSource`, `BroadcastTargetMode`, `BroadcastSendMode`, `BroadcastStatus`, `RecipientStatus`, `EmailProvider`, `SmsProvider`, `AdminNotificationCategory`. | M |
| R0.2 | Field additions: `NewsItem.ScheduledPublishAt`, `NewsItem.SendEmailOnPublish`, `EventRegistration.ReminderEmailSentAt`. Verify `BlogPost.ScheduledPublishAt` + `SendEmailOnPublish` already present (Phase 4). | S |
| R0.3 | Repository interfaces: `IEmailSuppressionRepository`, `IEmailBroadcastRepository`, `IEmailBroadcastRecipientRepository`, `IEmailTemplateRepository`, `IWebhookEventLogRepository`, `IAdminNotificationLastSentRepository`, `IEventVolunteerRoleRepository`, `IEventVolunteerSignupRepository`. | M |
| R0.4 | EF configurations + temporal tables on `EmailBroadcast` and `EmailTemplate` (per spec). Indexes: `EmailSuppression.EmailAddress` (unique), `EmailBroadcast.Status`, `EmailBroadcast.ScheduledSendAt`, `EmailBroadcastRecipient.(BroadcastId, Status)`, `EmailTemplate.TemplateKey` (unique), `WebhookEventLog.EventId` (unique), `AdminNotificationLastSent.(UserId, Category)` unique, `EventVolunteerSignup.(EventVolunteerRoleId, OccurrenceDate, UserId)` filtered unique where `CanceledAt IS NULL`. | M |
| R0.5 | Extend `SiteSettings` with Phase 5 keys: `EmailProvider`, `EmailFromAddress`, `EmailFromName`, `EmailReplyToAddress`, `SendGridApiKey`, `SendGridWebhookSecret`, `SmtpHost`, `SmtpPort`, `SmtpUsername`, `SmtpPassword`, `SmtpUseSsl`, `EmailEnabled`, `TestEmailRecipient`, `NewsEmailTargetMode`, `NewsEmailTargetGroupIds`, `BlogEmailTargetMode`, `BlogEmailTargetGroupIds`, `EmailSubjectPrefixNews`, `EmailSubjectPrefixBlog`, `AdminNotificationFrequency` (default), `UnsubscribeSigningKey` (auto-gen on first read if blank), `SmsProvider`, `TwilioAccountSid`, `TwilioAuthToken`, `TwilioFromNumber`. Update `SiteSettingsDto` + `UpdateSiteSettingsRequest` + validator + service. | L |
| R0.6 | Migration: `Phase5_Communications`. Verify generation; apply to dev DB. | M |

### Stage R1 — IEmailService Redesign + Suppression Infra + Existing Caller Refactor

| # | Step | Effort |
|---|---|---|
| R1.1 | Refactor `IEmailService` per spec: `SendTransactionalAsync(EmailMessage, ct)`, `SendBroadcastAsync(BroadcastEmailMessage, ct)`, `IsConfiguredAsync(ct)`. New `EmailMessage` record with `ToAddress`, `ToName`, `Subject`, `HtmlBody`, `PlainTextBody`, `UserId?`, `Category`, `Attachments?`. New `BroadcastEmailMessage` record with `Recipients` list and `BroadcastId`. New `EmailRecipient` record (`Address`, `Name`, `UserId?`, `MergeFields`). | M |
| R1.2 | Update `LoggingEmailService` to satisfy new surface. `IsConfiguredAsync` returns true (logging is always available). | S |
| R1.3 | Suppression service: `IEmailSuppressionService.IsSuppressedAsync(email)`, `BulkLookupAsync(emails)`, `AddAsync(email, type, source, reason)`, `RemoveAsync(email, currentUser)`. All admin operations audit-logged. | M |
| R1.4 | Refactor every existing `IEmailService` caller (`ConnectCardService`, `UserAdminService` invitation + reset, `GroupMembershipService` approve/decline, prayer-request notifier if any) to use `SendTransactionalAsync` with appropriate `Category=Transactional` (or `GroupCommunication` for group emails). Inline strings preserved here; template refactor is Stage R6. | L |
| R1.5 | `EmailEnabled=false` short-circuit: when false (or provider=None), `SendTransactionalAsync` and `SendBroadcastAsync` log + return successfully without dispatching. Document the reasoning. | S |
| R1.6 | Tests: suppression bulk-lookup, EmailEnabled gate, transactional bypasses suppression list, broadcast respects suppression list. | M |

### Stage R2 — SendGridEmailService

| # | Step | Effort |
|---|---|---|
| R2.1 | Add `SendGrid` NuGet to Infrastructure. `SendGridEmailService` impl: ApiKey from SiteSettings; `SendTransactionalAsync` builds a single-personalization message; `SendBroadcastAsync` chunks 500-at-a-time, each chunk one HTTP call with per-recipient personalizations + merge-field substitutions. | L |
| R2.2 | Captures returned SendGrid `X-Message-Id` for each personalization. Returned to caller via a new `EmailSendResult` so broadcast recipient rows can store the IDs. | M |
| R2.3 | Failure handling: per-batch HTTP error → mark all recipients in batch as `Failed` with reason; partial-success per personalization → store individual results; transient errors retried via SendGrid SDK's built-in retry (verified in tests). | M |
| R2.4 | Tests (Infrastructure): mock the `ISendGridClient`; verify request body shape; verify chunking; verify error handling. | M |

### Stage R3 — SmtpEmailService

| # | Step | Effort |
|---|---|---|
| R3.1 | Add `MailKit` NuGet. `SmtpEmailService` impl: connects per send (no long-lived connection); reads host/port/credentials/SSL from SiteSettings; sends one message per recipient (no batching — generic SMTP doesn't have a batch API). | M |
| R3.2 | List-Unsubscribe + List-Unsubscribe-Post headers added on broadcast messages. Reply-To header set when `EmailReplyToAddress` configured. | S |
| R3.3 | Tests (Infrastructure): mock SmtpClient (or use abstraction); verify message construction + header presence + recipient iteration. | M |

### Stage R4 — Provider Selection + Test Send Endpoint

| # | Step | Effort |
|---|---|---|
| R4.1 | DI: `IConfigureNamedOptions<EmailProviderOptions>` reading SiteSettings; factory chooses concrete `IEmailService` impl (Logging / SendGrid / SMTP) at request scope. Falls back to `LoggingEmailService` when provider configured but credentials missing (with WARN log). | M |
| R4.2 | `POST /api/admin/site-settings/test-email` — accepts in-flight provider config; constructs transient `IEmailService`; sends to current admin's email; returns success/failure JSON. Administrator-only. | M |
| R4.3 | Tests (Api): provider switch behavior; missing-credentials fallback; test-send endpoint happy + error paths. | M |

### Stage R5 — SendGrid Webhook Endpoint

| # | Step | Effort |
|---|---|---|
| R5.1 | `POST /api/webhooks/sendgrid` (`AllowAnonymous`). Reads raw body + signature header + timestamp header; verifies via SendGrid SDK's `EventWebhook` helper using `SendGridWebhookSecret`. Rejects 401 on mismatch / >5min timestamp skew. | M |
| R5.2 | Event handler: parse JSON array; for each event, look up `WebhookEventLog` by `sg_event_id` and skip if processed; otherwise process per type (delivered / open / click / bounce hard / spam_report / unsubscribe / dropped); update `EmailBroadcastRecipient` by `SendGridMessageId`; update `EmailSuppression` for hard-bounce/spam/unsubscribe; on spam/unsubscribe set user's notification prefs to all-off; record event in `WebhookEventLog`. | L |
| R5.3 | Aggregate-stats updater on `EmailBroadcast`: increment `DeliveredCount`/`BouncedCount`/`ComplaintCount`/`OpenCount` after webhook event; emit `BroadcastStatsUpdated` SignalR to admins group; evict broadcast-stats cache. | M |
| R5.4 | Tests (Api + Infrastructure): signed valid request → 200 + processed; unsigned → 401; expired timestamp → 401; duplicate event_id → no-op; bounce/spam suppression flow; stats increment. | L |

### Stage R6 — Email Templates + Refactor of Existing Senders

| # | Step | Effort |
|---|---|---|
| R6.1 | `IEmailTemplateRepository` + `EmailTemplateService`: GetByKey, List, Update (system templates: subject + body only). | M |
| R6.2 | `IEmailTemplateRenderer.RenderAsync(templateKey, contextDict)` returning `RenderedEmail { Subject, HtmlBody, PlainTextBody }`. Uses `{{variable}}` substitution with strict-mode default (missing variable → `TemplateRenderException`). Common merge fields injected automatically: `churchName`, `currentYear`, `unsubscribeLink` (when applicable). | M |
| R6.3 | Plain-text auto-derivation: walks the TipTap ProseMirror JSON via existing extractor (Phase 4 reuse from BlogService) to produce a text fallback when manual override is null. | S |
| R6.4 | Refactor existing transactional callers (Stage R1's stop-gap inline strings) to use `IEmailTemplateRenderer`. Includes invitation, password reset, account activated, connect-card ack, group-join approve/decline, event-registration confirm/cancel/waitlist-promotion. | L |
| R6.5 | Admin templates UI: `/admin/email-templates` list + `/admin/email-templates/{key}` editor (TipTap body, merge-field documentation panel, test-send button). Administrator-only. | L |
| R6.6 | Seed system templates with placeholder copy. Idempotent (skip if any rows exist). | M |
| R6.7 | Tests: render with all fields present; render with missing required field throws; system template delete forbidden; key immutability. | M |

### Stage R7 — Broadcast Composer Backend + Worker

| # | Step | Effort |
|---|---|---|
| R7.1 | `IRecipientResolver`: resolves `(targetMode, targetGroupIds, category)` → `IReadOnlyList<EmailRecipient>` by joining ApplicationUser + GroupMembership + suppression-list bulk lookup, applying preference filters per category. | L |
| R7.2 | `EmailBroadcastService`: CRUD (create draft, update draft, send-now, schedule, cancel, test-send). Permission: Editors + Administrators. | L |
| R7.3 | `BroadcastSendWorker` (`BackgroundService`): polls for `Status=Sending` (immediate) and `Status=Scheduled AND ScheduledSendAt <= now`. Resolves recipients at send time (re-evaluation per spec). Creates `EmailBroadcastRecipient` rows; calls `IEmailService.SendBroadcastAsync`; stores returned message IDs; emits `BroadcastSendStarted` / `BroadcastSendCompleted` SignalR; transitions Status. Errors per recipient logged; broadcast moves to `Failed` only on whole-batch error. | XL |
| R7.4 | Endpoints: `POST /api/admin/broadcasts` (draft), `PUT /api/admin/broadcasts/{id}`, `POST .../send`, `POST .../schedule`, `POST .../cancel`, `POST .../test-send`, `POST .../preview-recipients`, `GET /api/admin/broadcasts`, `GET .../{id}`, `GET .../{id}/recipients`, `GET .../{id}/recipients.csv`. | L |
| R7.5 | User-hard-delete extension: NULL `EmailBroadcastRecipient.UserId` while preserving `EmailAddressSnapshot` / `DisplayNameSnapshot`. | S |
| R7.6 | Tests: recipient resolution (all-members vs groups; preference filters; suppression filter); send-now flow; schedule + cancel; test-send isolation (no rows created in main recipient table); RowVersion concurrency on status flip; CSV export. | XL |

### Stage R8 — Broadcast Composer SPA

| # | Step | Effort |
|---|---|---|
| R8.1 | `/admin/broadcasts` list page (status badges, recipient count, send time, delivered/bounce stats columns). | M |
| R8.2 | `/admin/broadcasts/new` composer: subject, TipTap body (with merge-field picker dropdown), plain-text auto/override section, target-mode radio, group multi-select with member counts, send-mode radio, scheduled-at picker, recipient-preview panel ("47 members" + sample names), Save Draft / Test Send / Send Now / Schedule buttons. | XL |
| R8.3 | `/admin/broadcasts/{id}` detail: aggregate stats cards, paginated recipient table with status filter, CSV export button, Cancel-Schedule action when applicable. | L |
| R8.4 | SignalR client: subscribes to `BroadcastSendStarted` / `BroadcastSendCompleted` / `BroadcastStatsUpdated` and updates list/detail in place. | M |
| R8.5 | Mobile audit: composer at 375px (fields stack; toolbar accessible). | S |

### Stage R9 — Email-on-Publish for News and Blog

| # | Step | Effort |
|---|---|---|
| R9.1 | News + Blog publish flow: when `IsPublished` flips false→true and `SendEmailOnPublish=true`, auto-create `EmailBroadcast` (Subject from prefix + title; Body from rendered preview template — hero + excerpt + Read More link; Target from SiteSettings news/blog target mode). Inside the same transaction set `SendEmailOnPublish=false` to prevent dupes on re-publish. | L |
| R9.2 | News + Blog edit forms: `SendEmailOnPublish` toggle visible; defaults from SiteSettings (News=ON, Blog=OFF). Inline help: "This will email all members on publish — once sent, the toggle clears automatically." | M |
| R9.3 | Templates: `NewsEmailPreview` and `BlogEmailPreview` system templates seeded; rendered by the broadcast worker with the post's data as context. | M |
| R9.4 | Tests: publish triggers broadcast; preference filter (news/blog) respected; flag clears post-send; re-publish does not re-fire unless re-enabled. | M |

### Stage R10 — Scheduled Publishing Background Service

| # | Step | Effort |
|---|---|---|
| R10.1 | `ScheduledPublishingService` (`BackgroundService`): tick interval 60s; per-tick queries Blog + News + EmailBroadcast for due records; flips IsPublished/PublishedAt or transitions broadcast Status; runs post-publish workflows (search index upsert, cache eviction, email-on-publish trigger). RowVersion-guarded flip. | L |
| R10.2 | Audit log entry per auto-publish (action: `AutoPublished`, entity type, entity ID, scheduled time vs. actual time). | S |
| R10.3 | News + Blog edit forms: Publish-Now / Save-Draft / Schedule-for-Later. Status badges show "Scheduled for [date+time]". Cancel-schedule by clearing the date or reverting to Draft. | M |
| R10.4 | Tests: due record published; not-yet-due skipped; concurrent tick safety (RowVersion); audit entry written; broadcast schedule path. | M |

### Stage R11 — Notification Batching (Admin Digests)

| # | Step | Effort |
|---|---|---|
| R11.1 | `AdminNotificationDigestService` (`BackgroundService`): tick every 5 minutes; for each Editor/Administrator with `IsActive=true`, computes pending counts since `AdminNotificationLastSent.LastSentAt` for each `AdminNotificationCategory`; if frequency window elapsed (default 30min, configurable), sends digest using `ConnectCardDigest` / `GroupJoinRequestDigest` template with item summaries + admin links; updates `LastSentAt`. | L |
| R11.2 | `/profile/notifications` extension (Editor+ only): Admin Notifications section with per-category frequency override (Off / 30min / 1h / Daily). | M |
| R11.3 | Templates seeded: `ConnectCardDigest`, `GroupJoinRequestDigest`. | S |
| R11.4 | Tests: zero pending → no send; frequency-window check; per-user override beats default; multiple categories independent. | M |

### Stage R12 — One-Click Unsubscribe

| # | Step | Effort |
|---|---|---|
| R12.1 | `UnsubscribeTokenService`: HMAC-SHA256 sign + verify with `UnsubscribeSigningKey`; token format `base64url(userId|category|ts|hmac)`; 30-day expiry; auto-generates secret on first read if blank. | M |
| R12.2 | `GET /unsubscribe?token=...&category=...` endpoint: validates token; renders confirmation page (anonymous-OK); on confirmation `POST` (or HEAD/POST per RFC 8058 one-click), flips the appropriate `Receive*` preference; sends `BroadcastUnsubscribeConfirmation` template. Redirect to `/profile/notifications` with success banner if user is logged in. | M |
| R12.3 | `POST /unsubscribe` (RFC 8058 one-click endpoint): empty body, immediate flip, no confirmation page. | S |
| R12.4 | Broadcast email footer renderer: appends "Unsubscribe from [Category] | Unsubscribe from all | Manage preferences" links + List-Unsubscribe + List-Unsubscribe-Post headers on every non-transactional send. Server-side enforced (broadcast send pipeline rejects messages without footer). | M |
| R12.5 | Tests: token round-trip; expired token rejected; wrong-category rejected; one-click POST; preference flip persisted; bulk-unsubscribe adds to suppression list and flips all prefs. | M |

### Stage R13 — Volunteer Signups

| # | Step | Effort |
|---|---|---|
| R13.1 | `EventVolunteerRoleService` (admin CRUD) + `EventVolunteerSignupService` (member sign-up / cancel; admin list). Capacity check enforced server-side: count active signups for `(roleId, occurrenceDate)` < `SlotsNeeded`. | L |
| R13.2 | Endpoints: `GET/POST/PUT/DELETE /api/admin/events/{id}/volunteer-roles`; `GET /api/admin/events/{id}/volunteer-signups`; `GET /api/admin/events/{id}/volunteer-signups.csv`; `POST /api/events/{id}/volunteer/signup`, `POST /api/events/{id}/volunteer/cancel`; `GET /api/profile/volunteer`. | L |
| R13.3 | Admin event-edit page: "Volunteer Roles" tab (add/edit/delete/reorder roles). Signups view with date+role+status filters. | L |
| R13.4 | Member event-detail page: "Volunteer for this event" section with role list + slot counts; for recurring events, occurrence selector (next 4–8 dates); confirmation modal on Sign Up. | L |
| R13.5 | `/profile/volunteer` page: list of upcoming commitments with [Cancel] action. | M |
| R13.6 | `EventVolunteerReminderService` (`BackgroundService`): runs daily at configurable time (default 9am); finds signups where `OccurrenceDate` is 1-2 days out, `CanceledAt IS NULL`, `ReminderEmailSentAt IS NULL`; sends `EventVolunteerReminder` template; sets `ReminderEmailSentAt`. | M |
| R13.7 | `EventRegistrationReminderService` (mirrors above): 24-48h-before reminder for `EventRegistration` rows using `ReminderEmailSentAt` column added in R0.2. | M |
| R13.8 | Templates seeded: `EventVolunteerSignupConfirmation`, `EventVolunteerCancellation`, `EventVolunteerReminder`, `EventRegistrationReminder`. | S |
| R13.9 | SignalR: `VolunteerSlotFilled` / `VolunteerSlotOpened` to admins group. | S |
| R13.10 | Tests: capacity enforcement; double-signup blocked; cancel reopens slot; reminder idempotency; permission gates. | L |

### Stage R14 — SMS Service Stub

| # | Step | Effort |
|---|---|---|
| R14.1 | `ISmsService` interface (`SendAsync`, `IsConfiguredAsync`) + `SmsMessage` record in Application. | S |
| R14.2 | `NoOpSmsService` (default impl, WARN-logs message and returns). DI registers always. | S |
| R14.3 | `TwilioSmsService` stub class — ctor throws `NotImplementedException("SMS via Twilio is not implemented in v1; planned for v1.5")`. Twilio NuGet referenced. Class exists for v1.5 swap-in. | S |

### Stage R15 — Site Settings Email & Notifications Tab

| # | Step | Effort |
|---|---|---|
| R15.1 | SPA Email & Notifications tab — Provider Configuration section: provider dropdown, conditional fields, From/Reply-To, Test Recipient, Test Send button. Secret fields use existing masked-input component (Phase 4 Turnstile). | L |
| R15.2 | Member Communications section: News/Blog target-mode radio + group multi-select; subject prefix fields. | M |
| R15.3 | Admin Notifications section: default frequency dropdown. | S |
| R15.4 | Suppression List section: paginated table (email/type/reason/created); manual-add modal; manual-remove with CAN-SPAM warning confirm. | M |
| R15.5 | SMS Configuration section: provider dropdown (None / Disabled-Twilio with v1.5 tooltip); stub fields dimmed. | S |
| R15.6 | Mobile audit: section accordion at 375px; tables → cards. | M |

### Stage R16 — Output Cache + SignalR Surface

| # | Step | Effort |
|---|---|---|
| R16.1 | Cache tag policy: broadcast detail/stats cached 30s with tag `broadcast-{id}-stats`; webhook events evict matching tag. Admin endpoints uncached. | S |
| R16.2 | SignalR additions documented + emitted: `BroadcastSendStarted`, `BroadcastSendCompleted`, `BroadcastStatsUpdated` (admins group); `VolunteerSlotFilled`, `VolunteerSlotOpened` (admins group). | M |

### Stage R17 — Seed Data

| # | Step | Effort |
|---|---|---|
| R17.1 | All system email templates seeded with placeholder copy + merge-field documentation. | M |
| R17.2 | One sample completed broadcast (`Status=Sent`, sample recipient stats) for empty-state UX. | S |
| R17.3 | Sample volunteer roles on a seeded event (e.g., "Setup Crew × 2", "Greeters × 2"); sample signups by seeded members. | M |
| R17.4 | Idempotency: every seed checks for existing rows first. | S |

### Stage R18 — Tests

| # | Step | Effort |
|---|---|---|
| R18.1 | Application tests (recipient resolution, template rendering, scheduled publishing, notification batching, unsubscribe tokens, volunteer capacity). Aim: +30 tests. | L |
| R18.2 | Infrastructure tests (SendGrid mocked, SMTP mocked, webhook signature verification, suppression-list updates from webhooks). Aim: +12 tests. | L |
| R18.3 | Api tests (broadcast endpoints, test-send, webhook signed/unsigned, unsubscribe one-click, volunteer signup endpoints). Aim: +18 tests. | L |
| R18.4 | SPA tests (broadcast composer interactions, email-template editor, volunteer signup modal, profile/volunteer). Aim: +6 tests. | M |

### Stage R19 — Mobile Responsive Audit

| # | Step | Effort |
|---|---|---|
| R19.1 | 375px pass on every Phase 5 admin and public surface not already audited inline. | M |

### Stage R20 — Documentation + Final Verification

| # | Step | Effort |
|---|---|---|
| R20.1 | `IMPLEMENTATION_NOTES.md`: provider selection logic, suppression policy + transactional bypass, webhook signature approach, recipient resolution precedence (suppression > preferences > membership), scheduled-publishing concurrency safety, notification batching design, one-click unsubscribe + RFC 8058 compliance, volunteer-signup v1 limitations. | M |
| R20.2 | `README.md`: SendGrid setup, SMTP alternative, webhook URL configuration in SendGrid dashboard, suppression-list admin guide, broadcast workflow, template customization. | M |
| R20.3 | `ROADMAP.md`: SMS via Twilio (v1.5), volunteer substitute requests, skill tracking, automated scheduling, "members not attended N months" segment, branded newsletter templates. | S |
| R20.4 | Final smoke: `dotnet build` clean, `dotnet test` green, `npm run build` clean, `npm test` green, API boots, send a real test email through each provider path (Logging fallback verified; SendGrid + SMTP only if creds available locally). | M |
| R20.5 | `IMPLEMENTATION_NOTES.md` closing entry: end-of-Phase-5 state (test counts, new entity count, migration name). | S |

---

## R-4. Dependencies & Critical-Path Notes

- **R0 → all** Migration must land before any service references new entities.
- **R1 → R2, R3, R4, R6, R7** New `IEmailService` surface must exist before
  any concrete implementation or template-using caller compiles.
- **R2 → R5** SendGrid impl exists before webhook handler references its
  signature helpers and message-ID correlation.
- **R6 → R7, R9, R11, R13** Templates + renderer required before any sender
  uses `RenderAsync(templateKey, context)`.
- **R7 → R9** Email-on-publish creates a broadcast — broadcast pipeline
  must work first.
- **R7 → R12** Broadcast send pipeline must enforce footer + List-Unsubscribe
  headers — implement R12 inside R7 (interleaved) so no broadcast is ever
  sent without an unsubscribe path.
- **R10 → R7, R9** Scheduled publish triggers email-on-publish which uses
  the broadcast pipeline.
- **R0.5 → R15** SiteSettings keys must exist before the UI binds to them.
- **R13** is largely independent (only needs R6 templates).
- **R14** is fully independent.
- **R17 → R18** Seeds before integration tests that rely on seeded templates.

Critical path: **R0 → R1 → R6 → R7 → R12 → R9 → R10 → R20.**
**R2, R3** are parallelizable after R1. **R5** parallelizable after R2.
**R11** parallelizable after R6. **R13, R14** parallelizable after R0/R6.

---

## R-5. Risks

| Risk | Mitigation |
|---|---|
| Breaking change to `IEmailService` mid-flight leaves callers broken. | Stage R1 is one atomic refactor; all callers updated in lockstep; full backend build must pass before R1 closes. |
| Webhook signature verification bug = security bypass. | Use SendGrid SDK's official `EventWebhook` helper, not hand-rolled crypto. Tests cover signed + unsigned + expired-timestamp paths. |
| Broadcast worker double-sends on restart. | Status flip Draft→Sending→Sent guarded by RowVersion; in-flight Sending rows checked for partial recipient progress on worker startup; resume from where left off. |
| Suppression list lookup performance for large broadcasts. | Bulk `WHERE EmailAddress IN (...)` once per send. At church scale (≤500 recipients) this is one query. |
| One-click unsubscribe token leaks userId. | By design — unsubscribe must identify the recipient. HMAC prevents tampering; 30-day expiry limits replay window. |
| `EmailEnabled=false` accidentally left on after go-live; nobody notices for days. | Site Settings UI shows persistent banner: "Email is disabled — outbound mail is not being sent" when `EmailEnabled=false` and provider configured. Admin notification on save when toggled off. |
| SendGrid free tier (100/day) hit during dev. | Document in README. `LoggingEmailService` is the dev default — explicit opt-in to live SendGrid. |
| Scheduled publish + email-on-publish double-send when content is republished. | `SendEmailOnPublish` flag flipped to false in same transaction as broadcast row creation; tests cover re-publish path. |
| Volunteer signup race: two members claim the last slot simultaneously. | Capacity check inside the same transaction as the insert; unique constraint on `(roleId, occurrenceDate, userId)` prevents same-user double-claim; capacity overrun resolved by transaction-scoped count + insert pattern (catch unique violation, return 409). |
| Email rendering inconsistency across clients. | Templates use simple inline-styled HTML — no flexbox/grid in body markup; tested against the SendGrid preview viewer. |
| Hard-deleted user's broadcast history loses meaning. | UserId nulled, but `EmailAddressSnapshot` + `DisplayNameSnapshot` preserved at send time → audit row remains meaningful. |

---

## R-6. What I Will NOT Do in Phase 5

- No SMS implementation (only stub). SMS is v1.5.
- No volunteer substitute requests, skill tracking, or automated scheduling — ROADMAP.
- No bulk recipient targeting beyond Groups (e.g., "members who haven't
  attended in N months") — ROADMAP.
- No newsletter-style branded email templates (advanced visual design) — ROADMAP.
- No Astro docs site — Phase 6.
- No GA4 + cookie banner — Phase 6.
- No RSS feeds — Phase 6.
- No final accessibility audit — Phase 6.
- No image-compression refinements (already done in Phase 2).
- No multi-instance scale-out hardening for the broadcast/publish workers
  (single App Service instance is the v1 deployment target; RowVersion
  patterns are used so v1.5+ scale-out is a small change).

If during implementation I find myself drawn into any of the above, I will stop and ask.

---

## R-7. Awaiting Review

This Phase 5 plan is the only deliverable until you approve it. Once approved
(with or without adjustments), I'll execute R0 through R20 in order, updating
`IMPLEMENTATION_NOTES.md` as I go and surfacing genuine ambiguities via
`// TODO:` comments rather than guessing silently. Phases 1, 2, 3, and 4
deliverables stay intact throughout. Email deliverability rigor (suppression
list checks, RFC 2369 + RFC 8058 unsubscribe headers, immediate one-click
response, transactional/broadcast separation) is treated as a hard
correctness constraint, not a nice-to-have.
