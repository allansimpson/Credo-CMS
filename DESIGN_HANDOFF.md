# CredoCMS — Admin UI Refresh · Design Handoff

> **For Claude Code:** This document specifies a visual refresh of the CredoCMS admin shell. Treat it as the canonical source of truth for the work. The companion HTML prototype (`CredoCMS Admin Refresh.html`) is the **visual reference** — match its hierarchy, rhythm, and signature elements, but implement using the project's existing stack (React + TypeScript + Tailwind + the `system` CSS-variable theme). Do not copy inline styles from the prototype into production code; translate them into Tailwind classes and tokens.

---

## 1. Goal & scope

A visual refresh of the **admin shell only** — the interface staff use to edit content. The public church site is **out of scope** for this pass.

Ten screens are in scope:

1. Dashboard (`/admin`)
2. Pages list (`/admin/pages`)
3. Page editor (`/admin/pages/:id`)
4. News list (`/admin/news`)
5. Sermons list + series (`/admin/sermons`)
6. Events list + mini-calendar (`/admin/events`)
7. Users & roles (`/admin/users`)
8. Audit log (`/admin/audit-log`)
9. Site settings (`/admin/settings`)
10. Sign-in (`/login` or equivalent)

The single tech-savvy admin is the primary user. Density and clarity over hand-holding.

---

## 2. Aesthetic direction — "Editorial"

A warm, grounded, magazine-inspired admin. Sans-only typography. Two-tone palette (primary ink + a single warm accent). Squared corners preserved (the global `border-radius: 0 !important` override stays — it is intentional). Type does the heavy lifting; imagery is supporting only.

### Signature moves (use these consistently across all 10 screens)

- **Big tabular numerals** for stats, counts, version numbers, dates. Use `font-variant-numeric: tabular-nums`.
- **Rule dividers with a numeric label** for major sections inside a page (e.g. `01  Identity ─────────`). Hairline 1px borders in the soft border token.
- **Accent strips** — a 2–3px vertical bar on the left edge of cards/rows that need emphasis (featured items, the "you" row in users, warnings).
- **Eyebrow + headline + kicker** pattern at the top of every screen. The eyebrow is uppercase, letter-spaced, muted; the headline is large and tight (`-0.025em` tracking).
- **Monospace for system data** — slugs, file paths, IDs, timestamps in the audit log, color hex values.
- **Pull-quote / dark insets** — the deep ink color used as a background panel for "this Sunday" / "heads up" callouts. Accent strip on the left.

### What we're avoiding

- Rounded corners (system enforces square)
- Gradients of any kind
- Emoji as functional UI
- Decorative SVG illustrations — placeholders are striped, monospaced-labelled
- Multiple accent colors — one accent only
- Drop shadows beyond hairline borders

---

## 3. Design tokens

Map these to the existing `system` theme CSS variables. The current admin theme variables can be renamed/repurposed where the names overlap. Where new tokens are needed, add them following the existing naming.

### Color (light mode)

| Token | Value | Use |
|---|---|---|
| `--bg` | `#f6f4ef` | Page background |
| `--panel` | `#fbfaf6` | Card / table / surface background |
| `--panel-alt` | `#f0ede5` | Table headers, subtle inset rows |
| `--sidebar` | `#1a1815` | Left rail + dark insets |
| `--fg` | `#1a1815` | Primary text |
| `--fg-soft` | `#3a352e` | Secondary text |
| `--muted` | `#7a7165` | Tertiary / metadata text |
| `--border` | `#dcd5c5` | Hairline borders, panel edges |
| `--border-soft` | `#ebe6d8` | Row dividers, internal rules |
| `--accent` | `#b8531a` | Single accent — buttons, links, strips |
| `--accent-fg` | `#fbfaf6` | Foreground on accent |
| `--accent-soft` | `#b8531a22` | Accent tints (calendar today, version chip bg) |
| `--success` | `#4a6741` | Published, active, MFA on |
| `--warn` | `#b07a1a` | Drafts, capacity warnings, no-MFA |
| `--danger` | `#a8311c` | Destructive actions, security failures |

### Color (dark mode)

Dark mode swaps `--bg` to `#16140f`, `--panel` to `#201d18`, `--fg` to `#f5efe2`, etc. The accent stays the same hex; the sidebar becomes `#0f0d0a`. Implement dark via a class on `<html>` or `<body>` (`.theme-dark`) toggling the variable values — don't fork components.

### Typography

- **Stack:** `system-ui, -apple-system, "Segoe UI", sans-serif` for body. Optional: a single grotesque (e.g. **Söhne**, **Söhne Mono** for monospace, or **Inter Tight** + **JetBrains Mono** if free fonts preferred) loaded from a single source.
- **One sans family for everything.** No serif.
- **Heading tracking:** `-0.015em` to `-0.025em` (tighter as size goes up).
- **Body line-height:** 1.55–1.65.
- **Weights used:** 400 (body), 500 (emphasis, labels), 600 (headings, strong meta), 700 (eyebrows, BigNum).

