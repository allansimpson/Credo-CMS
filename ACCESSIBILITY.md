# Accessibility

Credo CMS targets **WCAG 2.1 AA** compliance for v1. This document
records the testing surface, baseline scores, known limitations, and
how to report issues.

## Target

- WCAG 2.1 AA across the public site and the admin shell.
- Lighthouse Accessibility score ≥ 95 on representative pages.
- Keyboard-navigable end-to-end (no mouse-only interactions).
- Screen-reader compatible with NVDA (Windows) + VoiceOver (macOS / iOS).

## Phase 6 audit summary

Phase 6 introduced the cross-cutting accessibility pass. Specific
fixes shipped:

- **Skip-to-main-content link** added to both layouts
  (`ChurchThemeLayout` for public, `AdminLayout` for admin). `sr-only`
  by default, visible on keyboard focus.
- **Footer social links** (`PublicFooter`) gained explicit
  `aria-label="{ChurchName} on {Platform}"`, `target="_blank"
  rel="noopener noreferrer"`, `aria-hidden="true"` on the icon.
- **Cookie consent banner** uses `role="dialog"` with
  `aria-labelledby` and `aria-live="polite"`. Buttons have
  focus-visible rings via Tailwind's `focus-visible:ring-2`.
- **Pagefind UI** in the docs site is keyboard-accessible by default
  (Pagefind's UI component manages its own ARIA).
- **Astro docs site** uses semantic HTML throughout: `<nav>` for the
  sidebar, `<main>` for content, proper heading hierarchy. Skip link
  in `BaseLayout.astro`.

## Tested screen readers

| SR | Browser | OS | Status |
|---|---|---|---|
| NVDA | Firefox | Windows 11 | Tested at v1 launch (operator responsibility) |
| VoiceOver | Safari | macOS 14 | Tested at v1 launch (operator responsibility) |
| VoiceOver | Safari | iOS 17 | Tested at v1 launch (operator responsibility) |

Note: Phase 6 ships the framework + automated checks; the manual
screen-reader pass against representative flows (sign in, prayer wall,
sermon detail, broadcast composer) is documented as a required
pre-launch acceptance test in [README.md](./README.md).

## Lighthouse scores

Targets per page:

- Performance: ≥ 85 mobile / ≥ 95 desktop
- Accessibility: ≥ 95
- Best Practices: ≥ 95
- SEO: ≥ 95

Pages audited (homepage, sermon archive, sermon detail, event detail,
blog detail) and the actual scores are captured in
[IMPLEMENTATION_NOTES.md](./IMPLEMENTATION_NOTES.md) under "Phase 6
Performance + accessibility scores" once the pre-launch deployment
dry-run is complete.

## Known limitations and v1.x targets

These are accessibility gaps documented as deferred work — the
underlying components have upstream limitations or require deep
refactors that would risk regressing functional behavior.

### TipTap editor

The TipTap rich-text editor (used in the broadcast composer, page
editor, blog editor, etc.) has well-documented accessibility quirks:

- Format-toolbar buttons announce icon names instead of action verbs
  in some screen readers. Mitigated by `aria-label` overrides where
  practical; full remediation requires upstream changes.
- Keyboard shortcuts (Ctrl+B, Ctrl+I, etc.) are documented in
  `/docs/content-management/pages` but not announced inline.
- Selection state isn't always announced. Screen-reader users may
  prefer the Markdown / plain-text fallback path.

**v1.x target:** evaluate alternatives or contribute upstream fixes.

### FullCalendar

The events calendar view (FullCalendar) has limited keyboard support
out of the box:

- Tab navigation between dates is supported but the visual focus
  indicator is sometimes inconsistent.
- Event cells respond to Enter/Space activation.
- Date-range selection via keyboard is awkward; mouse drag is more
  natural.

**v1.x target:** add custom keyboard handlers + clearer focus
indicators, or migrate to a more accessible calendar library.

### Color contrast in church themes

The site supports administrator-configured `PrimaryColor` and
`AccentColor` (Site Settings → Branding).

**Design-time:** both Public Site templates ship with default palettes
verified for WCAG AAA contrast (7:1 normal text, 4.5:1 large) against
the template background. The AAA target is documented; we do not
enforce it at runtime.

**Runtime:** the per-tenant `PrimaryColor` / `AccentColor` override is
contrast-checked at save time as a **soft warning** — never blocked.
The check runs against the active template's background (Editorial
`#f6f4ef`, Quiet `#fbfaf7`) and surfaces a warning string when the
chosen color drops below AA (4.5:1). Save proceeds regardless. Some
churches have inherited brand colors they must use even when contrast
is borderline; refusing the save would be paternalistic.

The contrast helper (`ColorContrast` in
`CredoCms.Application.SiteSettingsManagement`) is the canonical
WCAG-relative-luminance + contrast-ratio implementation; reuse it
rather than re-implementing per-call-site.

**v1.x target:** inline contrast preview in the Branding tab showing
the live ratio + AA/AAA pass markers as the admin picks colors.

### Profile photo alt text

Member profile photos use the member's display name as alt text
when no manual alt is supplied. For decorative usage (avatar in a
list) this may be redundant; for content usage (e.g., in a leader
detail page) it's appropriate. Document audit deferred to v1.x.

## Reporting issues

If you encounter an accessibility barrier:

1. **Site administrators:** open an issue via your project tracker.
2. **External users:** email the address configured in
   `SiteSettings.ContactEmail`. A site administrator will triage.

Include browser, screen reader (if applicable), the page URL, and a
description of the barrier. We'll prioritize WCAG AA failures as v1.x
hotfixes.

## Automated testing

The SPA test suite includes `vitest-axe` runs against five
representative pages: homepage, sermon archive, event detail, profile
(Personal Info tab), broadcast composer. Run via:

```sh
cd app
npm test
```

axe-core failures fail the build. Adding new public pages should
include an axe-core test in `src/__tests__/a11y/`.
