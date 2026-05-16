# CredoCMS — Public Site Templates · Design Handoff

> **For Claude Code:** This document specifies two starter templates for the CredoCMS public-facing church site. Read it in full, then **stop and ask the user clarifying questions before writing any code.** Capture what you need (font hosting, build-step preferences, multi-tenant behavior, sample content, etc.) and surface those questions back to the user. Treat the companion prototype (`CredoCMS Public Site.html`) as the visual reference — match hierarchy, rhythm, and signature elements, but implement using the project's existing stack (React + TypeScript + Tailwind + the `church` CSS-variable theme). Do not copy inline styles verbatim; translate them into Tailwind classes and tokens that respect the per-tenant primary/accent override mechanism.

---

## 1. Goal & scope

A visual refresh of the **public-facing church site** — what visitors and members see at the church's root domain. The admin shell (`/admin/*`) was specified separately in `DESIGN_HANDOFF.md` and is out of scope for this pass except where the public site must continue to coexist with it (single SPA, shared auth, shared SiteSettings).

The handoff specifies **two templates**, both within the `church` theme, selectable per tenant via SiteSettings (new field). They share the same component library, page layouts, and content shapes — they differ in palette, density, and typographic personality.

**Templates:**

1. **Editorial Warm** (default) — extends the admin Editorial language onto the public site. Cream + ink + warm accent, single sans, big tabular numerals, hairline rule dividers, dark insets for callouts. Feels familiar to staff who use the admin daily.
2. **Quiet Sanctuary** (alt) — pared-back, contemporary, generous whitespace. Single sans, very large display sizes, soft sage accent, almost monochrome neutrals, minimal chrome. Suits congregations who want a more modern feel.

**Screens in scope (10 each × 2 templates):**

1. Home
2. About / Our Story
3. I'm New (visitor landing)
4. What We Believe
5. Sermons — list
6. Sermons — detail
7. Events — list
8. Events — detail
9. News / Blog — list
10. News / Blog — detail
11. Leaders / Staff
12. Members area landing
13. Contact

(13 routes total; "list+detail" pairs counted as one screen above for convenience.)

---

## 2. Voice & tone (apply to all copy across both templates)

**Warm & invitational.** Address the reader directly. Low jargon. Honest about what church is and isn't. Comfortable with silence and short sentences. No marketing-speak, no rhetorical questions, no emoji.

**Examples in the prototype:**
- "We saved you a seat."
- "Whatever you wore on Saturday."
- "There's an offering, but if you're visiting we genuinely don't expect anything from you. Just be here."
- "We're glad to belong to a faith that's older than us."

The prototype uses placeholder copy for Hope Community Church. Treat all copy as **example fixtures** — final tenant copy will come from the church's content editors via the admin.

---

## 3. Design tokens

Both templates extend the existing `church` CSS variables (the per-tenant primary/accent override mechanism in `ChurchThemeLayout.tsx` must keep working). Add `data-template="editorial"` or `data-template="quiet"` to the theme root and let CSS select the right token set.

### Shared (both templates)

- **Square corners.** Keep `--radius: 0`. Preserve the global `border-radius: 0 !important` rule.
- **Per-tenant override.** `--primary` and `--accent` continue to be set at runtime from SiteSettings (HSL channel triples). The values below are the *template defaults* — the church admin can override per tenant.
- **Imagery role.** Type-led. Photos are supporting; never required for a layout to work. Where the prototype shows `ImageSlot` placeholders, build a real image component that accepts a slot from the CMS and gracefully degrades to a labelled box when no image is set.

### Editorial Warm — light mode tokens

| Token | Value | Use |
|---|---|---|
| `--bg` | `#f6f4ef` | Page background |
| `--panel` | `#fbfaf6` | Card / surface |
| `--panel-alt` | `#f0ede5` | Subtle inset wells |
| `--inset` | `#1a1815` | Dark insets, footer, hero overlay |
| `--inset-fg` | `#f5efe2` | Text on `--inset` |
| `--fg` | `#1a1815` | Primary text |
| `--fg-soft` | `#3a352e` | Secondary text |
| `--muted` | `#7a7165` | Metadata, captions |
| `--border` | `#dcd5c5` | Hairline borders |
| `--border-soft` | `#ebe6d8` | Row dividers |
| `--accent` (default) | `#b8531a` | Single accent — buttons, eyebrows, strips |
| `--accent-fg` | `#fbfaf6` | Text on accent |
| `--accent-soft` | `#b8531a22` | Accent tints |
| `--font-heading` | `"Inter Tight", system-ui, sans-serif` | All headings |
| `--font-body` | `"Inter Tight", system-ui, sans-serif` | All body |
| `--font-mono` | `"JetBrains Mono", ui-monospace, monospace` | Slugs, dates, system data |

