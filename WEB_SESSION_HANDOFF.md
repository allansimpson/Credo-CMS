# Web Session Handoff — Public Site Templates

> **For Claude Code (web session):** Read this file first, then the design
> handoff at [`PUBLIC_DESIGN_HANDOFF.md`](./PUBLIC_DESIGN_HANDOFF.md), then
> [`BUILD_PLAN.md`](./BUILD_PLAN.md). The opening prompt at the bottom of
> this file is the message to paste into the web session to pick up where
> the prior agent (this IDE-extension session) left off.

---

## Where things stand

### Branches

| Branch | Purpose | State |
|---|---|---|
| `main` | Production line | Phase 6 complete (commit `b3552fa`). |
| `claude/credo-cms-phase-1-06gIY` | Long-lived feature branch holding all six phases of CMS work + Phase 6 polish | Up to date with origin. `2cf7d97` adds `PUBLIC_DESIGN_HANDOFF.md`; everything before that is shipped Phase 1–6 work. **The parent branch for PR #1.** |
| `claude/public-site-pr1-foundation` | **PR #1** — Public Site visual refresh foundation | Pushed to origin. Two commits: `29bf839` (foundation) + `fef8114` (legacy chrome shims). Awaiting review. |

### What's shipped on `claude/public-site-pr1-foundation`

Foundation per the design handoff Section 9. Detailed list in commit
`29bf839`'s body; summary:

**Backend**
- `PublicTemplate` enum (`Editorial=0`, `Quiet=1`) in `CredoCms.Domain.Settings`.
- `SiteSettings.Template` field + EF conversion + migration
  `20260516024522_PublicSite_AddTemplateField`.
- DTOs + service + validator all round-trip the new field.
- `ColorContrast` helper in `CredoCms.Application.SiteSettingsManagement`
  (WCAG relative-luminance + ratio). For the AA soft-warning on tenant
  color overrides (warn-only per Q10).
- `DataSeeder` default Site Settings now uses Hope Community Church
  fixtures (Cedar Falls, IA — matches the prototype copy). Idempotent.

**CSS theme**
- `app/src/themes/church-theme.css` restructured with `[data-theme='church']`
  base block + `[data-template='editorial' | 'quiet']` token blocks.
- Tenant-overridable: `--primary`, `--accent` only.
  Template-fixed: everything else (`--bg`, `--panel`, `--inset`, `--inset-fg`,
  `--fg`, `--fg-soft`, `--muted`, `--border`, `--border-soft`, `--accent-fg`,
  `--accent-soft`).
- Per Q4 refinement: `--primary` maps to button/CTA usage only; headings
  use `--foreground` (template-fixed) so a tenant's primary color can't
  accidentally repaint type.
- `@font-face` declarations for Inter Tight + JetBrains Mono with
  `font-display: swap` + system fallback stack. Variable woff2 expected at
  `/fonts/` — operator drops the files during build.
- Global `border-radius: 0` enforced via CSS. `.rounded-full` is the only
  opt-in exception (Quiet pill filter chips).

**Tailwind**
- `app/tailwind.config.js` extended with `inset` + `inset.foreground`
  color tokens.

**Layout**
- `app/src/themes/ChurchThemeLayout.tsx` reads `settings.template`,
  writes `data-template` on the theme root at first paint + on settings
  change.

**Primitives** (all in `app/src/components/public/`, all TypeScript-typed,
no `any`, Tailwind classes only):
- `<PublicHeader>` — both templates, mobile drawer, announcement bar
  conditional (Editorial only).
- `<PublicFooter>` — both templates, social-icons block, cookie
  preferences modal.
- `<Eyebrow>` — uppercase letter-spaced label with optional accent rule.
- `<Headline>` — clamp()-sized display/h1/h2/h3.
- `<BigNum>` — tabular-nums, four sizes, four tones.
- `<Chip>` — squared category chip with three tones.
- `<Btn>` / `<BtnLink>` — primary/secondary/ghost/inverse/inverseFilled.
- `<ImageSlot>` — `<picture>` with WebP source + JPEG fallback. Renders
  prototype-style `[ HERO PHOTO ]` placeholder when src is null (per Q3
  addendum: hairline-border + mono-caps caption).
- `<PIcon>` — Lucide wrapper at 1.6 stroke + aria-hidden default.
- `usePublicActivePage()` — derives the active-nav token from
  `useLocation()`.

**Legacy chrome shims** (`fef8114`)
- `app/src/components/shared/PublicNavBar.tsx` and `PublicFooter.tsx`
  collapsed to ~7-line shims that read `settings.template` + derive
  `activePage`, then delegate to the new `<PublicHeader>` / `<PublicFooter>`.
- Effect: **all 17 existing public pages picked up the new template-aware
  chrome with zero per-page changes.**
- `app/src/pages/public/PublicLayout.tsx` dropped its explicit
  `<AnnouncementBar />` render (the new header handles it conditionally
  per template).

**Docs**
- `ACCESSIBILITY.md` updated: AAA design-time aspiration + AA save-time
  soft-warning policy.

### Verification on `claude/public-site-pr1-foundation`

- `dotnet build` clean (0 warnings, 0 errors).
- `dotnet test` 311/311 passing (Domain 15, Application 166,
  Infrastructure 81, Api 49).
- `npm run build` clean (700 KB vendor chunk warning is pre-existing).
- `npm test` 26/26 passing.

### Visible behavior in DEV after PR #1

