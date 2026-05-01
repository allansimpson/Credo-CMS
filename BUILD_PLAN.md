# Credo CMS — Phase 1 Build Plan

**Status:** Awaiting review. No implementation work has started.

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