### Quiet Sanctuary — light mode tokens

| Token | Value | Use |
|---|---|---|
| `--bg` | `#fbfaf7` | Page background |
| `--panel` | `#ffffff` | Card / surface |
| `--panel-alt` | `#f4f2ec` | Inset wells |
| `--inset` | `#26302a` | Dark band (used sparingly) |
| `--inset-fg` | `#e9eae3` | Text on `--inset` |
| `--fg` | `#1f231f` | Primary text |
| `--fg-soft` | `#48504a` | Secondary text |
| `--muted` | `#8b8e84` | Metadata |
| `--border` | `#e3e1d8` | Borders |
| `--border-soft` | `#eeece4` | Subtle dividers |
| `--accent` (default) | `#5b7a5a` | Single accent (sage) |
| `--accent-fg` | `#ffffff` | Text on accent |
| `--accent-soft` | `#5b7a5a18` | Accent tints |
| Fonts | Same as Editorial (Inter Tight + JetBrains Mono) | — |

### Typography rules

- **One sans family per template** — both templates use Inter Tight by default. Load from Google Fonts (`Inter+Tight:wght@400;500;600;700` + `JetBrains+Mono:wght@400;500;600`) or self-host.
- **Tracking** — Editorial: `-0.02em` on headlines. Quiet: `-0.025em` to `-0.035em` (tighter, larger).
- **Headline scale**:

| Use | Editorial | Quiet |
|---|---|---|
| Hero / display | 84–88px | 88–120px |
| Section H1 | 56–72px | 72–104px |
| H2 | 36–48px | 42–64px |
| H3 (cards) | 22–28px | 26–32px |
| Body lead | 18px | 21px |
| Body | 14–17px | 15–18px |
| Eyebrow | 11px, `0.18em` tracking, uppercase | 12px, `0.18em` tracking, uppercase |

### Spacing

- Editorial: section padding `64–88px` vertical, `56px` horizontal.
- Quiet: section padding `96–128px` vertical, `64px` horizontal. (More breathing room.)
- Both: 4px base grid. Card padding 22–28px.

---

## 4. Signature elements

### Editorial Warm
- **Big tabular numerals** for stats, dates, version numbers.
- **Rule-divider headings** with optional numeric prefix (e.g. `01 · Series`).
- **Accent strips** — 2–3px solid bars on left edges of featured items / pull-quotes.
- **Eyebrow + headline + kicker** at the top of every screen.
- **Dark inset blocks** for "This Sunday", "Tend to", footer.
- **Mono for system data** — slugs, timestamps, scripture references, hex.

### Quiet Sanctuary
- **No rules, no insets by default.** Whitespace and tracking do the work.
- **Pill-style filter chips** instead of segmented borders.
- **Large eyebrow above the headline** — simple uppercase letter-spaced label, no rule line.
- **Single rule dividers** between content blocks (`border-top: 1px solid var(--border-soft)`).
- **Centered editorial blockquotes** for scripture/quote moments — large display type, small caps citation.

---

## 5. Shared component inventory

Build once, used by both templates (templates differ only in tokens). Add to `components/shared/` or wherever the public-site components live.

- **`<PublicPage template activePage>`** — top-level wrapper. Mounts `<PublicHeader>`, `<main>`, `<PublicFooter>`.
- **`<PublicHeader template activePage>`** — varies by template (see section 6). Includes the announcement bar.
- **`<AnnouncementBar>`** — already exists; ensure it picks up new theme tokens.
- **`<PublicFooter template>`** — varies by template.
- **`<Eyebrow accent?>`** — uppercase letter-spaced label, with optional leading rule.
- **`<Headline size>`** — `<h1>` styled by template tokens.
- **`<BigNum size>`** — tabular numerals, used for dates and stats.
- **`<Chip tone>`** — small uppercase label, `muted | accent | inverse`.
- **`<Btn variant size icon iconRight>`** — `primary | secondary | ghost | inverse | inverseFilled`.
- **`<ImageSlot ratio label tone>`** — image placeholder that becomes a real `<picture>` when content is supplied.
- **`<PIcon name size color>`** — single icon set, line-style, 1.6px stroke.

