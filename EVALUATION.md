# Project evaluation

Single-pass static review across the codebase looking for issues,
bugs, and refactoring opportunities. Findings are bucketed by impact +
whether they're fixed inline (✓) or tracked as v1.x follow-ups (→).
The v1.x items are also captured in `ROADMAP.md`.

## Methodology

Looked at:

- Compiler / Roslyn analyzer warnings (`dotnet build -p:TreatWarningsAsErrors=false`).
- All `TODO` / `FIXME` / `HACK` comments across the repo.
- TypeScript `any` usage.
- Empty `catch` / swallow-style error handling.
- Sync-over-async patterns (`.Result`, `.GetAwaiter().GetResult()`, `.Wait()`).
- Hardcoded secrets / connection strings / API keys.
- Stray `console.*` statements.
- Dead-code candidates (exports never imported).
- Auth / authorization patterns across controllers.
- Duplicated logic that's begging for consolidation.

## Findings

### Security

**1. Default `RegistrationTokenSigner` secret could leak to production.** ✓ Fixed.
   The `TokenSigningSecret` ships with a hardcoded default dev value
   (`"credo-cms-dev-token-secret-change-me-in-production-please-do-it"`)
   that's used if config doesn't override. A misconfigured deploy would
   silently sign all "cancel my registration" tokens with the same
   known-globally-public string, breaking the security model. **Fix:**
   `Program.cs` now hard-fails at startup in Production when the secret
   is empty or equal to the default. Dev / Testing builds still accept
   the default silently so contributors don't need extra config.

**2. `RegistrationTokenSigner` accepts ≥16-char secrets but spec says ≥32.** →
   Class docstring + options doc-comment claim 32-char minimum; the
   actual check is `_key.Length < 16`. Loosen the docs to match (16 is
   secure enough for HMAC-SHA256) or tighten the check to 32. Track as
   v1.x.

**3. Tenant `PrimaryColor` / `AccentColor` accepted without sanitization.** →
   The fields are persisted as hex strings and emitted into CSS custom
   properties via JS. No CSP issues today, but if the admin pastes
   `url(javascript:alert(1))` style JS the runtime conversion via
   `hexToHsl` silently rejects it (regex check). Worth a server-side
   regex on save too. Track as v1.x.

### Bugs

**4. Output cache invalidation is process-local.** →
   `IOutputCacheInvalidator.InvalidateAsync(tag)` calls
   `IOutputCacheStore.EvictByTagAsync` which only evicts the local
   process. Two-instance App Service scale-out would have stale-cache
   bugs after every write. Already documented in
   ARCHITECTURE.md + ROADMAP. v1.x via Redis.

**5. `cookiePolicyPageSlug` + `cookiePolicyPageId` coexist on the admin
   `SiteSettings` TS type.** →
   Inheritance accident from extending `PublicSiteSettings`. The
   `UpdateSiteSettingsRequest` omits the slug to dodge ambiguity; works
   today but the dual-field type is confusing. Clean by un-extending
   `SiteSettings extends PublicSiteSettings` and declaring the admin
   shape separately. Wider refactor; v1.x.

**6. `BeliefsTeaser` on HomePage links to `/what-we-believe`** which is
   a Page slug seeded by `SeedSamplePagesAsync` — works on a fresh
   deploy. Earlier session note worried about 404 until PR #8 lands,
   but the seed already covers it. Closed; no action needed.

### Maintainability / consistency

