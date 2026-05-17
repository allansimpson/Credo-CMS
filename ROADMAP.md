# Roadmap

Final v1 ROADMAP after Phase 6 review. Each item is tagged:

- **[v1.x]** — likely follow-on work after v1 launch.
- **[v2]** — multi-tenancy / SaaS scope.
- **[out-of-scope]** — explicitly not planned.

If a feature appears here that you (the church operator) urgently
need, prioritize it via your project tracker. The list is not
ordered by priority within each tag.

## v1.x candidates

### Communications

- **[v1.x] SMS via Twilio** — interface + structural placeholder ship in v1; Twilio implementation planned for v1.5.
- **[v1.x] Refactor existing transactional senders to use IEmailTemplateRenderer** — invitation / password reset / connect-card ack / group-join decision currently use inline strings; templates seeded but cutover deferred.
- **[v1.x] Per-recipient List-Unsubscribe-URL header** — v1 ships RFC 2369 via broadcast-level mailto + body-footer per-recipient link; per-recipient HTTPS header via SendGrid Personalization.Headers is a follow-up.
- **[v1.x] Broadcast composer rich-text editor** — current composer is textarea-based; TipTap-based RTE with merge-field picker is a follow-up.
- **[v1.x] Recipient CSV export endpoint** — endpoint placeholder exists; SPA download path is a follow-up.
- **[v1.x] Bulk recipient targeting beyond Groups** — segments like "members who haven't attended in N months".
- **[v1.x] Branded newsletter-style HTML templates** — visually-designed multi-column templates with click tracking.

### Volunteers + events

- **[v1.x] Volunteer substitute requests** — v1 ships lightweight signup/cancel; future iteration adds "request a substitute" flow.
- **[v1.x] Volunteer skill tracking + availability** — first-class skill tags, availability windows.
- **[v1.x] Automated volunteer scheduling** — algorithmic match of skills + availability to roles.
- **[v1.x] File upload + repeating field groups in Event Registration** — currently single-value text/number/select fields only.
- **[v1.x] Bulk recurrence exceptions on Events** — apply the same override to multiple occurrences at once.
- **[v1.x] "Move to different date" recurrence override** — currently overrides cancel or change time; full date-shift not supported.
- **[v1.x] Drag-to-reschedule on the admin calendar** — currently edit-form only.

### Members + community

- **[v1.x] Class signup / RSVP** — currently public read-only browsing; RSVP / waitlist not modeled.
- **[v1.x] Prayer Request comments by members** — members can submit + pray-for; replying is admin-only.
- **[v1.x] Blog comments** — read-only public comments not modeled.
- **[v1.x] Member-to-member messaging** — 1:1 messaging not modeled.
- **[v1.x] Family/Household relationships in directory** — currently flat; family grouping deferred.
- **[v1.x] Bulk group membership operations** — add/remove many at once.
- **[v1.x] Group categories/tagging** — currently flat list of groups.

### Content + cross-linking

- **[v1.x] Sermon series ↔ blog cross-linking** — currently sermon ↔ single blog only.
- **[v1.x] Class series ↔ sermon cross-linking** — class slots can reference content but cross-link UX not surfaced.
- **[v1.x] Member-side giving history integration via Tithe.ly webhooks** — currently external link only.

### Performance + ops

- **[v1.x] Real-time output cache invalidation across multiple App Service instances via Redis** — single-instance is the v1 deployment target.
- **[v1.x] Real screenshots in Astro docs** — placeholder model used in v1 (`<Screenshot src="placeholder://..." />` renders a bordered placeholder).
- **[v1.x] Real Azure deployment dry-run** — Phase 6 ships the Bicep + GH Actions wiring + a local Docker Compose alternative; the actual end-to-end Azure dry-run is a required pre-launch acceptance test the project owner runs against their subscription.
- **[v1.x] axe-core automated accessibility tests on 5+ representative SPA pages** — Phase 6 ships the framework + ACCESSIBILITY.md target list; per-page test files are a follow-up.

### Other

- **[v1.x] Generic form builder** — beyond event registration's per-event field model.
- **[v1.x] Mobile native app** — separate project (iOS / Android wrappers around the SPA initially).
- **[v1.x] Live chat widget** — third-party embed (Crisp, Intercom, etc.).
- **[v1.x] Photo galleries** — image-grid content type with lightbox.

### From project evaluation

See `EVALUATION.md` for the diagnostic write-up. Open v1.x items:

- **[v1.x] Admin Dashboard live data** — four placeholder endpoints (sermon-of-the-week, recent activity, this-Sunday, tend-to action queue) need wiring.
- **[v1.x] Login page pull-quote** — currently static; pull from a public API.
- **[v1.x] Public-endpoint regression tests** — `WebApplicationFactory` snapshot tests for `/api/public/homepage`, `/sermons`, `/events` etc.
- **[v1.x] Fix `react-hooks/exhaustive-deps` warnings (8)** — pre-existing in admin pages; ESLint flags them as warnings. Either include the missing deps or wrap the load functions in `useCallback`.
- **[v1.x] Re-enable `react-refresh/only-export-components`** — currently disabled because the codebase co-locates `AuthContext` / `SiteSettingsContext` with their providers and hooks. Refactor those contexts into separate files, then re-enable the rule.