---

## 6. Header & footer (per template)

### Editorial Warm
- Announcement bar (dark inset bg, accent eyebrow + message + CTA link).
- Three-column header: logo+wordmark left, horizontal nav center, "Sign in" + accent "Plan a visit" CTA right.
- Footer: 4-column dark inset — logo block + "Visit / Connect / Give & Serve" columns + bottom rule with copyright, privacy/accessibility, and a small "Built on Credo CMS" credit.

### Quiet Sanctuary
- **No announcement bar by default** (optional — admin can enable).
- Single-row header: wordmark "Hope Community<span muted>.church</span>" left, horizontal nav center, "Sign in" + accent "Visit Sunday" right.
- Footer: 3-column on a panel-alt background — pull-quote tagline + address left, "Visit / Connect" columns right. Bottom rule with copyright and policy links.

---

## 7. Per-screen specs

Each screen exists in both templates. The **content shape is identical**; the visual treatment differs. Match the prototype's structure.

### 7.1 Home
**Hero** — Editorial uses photo-led overlay with type pulled bottom-left. Quiet uses split: type left, photo right.
**Blocks (in priority order, ship all):**
1. Hero with service times
2. This Sunday (current sermon + series link)
3. I'm New CTA strip
4. Upcoming events (3–4)
5. Latest news (3 cards)
6. What we believe (short summary + link)
7. Give / Donate CTA

Editorial uses dark `--inset` blocks for "I'm New" and "Give" strips. Quiet uses panel + rule dividers throughout, never dark insets in the body of the page.

### 7.2 About / Our Story
**Editorial:** hero with eyebrow + huge headline + image strip caption + two-column magazine body (with drop cap) + 4-stat numbers row + 5 values grid.
**Quiet:** hero with large headline + body paragraph + full-width image + centered prose body + 4-stat row (no boxes, just numbers).

### 7.3 I'm New
**Both:** hero, "the visit hour by hour" timeline using `BigNum` for times, FAQ grid, sign-up CTA.
**Editorial:** dark inset CTA at the bottom. **Quiet:** centered CTA on a rule.

### 7.4 What We Believe
**Both:** hero, 7-point belief list. **Editorial:** 3-column grid per row (num / title / body). **Quiet:** wider rows, larger title, more spacing. End with centered "we confess with the whole Church" creeds reference.