**7. `<PublicPage>` primitive shipped in PR #1 conflicts with the
   PublicLayout chrome-shim approach.** →
   `PublicLayout` (existing wrapper for `<Route element={<PublicLayout/>}>`)
   already mounts `<PublicNavBar>` + `<PublicFooter>` (the shims delegate
   to the new template-aware versions). Wrapping a page in `<PublicPage>`
   inside that layout double-renders the chrome. PR #2's HomePage works
   around this by NOT using `<PublicPage>` and emitting sections in a
   fragment. Future option: either (a) slim down PublicLayout to be
   chrome-less (foundation refactor — call it PR #1.5), or (b) remove
   `<PublicPage>` from the public primitives index and document that
   pages compose chrome only via PublicLayout. v1.x decision.

**8. Two `ParaJson` helpers used to exist in `DataSeeder.cs`** — caught
   when the seed-data expansion (Track A) accidentally re-added a
   duplicate. ✓ Fixed (the duplicate was removed during the same
   commit). The remaining single helper is fine.

**9. `SmtpEmailService` has two `catch { /* swallow */ }` blocks** for
   disconnect failures. Acceptable — disconnect errors don't affect
   already-sent messages — but the bare `catch` could mask cancellation
   tokens. Worth tightening to `catch (Exception)` with explicit
   passthrough for `OperationCanceledException`. Minor; v1.x.

**10. Admin Dashboard has four `// TODO: wire endpoint` placeholders.** →
    `AdminDashboard.tsx` (sermon-of-the-week, recent activity, this-
    Sunday, tend-to action queue) all render hardcoded placeholders.
    Each needs a corresponding admin-dashboard endpoint. Documented;
    v1.x.

**11. `LoginPage.tsx` has a TODO** for pulling the current sermon
    pull-quote from a public API. The login page currently renders a
    static quote. Minor; v1.x.

**12. SPA index chunk is 700 KB** (gzip 215 KB).
    Vendor + initial-route code combined. Already documented in
    ROADMAP. v1.x via manualChunks / route-level dynamic imports.

### Code quality

**13. `console.warn` in `useNotificationHub`** for SignalR connection
    failures. Acceptable — infrastructure-level diagnostic, no PII —
    but inconsistent with the rest of the SPA which avoids console.*
    in favor of toast notifications + error context. Track as polish.

**14. No CI lint / format check.** →
    `ci.yml` builds + tests but doesn't run `dotnet format` or
    `eslint --max-warnings 0`. PRs that pass tests can still ship
    inconsistent style. Adding the checks is one workflow-step diff
    each. Track as v1.x.

**15. No regression net for public-page rendering** beyond the new
    HomePage tests. SeoTags break, layout drift, etc., would slip
    through. A small set of `WebApplicationFactory` snapshot tests for
    `/api/public/homepage`, `/sermons`, `/events` would catch the
    backend-shape regressions. Track as v1.x.

### Performance

**16. `IRecipientResolver.BulkLookupAsync` allocates `HashSet<string>`
    per call** to filter against suppression list. At church scale
    (≤500 recipients per send) this is negligible. No action.

**17. Background services tick every 15s–60s in process** — including
    the broadcast worker which polls on every tick. If the v1
    deployment grows to many broadcasts in flight, polling becomes
    inefficient. Long-term: switch to SQL Server Service Broker or
    AzureServiceBus. Documented; v2 / v1.x.

## Inline fixes applied in this evaluation

- ✓ #1 — production-environment validation of `RegistrationTokenSigner` secret.

## Carry-forwards (v1.x)

Added to `ROADMAP.md` under "v1.x candidates":

- Tenant color sanitization on save (#3)
- `cookiePolicyPageSlug`/`Id` type refactor (#5)
- `<PublicPage>` / `PublicLayout` consolidation (#7)
- SmtpEmailService swallow-block tightening (#9)
- Admin Dashboard live-data endpoints (#10)
- LoginPage pull-quote (#11)
- CI lint/format checks (#14)
- Backend public-endpoint regression tests (#15)

Items already in ROADMAP that this evaluation re-confirms:
Output cache scaling (#4), SPA bundle size (#12), background service
scaling (#17).

## What I did NOT find

- No SQL injection patterns. EF Core parameterizes; no raw `FromSql`
  with interpolation outside the few `FromSqlInterpolated` calls
  which use FormattableString correctly.
- No XSS patterns in user-supplied content paths — everything goes
  through React's auto-escape or TipTap's controlled JSON.
- No sync-over-async (the `.Result` grep hits were all `PagedResult`
  type references, not blocking calls).
- No empty `catch` blocks.
- No leaked HTTP secrets in source (the dev-default
  `RegistrationTokenSigner` secret is the closest, and it's now
  blocked at production startup).
- No `any` in TypeScript (the one grep hit was a comment line, not
  code).