### Type scale

| Style | Size | Weight | Tracking | Use |
|---|---|---|---|---|
| Display | 38–42px | 700 | -0.025em | Login welcome, login screen H1 |
| H1 | 32–36px | 600 | -0.02em | PageHeader title |
| H2 | 19–22px | 600 | -0.015em | Card titles, section headlines |
| H3 | 16–17px | 600 | -0.005em | Table row titles, sermon titles |
| Body | 13.5–15.5px | 400–500 | 0 | Default text |
| Small | 12–12.5px | 500 | 0 | Form labels, table cells |
| Eyebrow | 10–11px | 600 | 0.14–0.18em | UPPERCASE meta labels above titles |
| Mono | 11–12.5px | 500 | 0 | Slugs, IDs, hex, timestamps |
| BigNum | 22–42px | 700 | -0.02em | Stats, counts, version numbers (tabular-nums) |

### Spacing

A 4px base. Most layouts use 14 / 16 / 20 / 24 / 28 / 32 / 36 px gaps. Page padding is `padding: 28px 32px 36px` for the main content region. Card padding is `20–24px`.

### Borders & corners

- Always 1px solid using `--border` or `--border-soft`.
- **No border-radius anywhere.** Keep the global `border-radius: 0 !important` rule.
- Accent strips are 2–3px wide solid bars positioned absolutely on the left edge of containers.

---

## 4. Component inventory

These are the primitives that should land in `components/shared/` (or wherever shared components live in the repo). Prefer creating these as small composable Tailwind components rather than copying the inline-styled prototype.

### New / refined primitives

- **`<Btn variant size icon iconRight>`** — variants: `accent` (filled, accent bg), `secondary` (panel bg + border), `ghost` (text only), `danger` (red text). Sizes: `xs` (24px), `sm` (28px), `md` (32px), `lg` (36px). Square corners.
- **`<Chip tone variant dot>`** — tones: `muted | success | warn | danger | accent`. `variant` toggles between filled-soft and outlined. `dot` adds a 6px colored circle.
- **`<Input prefix suffix>`** — square, hairline border, 32–40px tall, supports leading/trailing icons.
- **`<Avatar name size tone>`** — initials-only. Two letters from name. Tones: `accent | muted | inverse`.
- **`<Switch on>`** — flat rectangle, not a pill. Square corners.
- **`<MetaLabel>`** — uppercase eyebrow with letter-spacing. `font-size: 10–11px`, `font-weight: 600`, `letter-spacing: 0.14em`, `text-transform: uppercase`, `color: var(--muted)`.
- **`<BigNum size>`** — tabular-nums, tight tracking, used for stats and counts.
- **`<SectionHead number title subtitle>`** — section divider with numeric prefix, title, optional subtitle, hairline rule below.
- **`<PageHeader eyebrow title kicker subtitle actions>`** — top-of-page composite. Eyebrow above title, kicker is an inline italic-feel descriptor next to the title, subtitle below, actions on the right.
- **`<Icon name size color>`** — single icon set, line-style, 1.6px stroke. Names used: `dashboard, page, news, mic, calendar, users, doc, megaphone, audit, settings, search, plus, filter, eye, mail, phone, pin, lock, globe, image, history, arrow-right, chevron-down, terminal, play, upload, download, external, church`.

### Layout primitives (used in tables and forms)

- **`<TableHead cols>`** — uppercase eyebrow-style column headers on `--panel-alt` background, hairline bottom border.
- **`<TableRow cols last>`** — grid-based row with explicit column sizes, hairline bottom border (none on last row).
- **`<FilterPills items activeIndex>`** — connected square segments, shared border, accent bg on active.
- **`<Field label required hint>`** — vertical stack of label + control + hint. Label is `12px / weight 500 / muted`.
- **`<Grid cols>`** — simple `repeat(n, 1fr)` form grid with 14px gap.

---

## 5. Per-screen specs

For each screen below: structure, key data, signature moves to apply, copy guidance.

### 5.1 Dashboard (`/admin`)