### From "from scratch" deep-dive analysis

- **[v1.x] Single job-table background runner** — replace 5 polling `BackgroundService` workers (broadcast, scheduled-publishing, admin-digest, volunteer-reminder, versioning-trim) with one `JobRunner` over a `Jobs` table with row-level leasing. Removes the dual-instance race and gives queryable history.
- **[v1.x] SaveChanges-interceptor cache invalidation** — replace ~28 manual `IOutputCacheInvalidator.InvalidateAsync(tag)` calls with an EF interceptor that derives invalidation tags from changed entity types. Removes the "remember to invalidate" footgun.
- **[v1.x] OpenAPI → SPA type codegen** — replace hand-written TS mirror of 192 DTO records. Drift becomes a compile error.
- **[v1.x] Validation consolidation** — pick FluentValidation as the single source; drop `[Required]` attributes on request DTOs and bespoke service-layer checks for already-validated invariants.
- **[v1.x] Migration squash at v1 tag** — fold migrations 1–23 into one `V1_Schema` baseline at the v1 release tag.
- **[v1.x] Demo seed → JSON fixtures** — split `DataSeeder.cs` system-required reference data from demo content; demo content reads from `Seeding/Fixtures/*.json` instead of inline `new BlogPost { ... }`.
- **[v1.x] Covert routing consistency** — `/docs/*` returns 404 to non-admins; other admin-only controllers return 401/403. Pick one (covert or standard) and apply across all admin-mounted controllers.
- **[v1.x] Composition-root split** — `Program.cs` (379 LOC) and `Infrastructure/DependencyInjection.cs` (272 LOC) split into per-concern composition modules.
- **[v1.x] `SettingsPage.tsx` per-tab split** — 823-line tabbed component → `pages/admin/settings/{BrandingTab,EmailTab,AnalyticsTab,…}.tsx`.
- **[v1.x] Email-provider routing source** — route by host-bound configuration, not by DB-settable `SiteSettings.EmailProvider`. Prevents misconfigured Production from routing live mail through `LoggingEmailService`.
- **[v1.x] Drop `<PublicPage>` primitive** — `PublicLayout` is the chrome provider; the primitive double-renders and is the documented reason PR #2's HomePage doesn't use it.
- **[v1.x] Delete chrome-shim components** — `PublicFooter` / `PublicHeader` shims around the canonical versions in `@/components/public/`. Replace import paths repo-wide and remove the shims.
- **[v1.x] `SiteSettings` TS type decoupling** — stop extending `PublicSiteSettings`; declare admin shape separately. Removes the `cookiePolicyPageSlug` / `cookiePolicyPageId` dual-field ambiguity.
- **[v1.x] Strip stale tenant-readiness comments** — multi-tenant forward-declarations scattered across entity / service files. Concentrate in `MULTI_TENANCY.md`; remove the comment debt.
- **[v1.x] Temporal-tables only where audited** — currently every table is temporal. Drop temporal versioning on tables that don't need it (join tables, notifications, broadcast tracking, webhook events). Net write-path + storage win.

### From "from scratch" — deferred to v2

- **[v2] Vertical-slice refactor** — replace the four-project DDD layering (Domain / Application / Infrastructure / Api) with folder-per-feature slices. Domain is too shallow to pay for the horizontal split.
- **[v2] Drop the repository layer** — 27 repos over EF Core's `DbContext`. Services take `ApplicationDbContext` directly; narrow projection helpers for shared `Select(... => new Dto())` shapes.
- **[v2] Re-evaluate Astro for docs** — admin-only docs site with Pagefind static search. Could be served as plain rendered Markdown via the API.

## v2 candidates (multi-tenancy + SaaS)

- **[v2] Multi-tenant architecture** — see [MULTI_TENANCY.md](./MULTI_TENANCY.md) for the migration path.
- **[v2] Per-church branding management** — currently single Site Settings row.
- **[v2] Multi-campus support** — currently single-location.
- **[v2] Cross-tenant analytics for the SaaS operator**.
- **[v2] Subscription billing**.
- **[v2] Self-service tenant signup**.

## Permanently out of scope

- **[out-of-scope] Native check-in stations** — require dedicated hardware.
- **[out-of-scope] Background check integration** — sensitive PII; out of project scope.
- **[out-of-scope] Worship song library** — separate domain (CCLI etc.).
- **[out-of-scope] Service planning** — Planning Center Services functionality is its own product.
- **[out-of-scope] Sacramental records** — tradition-specific; churches with this need can extend.
- **[out-of-scope] Public newsletter signup form** — project chose RSS feeds instead (Phase 6).
- **[out-of-scope] i18n / multilingual content** — English-only by design. Open to revisit if specific demand emerges.