After `dotnet ef database update` and an API restart, the new
template-aware header + footer render on every public page. Toggling
Site Settings → Branding → Template flips the visual treatment
immediately on next request. **Page bodies remain on their legacy
templates** until each screen PR (#2+) rebuilds them.

---

## What's not done

PR #2 onward per Section 9 of the design handoff. The agreed cadence:

| PR | Screen | Status |
|---|---|---|
| #1 | Foundation (tokens, primitives, chrome, seed) | Done. |
| #2 | Home | **Not started.** |
| #3 | About | Not started. New route. |
| #4 | I'm New | Not started. New route. |
| #5 | Sermons list + detail | Not started. Existing pages to migrate. |
| #6 | Events list + detail | Not started. Existing pages to migrate. |
| #7 | News list + detail | Not started. Existing pages to migrate. |
| #8 | Beliefs (What We Believe) | Not started. New route. |
| #9 | Leaders | Not started. Existing page to migrate. |
| #10 | Contact | Not started. New route. |
| #11 | Members landing (`/members/home`) | Not started. New route. |

Per Q6 the Members landing canonical route is `/members/home` (not
`/dashboard`).

The user expressed mid-session interest in extending PR #1 to also rewrite
HomePage + create the missing static pages (About / I'm New / Beliefs /
Contact / Members landing). That request was acknowledged but not yet
implemented — it should land as PR #2 (or a bundled "static-pages" PR)
once the web session picks up.

---

## Out-of-scope items called out during PR #1

These were deferred deliberately and are documented in `29bf839`'s commit
body:

- Self-hosted `.woff2` files themselves. `@font-face` declarations + the
  fallback stack ship; the operator drops the woff2 files into
  `app/public/fonts/` during build. Q1 default.
- Surfacing the contrast-warning string in the admin save response. The
  `ColorContrast` helper exists; the Settings page UI hook for the warning
  lands when the Settings page is next touched.

---

## Hard rules to maintain across PR #2+

- **Tailwind classes only, no inline styles.** Translate prototype
  inline styles into utility classes; use CSS variables directly for color
  so the per-tenant override keeps working.
- **`border-radius: 0` everywhere.** Only the Quiet template's pill filter
  chips (`rounded-full`) are allowed to opt back in.
- **Don't touch admin routes.** Only public-facing pages and shared
  components in scope.
- **TypeScript types on every new component prop. No `any`.**
- **Match content shape across templates.** Same blocks, same order, same
  data; only visual treatment differs.
- **One screen per PR.** Branch from `claude/public-site-pr1-foundation`
  (or from this branch's eventual merge target) per screen, stop for
  review before starting the next.

---

## How a fresh web session picks this up

1. Clone (or fetch) the repo and check out the foundation branch:
   ```sh
   git fetch origin
   git checkout claude/public-site-pr1-foundation
   ```
2. Read `PUBLIC_DESIGN_HANDOFF.md`, `BUILD_PLAN.md`, and this file.
3. Confirm the build is clean locally:
   ```sh
   cd api && dotnet test CredoCms.slnx --nologo
   cd ../app && npm install && npm run build && npm test -- --run
   ```
4. Apply the Phase 6 + Public Site migrations to your local DB:
   ```sh
   cd api && dotnet ef database update --project CredoCms.Infrastructure --startup-project CredoCms.Api
   ```
5. Boot the API + SPA in DEV; verify Editorial chrome renders, then flip
   Site Settings → Branding → Template to Quiet and confirm the chrome
   changes treatment.
6. Branch from `claude/public-site-pr1-foundation` for PR #2 (Home):
   ```sh
   git checkout -b claude/public-site-pr2-home
   ```
7. Build PR #2 per the handoff's Section 7.1. Use the primitives in
   `app/src/components/public/`; both templates rendered from the same
   `HomePage.tsx` source, gated by the template ID read from
   `useSiteSettings()`.

---

## Opening prompt for the web session

Paste the following into the first message in the web session. It mirrors
the original handoff's structure but reflects current state.

```
I've added PUBLIC_DESIGN_HANDOFF.md and WEB_SESSION_HANDOFF.md at the
repo root. Read both in full, then BUILD_PLAN.md, then stop before
writing any code.

We're picking up the Public Site visual refresh. PR #1 (foundation —
tokens, primitives, header, footer, seed) is shipped on
claude/public-site-pr1-foundation (commits 29bf839 + fef8114). Backend
311/311 passing; SPA 26/26 passing; both builds clean. The new
template-aware header + footer render on every public page via the
legacy chrome shims; page bodies are still on legacy templates and need
PRs #2+ to migrate.

Branch from claude/public-site-pr1-foundation for the next screen.
PR #2 = Home (Editorial + Quiet), per Section 7.1 of
PUBLIC_DESIGN_HANDOFF.md. Both templates rendered from the same
HomePage.tsx, gated by the template id from useSiteSettings().

Before writing any code: confirm you understand:
  1. The hard rules in PUBLIC_DESIGN_HANDOFF.md (Tailwind only,
     border-radius: 0, no admin routes, TypeScript types, content shape
     matches across templates).
  2. Per Q4 the --primary token is for buttons/CTAs only — headings use
     --foreground.
  3. Per Q6 the Members landing canonical route is /members/home.
  4. Per Q3 ImageSlot placeholders render as labelled hairline-border
     boxes with [ MONO CAPS CAPTION ] — never a broken-image icon.
  5. Per Q7 scripture quotes accept <blockquote data-scripture cite="...">
     and are styled by static CSS only (TipTap custom block deferred).
  6. Per Q8 sermon player renders the YouTube embed; null video id
     renders the same hairline-border placeholder treatment.
  7. Per Q10 tenant-color contrast is a soft warning (warn, don't block).

Confirm understanding + ask any clarifying questions you have for PR #2
specifically, then proceed. One screen per PR; stop for my review
before moving to the next.
```
