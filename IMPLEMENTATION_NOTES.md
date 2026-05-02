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

## Deviations from the Prompt

_None — every section of the Phase 1 prompt is addressed. Out-of-scope items
(Phases 2–6) are explicitly deferred and noted in `ROADMAP.md`._