**Structure (top → bottom):**
1. `PageHeader` — eyebrow shows current date (`Sunday · 5 May 2026`), title `Welcome back, {firstName}.`, subtitle one line of warm context, actions `Quick add` + `Compose page`.
2. **Stat strip** — single horizontal panel with 4 columns separated by 1px vertical rules. Each shows MetaLabel + BigNum (42px) + sub-line. Accent strip on the left edge of cards that have a tone (`accent` for primary, `warn` for stale drafts).
3. **Two-column grid (1.5fr / 1fr):**
   - Left: **Recent activity** card — 5 rows, each `Avatar + "{Person} {verb} {target}" + relative time`. Hairline dividers between rows.
   - Right column stack:
     - **This Sunday** dark inset (sidebar bg, accent strip on left). Pull-quote of upcoming sermon title in heading font.
     - **Tend to** card — 3 rows, each prefixed with a 3px tone bar (warn, accent, danger). One short sentence each.

**Copy tone:** warm but operational. "Three drafts are waiting on you and the site is healthy." not "You have 3 pending items requiring action."

### 5.2 Pages list (`/admin/pages`)

**Structure:**
1. `PageHeader` — eyebrow shows counts (`10 active · 3 drafts · 2 deleted`), title `Pages`, kicker `every static page on the public site`.
2. **Filter row** — `FilterPills` on the left (All / Published / Drafts / Members only / Deleted, each with count), search input on the right.
3. **Table** — columns: Page (3fr) / Status (1.1fr) / Last edited (1.4fr) / Actions (1fr).
   - Page cell: title in heading font + `System` / `Members` chips inline + slug in mono on row 2.
   - Status: `Chip` with dot.
   - Last edited: relative time on top, "by {Name}" muted below.
   - Actions: ghost `View` + secondary `Edit` with arrow icon.

### 5.3 Page editor (`/admin/pages/:id`)

**Structure:**
1. **Editor command bar** (replaces the standard top bar for editor screens) — 64px tall, `--panel` bg, accent vertical bar on left. Shows MetaLabel `Editing · v12 unsaved`, page title, "Unsaved changes" chip, last saved time, then `History`, `Preview`, `Save draft`, `Publish` buttons.
2. **Two-column body:**
   - Left (flex 1, max-width 760px content): MetaLabel `Public page · /new`, large title input (40px display weight, transparent border-less), URL row with globe icon + monospace slug + "edit slug" link, hero image placeholder (300px), then the editor card with toolbar + content + footer (`284 words · 1m 24s read`).
   - Right (320px aside, `--panel` bg): `Publishing` section (status chip, visibility, members-only switch, featured switch), `Schedule` section (BigNum version + last published), `Recent versions` list (mono version chip + time + author, current version flagged `LIVE` in accent), and a destructive `Delete page` button at the bottom.

### 5.4 News list (`/admin/news`)

**Structure:**
1. `PageHeader` — `Compose post` action.
2. **Featured panel** — full-width 2-column grid: image placeholder on left with absolutely-positioned `FEATURED` badge (accent bg, uppercase letterspaced), text on right with date eyebrow, large heading, excerpt, author + Edit button.
3. **The index** — h3 + `FilterPills`. List of remaining posts as rows: `BigNum (02-style padded)` on left + tags chips + heading-font title + excerpt, then author cell, then actions.

### 5.5 Sermons (`/admin/sermons`)

**Structure:**
1. `PageHeader` — `Upload audio` + `New sermon` actions.
2. **Series row** — 4-column grid of series cards. The currently-active series uses dark sidebar bg + accent strip; others are plain panel. Each shows `Series 0X` MetaLabel, series name in heading font, footer rule with sermon count and date range.
3. **Recent messages table** — columns: # (mono, 50px) / Sermon (title + scripture mono + series name in muted) / Preacher (avatar + name) / Date / Length (mono right-aligned) / Actions (`Listen` + `Edit`).

### 5.6 Events (`/admin/events`)

**Structure:** 1fr / 320px split below header.

**Left column:**
- `PageHeader` then SectionHead `01  Up next`.
- Events list — each row is a 4-column grid: `Date block` (88px, day abbr / BigNum date / month abbr, separated by right rule) / Title block (category chip + status chip + heading + when) / Capacity (filled/cap + percent + thin progress bar; bar turns warn at >85%) / Actions (`Roster` + `Edit`). Featured events get an accent strip; "needs volunteers" gets a warn strip.

**Right aside:**
- **Mini-calendar** — month label, weekday letters, 7×5 grid. Today = accent bg + accent-fg text. Days with events = accent-soft bg.
- **Categories** — list of category names with mono padded counts.
- **Heads up** dark inset — accent strip + short sentence + small accent button.

### 5.7 Users & roles (`/admin/users`)

**Structure:**
1. `PageHeader` — `Manage roles` + `Invite member` actions.
2. SectionHead `01  Roles in use`. 4-col grid of role cards (Owner has accent strip), each with role name + BigNum count + permissions one-liner.
3. SectionHead `02  Members`. Table: Person (avatar + name + "You" chip if self + email in mono) / Role (color-coded: Owner = accent) / MFA (Chip on/off) / Last active / Status (Active or Invited) / Manage button.