### 7.5 Sermons — list
**Both:** hero + featured-current sermon panel with play button + filter pills + listing.
**Editorial:** filter pills use dark inset selected state. **Quiet:** filter pills are pill-shaped (the only place radius isn't 0 — borderRadius: 999 on pills is allowed).
**Listing row:** part number (BigNum or mono) + title + scripture + speaker + date + length + play button.

### 7.6 Sermons — detail
**Both:** breadcrumb + sermon header with passage / preacher / date / length stats row + video/audio player + scripture passage card + outline + series navigation grid.

### 7.7 Events — list
**Both:** hero + filter chips + featured event card + month-grouped listing with day-block (day name / BigNum date / month) per row.

### 7.8 Events — detail
**Both:** breadcrumb + title + body content + sticky right-column info card with date/time/location/CTA buttons.

### 7.9 News — list
**Both:** hero + featured post + filter chips + 2-column grid of remaining posts (image + chip + headline + excerpt + byline).

### 7.10 News — detail
**Both:** centered title block with chip + headline + byline strip + full-width hero image + max-width prose body with drop cap + scripture blockquote (accent quote marks) + related posts at bottom.

### 7.11 Leaders / Staff
**Both:** hero + 3-column grid of leader cards (4:5 photo + role eyebrow + name + bio + email link) + elders & deacons strip pointing to full directory.

### 7.12 Members landing
**Both:** personalized "Welcome back, {name}" + 4 quick-action cards (calendar, messages, give, profile) + "Your groups" list + "Giving · year to date" with `BigNum` + annual statement card.

### 7.13 Contact
**Both:** hero + form (name / email / subject / message) + office hours + direct lines (phone, email, address) + map placeholder.

---

## 8. Tweakable per tenant (via existing SiteSettings)

The SiteSettings row already supports primary + accent color override. **Add one new field:**

- `template`: `'editorial'` | `'quiet'` — default `'editorial'`.

Render `<PublicPage template={siteSettings.template}>` so the admin can swap templates from the existing Branding tab in `/admin/settings`. No tenant-facing template picker UI is needed beyond a single radio in admin Settings.

---

## 9. Implementation guidance for Claude Code

### Before writing any code

**Stop and ask the user clarifying questions** about:

1. **Font hosting** — Google Fonts CDN (simplest) or self-host? The site already uses ui-sans-serif in church-theme.css; both templates want Inter Tight. Confirm whether to swap the default or keep ui-sans-serif as a fallback.
2. **Build target** — Is there a route-per-screen file convention to follow? Verify the existing `pages/public/*` structure and whether new pages should follow the same shape.
3. **Image strategy** — How are hero images managed today? Use existing `ImageUpload`/blob storage flow? Is there a `<picture>` helper for the auto-generated WebP variant the homepage hero etc. needs?
4. **Multi-tenant token scoping** — `ChurchThemeLayout.tsx` currently sets `--primary` and `--accent` on the `[data-theme='church']` element via useEffect. Should the new `data-template` attribute live on the same element, and do we need to add `--inset`, `--inset-fg`, `--panel-alt`, `--border-soft` etc. as tenant-overridable, or are those template-fixed?
5. **Sample content** — Is there a SeedData step or fixture file we should populate so a fresh `dotnet ef database update` produces a runnable demo of both templates? If not, should we add one?
6. **Routing for Members landing** — `/members` vs nesting under `/profile`? The prototype calls it "Members landing"; confirm the canonical path.
7. **Scripture quote component** — News detail uses a scripture blockquote. Does the TipTap editor have a custom block we should integrate, or should the static page accept it as plain markup?
8. **Sermon player** — The Sermon detail prototype shows audio + video. Confirm which media types are supported in the current sermon entity, and whether an existing player component exists.
9. **Theme switch UX** — Should switching templates require a republish/cache-bust, or take effect on next request?
10. **Accessibility commitments** — Confirm the AA/AAA baseline. Both templates aim for WCAG AAA contrast in default palette; verify the per-tenant accent override won't drop below AA.

**Once those are answered, proceed in this order:**

1. **Tokens & template selector** — extend `church-theme.css` with `[data-template='editorial']` and `[data-template='quiet']` rule blocks. Wire `siteSettings.template` through `ChurchThemeLayout`.
2. **Shared primitives** — build the component inventory in section 5.
3. **Header + footer** — both templates.
4. **Then screens, in this order:**
   1. Home
   2. About
   3. I'm New
   4. Sermons list + detail
   5. Events list + detail
   6. News list + detail
   7. Beliefs
   8. Leaders
   9. Contact
   10. Members landing

### Rules

- **Tailwind, not inline styles.** Translate prototype inline styles into Tailwind utility classes or `@apply` blocks. Use CSS variables directly for color so the per-tenant override keeps working.
- **Preserve the squared-corners override.** Only pill-shaped filter chips in the Quiet template may use `rounded-full`.
- **One sans family by default.** Don't introduce a serif.
- **Don't touch admin routes.** Only public-facing pages and shared components in scope.
- **TypeScript types** on every new component prop. No `any`.
- **Match content shape across templates** — same blocks, same order, same data; only visual treatment differs.

---

## 10. Out of scope

- Admin shell visuals (covered in `DESIGN_HANDOFF.md`)
- API routes, server code, schema changes (except adding the `template` field to SiteSettings)
- Auth flows
- Per-screen analytics, SEO meta beyond what's already in `SeoTags.tsx`

---

## 11. Reference materials

- **Prototype:** `CredoCMS Public Site.html` — open this for the visual reference. Pan/zoom to compare templates side-by-side.
- **Companion docs:**
  - `DESIGN_HANDOFF.md` — admin shell handoff (same shape as this doc).
  - `church-theme.css`, `ChurchThemeLayout.tsx` — existing per-tenant theme mechanism.
  - `AnnouncementBar.tsx` — existing announcement bar to extend.

---

*End of handoff. Begin by asking clarifying questions; do not write production code until those are answered.*