### 5.8 Audit log (`/admin/audit-log`)

**Structure:** 1fr / 280px split.

**Left:**
- `PageHeader` — `Filter` + `Export CSV` actions.
- **Day-grouped sections** — each starts with a heading rule (`Today · Tuesday, May 5` left, event count right). Within each day, rows are: timestamp (mono, 92px) / actor avatar (or terminal icon for `system`) / `{actor} {verb} {target}` + meta line below / right-aligned `Diff` and conditional `Revert` buttons. Tone strips on the left for accent (content edits), success (publishing), warn (role changes), danger (failed logins).

**Right aside:**
- **By type** card — 5 rows each with a tone strip + label + 3-digit padded mono count.
- **By person** card — avatars + name + count.
- **Retention note** — small inset card explaining 90-day archive policy.

### 5.9 Site settings (`/admin/settings`)

**Structure:**
1. `PageHeader` — title `Site Settings`, no actions in header.
2. **Two-column body:**
   - Left nav (200px): MetaLabel `Sections` + 6 nav items (Branding / Content / Email & Notifications / Integrations / Privacy & Security / Advanced). Active item has 2px accent left border.
   - Right form (max-width 720px, gap-36px stack):
     - SectionHead `01  Identity` — name (required) + tagline.
     - SectionHead `02  Logo & marks` — 124×124 logo preview box + replace/remove buttons + helper text.
     - SectionHead `03  Palette` — Primary + Accent color rows, plus an 8-step tonal preview strip with footer noting contrast ratio.
     - SectionHead `04  Contact` — email / phone / address fields with leading icons.
     - Bottom action row — last-saved muted text on left, `Discard` + `Save changes` on right, hairline rule above.

### 5.10 Sign in (Login)

**Two-column split, no sidebar/topbar.**

- **Left column (1fr, dark sidebar bg):** Logo lockup at top (32×32 accent-bg square + church icon, name + small "Credo Workbench" caption), pull-quote in middle (MetaLabel `This week's pull-quote` in accent + 28px heading-font quote in cream + small attribution line), tiny version + domain row at bottom.
- **Right column (1fr, panel bg, centered max-width 360px):** MetaLabel `Member sign-in`, display H1 `Welcome back.`, helper paragraph, email input, password input with `Forgot password?` link inline, full-width accent submit button, hairline rule + "Need an account? Administrators invite you by email" footer.

---

## 6. Tweaks (runtime configurable)

Three values should be exposed via the existing tweak/settings mechanism (or hardcoded as theme defaults if you'd rather):

- **Primary ink** — swatches: `#1a1815` (default), `#2a1f17`, `#1f2820`, `#1a1f2e`, `#2c1f1a`
- **Accent** — swatches: `#b8531a` (default), `#5e2b1f`, `#4a6741`, `#7a5230`, `#a8311c`, `#c9912b`
- **Dark mode** — boolean toggle, swaps the variable bundle

---

## 7. Implementation guidance for Claude Code

### Order of operations

1. **Tokens first.** Update the `system` theme CSS variables in the global stylesheet to match section 3. This will visually shift everything before any component changes.
2. **Primitives next.** Build/refactor the components in section 4 inside `components/shared/`. Do this once; reuse everywhere.
3. **Then screens, in this order** — each screen is a pull request:
   1. Sign-in (smallest scope, sets the visual tone)
   2. Dashboard
   3. Settings (exercises every primitive)
   4. Pages list
   5. Page editor
   6. News list
   7. Sermons
   8. Events
   9. Users & roles
   10. Audit log

### Rules

- **Reuse existing components where they already match the spec.** Only create new ones where the spec calls for something the codebase doesn't have.
- **Tailwind, not inline styles.** Translate the prototype's inline styles into Tailwind utility classes or `@apply` blocks. Use the CSS variables directly for color so dark mode just works.
- **Preserve the squared-corners override.** Do not remove `border-radius: 0 !important`.
- **Don't touch the public church site theme.** Only the `system` (admin) theme is in scope.
- **Type safety.** Add proper TypeScript types for every new component prop. No `any`.
- **Keep the prototype HTML around** as a reference but don't copy its structure into the React tree wholesale.

### How to verify

After each screen, compare side-by-side with the prototype at the same screen. The spacing, hierarchy, and signature moves should match. Pixel-perfect is not required — proportional fidelity is.

---

## 8. Out of scope (do not touch)

- The public church site, its theme, components, or routes
- Authentication/authorization logic (only the sign-in screen's *visual*)
- Database schemas, API routes, server code
- Any feature work — this is a visual refresh only

---

*End of handoff. Questions or ambiguities should be resolved by referring to the prototype, then asking for clarification.*
