# Sermons by Series — Featured + Archive (Claude Code handoff)

## TL;DR

Replace the current plain card grid in **`app/src/pages/public/SermonSeriesListPage.tsx`** (`SermonSeriesListPublicPage`) with the **Featured + Archive** design: an editorial page header, the shared search / view-switch bar, a large "Now preaching" hero for the flagship active series, a compact "Also running" row for the other active series, and a year-grouped two-column index of completed series.

This requires:

1. **No new entities** — the existing `SermonSeries` plus the `Sermon ⇒ series` join already carry everything. We derive counts, the active flag, and the "latest message" from data we already store.
2. A richer **`sermonSeriesApi.listPublicWithStats()`** (or extend the existing `listPublic()`) DTO that adds: `description`, `context`, `scopeLabel`, `sermonCount`, `plannedParts`, and `latestSermon`.
3. A **rewrite** of `SermonSeriesListPage.tsx` plus a handful of new shared components.

The visual design is locked. Use the screenshots in `screenshots/` and the live React reference (`by-series-source.html`) as the source of truth for layout, spacing, type, and color.

> **Ignore the dark "IDEA · 3 · Featured + Archive" strip** near the top of the reference render — that is exploration chrome (`SeriesPageHeader`'s idea callout) and is **not** part of the page. The real page starts with the eyebrow + "Follow the thread." headline.

---

## Resolved design decisions (v2)

Seven implementation questions came back from engineering review. Designer rulings below — these **supersede** any conflicting detail in the component specs further down.

1. **Hero description → render `descriptionJson` as plain text + truncate.** Do **not** add a second description field. Strip the existing TipTap `descriptionJson` to plain text server-side and truncate at ~240–280 chars on a word boundary with an ellipsis. The hero lead is intentionally unformatted (16px `--fg-soft`, line-height 1.6); dropping bold/italic there is deliberate, not a regression. → `description` in the DTO is the **derived** plain-text string, not a new stored column.

2. **`context` → Site Settings JSON array (`SermonContextsJson`), defaulting to the 4 values.** Configurable per church, matching `NewsCategoriesJson` / `EventCategoriesJson`. **Dot colors are assigned by index, not by name:** palette `['--accent','--fg','--muted','--success','--warn','--danger']`, `color = palette[index % palette.length]`. The 4 defaults land on distinct tokens; custom tracks ("Sunday School", "Youth Wednesday") get a stable color automatically. This replaces the hard name→color map in the `<ContextLabel>` spec. (A future optional per-context color override is fine.)

3. **`SeriesViewBar` is NEW — build it as a shared component now.** "Reuse, don't fork" was forward-looking; it doesn't exist yet. Build it shared (not inline) on this page. **Retrofitting it onto `SermonsArchivePage` + `SermonsByBookIndexPage` is OUT of scope here** — file a fast-follow ticket; the bar is the connective tissue across the three browse surfaces and they'll look unfinished until consistent. Tabs still navigate correctly in the meantime.

4. **Hero play overlay → plays the latest sermon; collapse to a single text CTA.** The centered play glyph links to `/sermons/{latestSermon.slug}` (play = playback). **Remove the separate "Latest message" button**; keep one text CTA: **"All {n} in the series" → `/sermons/series/{slug}`.** This supersedes the two-CTA spec in `<NowPreachingHero>`.
   - **Drop the outer left-column / card wrapper `<a>`** — nested anchors are invalid HTML. The hero has exactly **three independent links**: (a) art + play overlay → latest sermon; (b) series title (right column) → series detail; (c) the "All {n} in the series" CTA → series detail.
   - To keep the right column feeling clickable you *may* stretch the title link over the title + scope + description block via a `::after` overlay — but **scope it to exclude the CTA** and raise the CTA above it (`position: relative; z-index`) so both stay clickable. A plain hover-underlined title link is an acceptable simpler choice; correctness over cleverness.
   - (Acceptable fallback if minimizing churn: overlay → series detail, keep both CTAs and the original single wrapper.)

5. **`plannedParts` overflow (`sermonCount >= plannedParts`) → clamp at 100% + open-ended caption.** Fill the bar to 100% and switch the caption to the existing open-ended form **"{n} PARTS · ONGOING"** (reuse the `plannedParts == null` state). No "over plan" copy; never auto-promote `plannedParts` (that silently edits admin data). Updates the `<ActiveProgress>` spec.

6. **Empty archive (zero completed series) → render the section with a quiet empty state; do not hide.** Same philosophy as the "no active series" slot. Copy: *"No completed series yet — they'll be archived here once a current series wraps."* in the muted inset treatment. Adds a case to the edge-case table.

7. **`scopeLabel` (when null) → derive from the SERIES-level `scriptureReferences`**, not aggregated sermon-level refs. It's what the editor saved — authoritative and predictable. Format as a compact range ("Luke 14–15", "Hebrews"). Updates the `scopeLabel` row in the data-model table.

**Unblocked to build in parallel now** (none of the above touch them): the schema additions, `<SeriesArchive>`, `<ArchiveYearGroup>`, `<ArchiveIndexRow>`, `<CoverageBar>`, and the index/grouping logic. The open calls only affected the hero, the context palette, and the view bar.

---

## Visual reference

| File | What it shows |
|---|---|
| `screenshots/01-hero.png` | Top of the page — "SERMONS · BY SERIES" eyebrow, "Follow the thread." headline, the consistent search + `[Latest] [By Series*] [By Book]` bar, the "NOW PREACHING" eyebrow, and the hero card (series art + accent chip on the left, series title on the right). |
| `screenshots/02-archive.png` | The completed-series index beneath the hero — two columns grouped by year (2023, 2022 …), each series a row with scope + start month and a mono accent part-count. |
| `by-series-source.html` | **Open this in a browser at ~1280px.** Live-rendering React reference with the real layout. Single source of truth for spacing and type. |

States not fully shown in the screenshots:

- **Hero right column** (cropped in 01): context label, 48px series title, mono `{scope} · {speaker}`, a description paragraph, the progress bar + `PART 4 OF ~6 · 67%` line, and two CTAs — `Latest message` (primary, play icon) and `All 4 so far` (secondary, arrow).
- **"Also running"** band (between hero and archive): a `var(--panel-alt)` section with an "Also running" eyebrow and a two-column grid of the *other* active series as compact rows (play badge · context + title + scope/progress · arrow).

---

## Page structure

```
┌─────────────────────────────────────────────────────────────────┐
│ <SeriesPageHeader>                                               │
│   eyebrow "SERMONS · BY SERIES" · headline "Follow the thread."  │
│   meta: "{n} SERIES · {m} MESSAGES · {k} RUNNING NOW"            │
├─────────────────────────────────────────────────────────────────┤
│ <SeriesViewBar>   (shared with the archive / by-book pages)      │
│   search input · [Latest] [By Series*] [By Book]                 │
├─────────────────────────────────────────────────────────────────┤
│ <NowPreachingHero>            eyebrow "Now preaching" / "LATEST…" │
│   ┌───────────────────────┬─────────────────────────────────┐    │
│   │ series art            │ context · TITLE · scope·speaker │    │
│   │ + context chip        │ description                     │    │
│   │ + play overlay        │ progress bar · PART 4 OF ~6     │    │
│   │                       │ [Latest message] [All 4 so far] │    │
│   └───────────────────────┴─────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────────┤
│ <AlsoRunning>   (panel-alt band — only if >1 active series)      │
│   eyebrow "Also running"                                         │
│   ┌────────────────────────┬────────────────────────┐            │
│   │ ▶ Through Hebrews       │ ▶ Praying the Psalms   │            │
│   │   Hebrews · part 12/13  │   Psalms · 13 parts    │            │
│   └────────────────────────┴────────────────────────┘            │
├─────────────────────────────────────────────────────────────────┤
│ <SeriesArchive>           "The archive"  ·  {n} COMPLETED SERIES │
│   ┌──────────────────────┬──────────────────────┐                │
│   │ 2023 · 5             │ 2021 · 1             │                │
│   │   Sermon on Mount  9 │   The Servant King 16│                │
│   │   The Light Has…   4 │                      │                │
│   │   …                  │                      │                │
│   │ 2022 · 3             │                      │                │
│   │   First Things    11 │                      │                │
│   └──────────────────────┴──────────────────────┘                │
└─────────────────────────────────────────────────────────────────┘
```

---

## Component breakdown

All measurements come straight from `sermon-byseries-ideas.jsx` → `FeaturedArchiveDirection` and its helpers. Port the structure to TS + Tailwind; don't import the JSX.

### `<SeriesPageHeader>`

```tsx
<section style={{ padding: '52px 56px 28px', borderBottom: '1px solid var(--border)' }}>
  <div style={{ maxWidth: 1180, margin: '0 auto', display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', flexWrap: 'wrap', gap: 16 }}>
    <div>
      <Eyebrow accent>Sermons · By Series</Eyebrow>
      <Headline size={64} style={{ marginTop: 14 }}>Follow the thread.</Headline>
    </div>
    <span className="font-mono text-[11.5px] text-muted tracking-[0.08em]">
      {totalSeries} SERIES · {totalMessages} MESSAGES · {activeCount} RUNNING NOW
    </span>
  </div>
</section>
```

`Eyebrow` and `Headline` are the existing public primitives (see `public-tokens.jsx`). `totalMessages` = sum of `sermonCount` across all series.

### `<SeriesViewBar>`

The **same** search + tab bar used by the regular archive and the by-book index — reuse it, don't fork. Search input on the left (placeholder `Search series — 'Hebrews', 'Advent', 'Psalms'`), three tab buttons on the right with **"By Series" active** (active = `var(--inset)` background, `var(--inset-fg)` text). Wrapper: `padding: 24px 56px; background: var(--panel-alt); border-bottom: 1px solid var(--border)`.

- `Latest` → `/sermons`
- `By Series` → `/sermons/series` (current — active state)
- `By Book` → `/sermons/by-book`
- Submitting the search navigates to `/sermons?q={query}` (the browse pages are not search surfaces).

### `<NowPreachingHero>`

**Purpose**: showcase the flagship active series. One per page. Pick the active series with the most-recent sermon.

**Props** (derive from the series DTO below):
```ts
interface NowPreachingHeroProps {
  series: PublicSermonSeriesWithStats; // status === "active"
}
```

**Layout**:
- Wrapper `<section>` `padding: 48px 56px; border-bottom: 1px solid var(--border)`. Inner `max-w-[1180px]` centered.
- **Eyebrow row**: flex baseline space-between. Left `<Eyebrow accent>Now preaching</Eyebrow>`. Right mono 11.5px muted `0.12em`: `LATEST · {latestSermon.dateLabel}`.
- **Card grid** `grid-template-columns: 1.3fr 1fr; gap: 40px`.
  - **Left** `<a href="/sermons/series/{slug}">`, `position: relative; background: var(--inset)`. Holds the banner (16/9) — render the real `<picture>`/`<img>` when `bannerImageUrl` exists, else the `aspect-video bg-panel-alt` placeholder. Overlay a `<Chip tone="accent">` with the **context** at `left:18px; top:18px`, and a centered 60×60 play button (`var(--accent)` bg, `var(--accent-fg)` icon).
  - **Right** flex column, justify center:
    - `<ContextLabel context={series.context} />`
    - `<Headline size={48}>{series.title}</Headline>`
    - mono 12.5px accent `0.04em`, marginTop 10: `{scopeLabel} · {speakerLabel}`
    - description paragraph: 16px `var(--fg-soft)`, line-height 1.6, marginTop 16 (hide if empty)
    - `<ActiveProgress series={series} />` in a `max-w-[380px]` wrapper, marginTop 20
    - CTAs marginTop 22, gap 12: primary `Latest message` (play icon) → `/sermons/{latestSermon.slug}`; secondary `All {sermonCount} so far` (arrow) → `/sermons/series/{slug}`.

### `<ActiveProgress>`

```
known plannedParts:   ┌──────────────┐  ← CoverageBar covered=sermonCount total=plannedParts (accent fill, 8px)
                      PART 4 OF ~6 · 67%
unknown plannedParts: ████████████████  ← solid 85%-opacity accent bar
                      13 PARTS · ONGOING
```

Caption: mono 11px muted, `0.04em`, marginTop 8. `pct = round(sermonCount / plannedParts * 100)`. `CoverageBar` is a 6–8px track (`var(--border-soft)`) with an accent-filled inner div at `covered/total %` — lift the same component used on the by-book hero.

### `<AlsoRunning>`

Render **only when there are ≥2 active series**. Wrapper `padding: 36px 56px; background: var(--panel-alt); border-bottom: 1px solid var(--border)`. `<Eyebrow>Also running</Eyebrow>`, then a `grid-template-columns: repeat(2, 1fr); gap: 24px` of the non-hero active series. Each is an `<a href="/sermons/series/{slug}">` card: `grid auto 1fr auto`, `background: var(--panel); border: 1px solid var(--border); padding: 18px 20px`. Left: 42px play badge (`var(--accent-soft)` bg, accent icon). Middle: `<ContextLabel size={10}>`, title 20px heading, mono 11px muted `{scopeLabel} · {plannedParts ? "part N of ~M" : "N parts"}`. Right: arrow icon.

### `<SeriesArchive>` + `<ArchiveYearGroup>` + `<ArchiveIndexRow>`

Wrapper `padding: 44px 56px 72px`. Header row: `<Headline size={30}>The archive</Headline>` + mono `{completedCount} COMPLETED SERIES`. Body `grid-template-columns: 1fr 1fr; gap: 64px`.

**Column balancing**: group completed series by **start year** (newest year first). Split the year-groups across the two columns with `colA = years.slice(0, ceil(years.length/2))`, `colB = years.slice(ceil(years.length/2))`. (Keep whole year-groups intact — don't split a year across columns.)

**`<ArchiveYearGroup>`**: mono 11px header `{year} · {items.length}` (count at 55% opacity), `border-bottom: 1px solid var(--border)`, padding-bottom 10, then the rows. marginBottom 30.

**`<ArchiveIndexRow>`** — an `<a href="/sermons/series/{slug}">`:
- `grid-template-columns: 1fr auto; align-items: baseline; gap: 16px; padding: 13px 0; border-bottom: 1px solid var(--border-soft)`
- Left: title 18px `font-heading` weight 500; below it mono 10.5px muted `{scopeLabel} · {startMonthLabel}` (e.g. `Genesis 1–11 · Sep 2022`).
- Right: mono 13px accent weight 600 `tabular-nums` — the `sermonCount`.

### `<ContextLabel>`

Mono caps label with a leading 8px color dot keyed to the teaching track:

| context | dot color |
|---|---|
| `AM Worship` | `var(--fg)` |
| `AM Bible Class` | `var(--muted)` |
| `PM Worship` | `var(--accent)` |
| `Wednesday Night` | `var(--accent)` |

---

## Data model — what exists, what to add

### Already on `PublicSermonSeries` (no change)

`id`, `slug`, `title`, `bannerImageUrl`, `bannerImageWebpUrl`, `bannerImageAlt`, `startDate`, `endDate`. The active flag is **`endDate == null`**.

### Add: `PublicSermonSeriesWithStats`

Extend the DTO returned to the list page (additive — existing consumers keep working):

```ts
export type SeriesContext =
  | "AM Worship" | "AM Bible Class" | "PM Worship" | "Wednesday Night";

export interface PublicSermonSeriesWithStats extends PublicSermonSeries {
  description: string;            // 1–2 sentences; "" if none
  context: SeriesContext;         // which teaching track the series ran in
  scopeLabel: string;             // "Hebrews", "Luke 14–15", "Selected Psalms", "Various"
  sermonCount: number;            // messages published in this series
  plannedParts: number | null;    // expected length for active series; null = open-ended/complete
  latestSermon: {
    slug: string;
    title: string;
    publishedAt: string;          // ISO
    dateLabel: string;            // "Oct 06" — pre-formatted for the hero eyebrow
  } | null;
  status: "active" | "complete";  // server-computed convenience = endDate == null ? active : complete
}
```

Where each field comes from:

| Field | Source |
|---|---|
| `description` | New nullable column on `SermonSeries` (admin-editable). Falls back to `""`. |
| `context` | New column on `SermonSeries` (enum). Defaults to `"AM Worship"` for legacy rows; admin can set it. |
| `scopeLabel` | New nullable column (free text, e.g. `Hebrews`, `Luke 14–15`). If null, derive from the most-referenced book across the series' sermons; if still ambiguous, `"Various"`. |
| `sermonCount` | `COUNT(*)` of sermons where `sermonSeriesId == series.id`. |
| `plannedParts` | New nullable column. Only meaningful for active series; drives the `PART n OF ~m` progress. |
| `latestSermon` | The series' most-recently-published sermon (or `null` if none yet). `dateLabel` = `MMM dd`. |
| `status` | `endDate == null ? "active" : "complete"`. |

> **Minimal-DB variant**: if you'd rather not add `context` / `scopeLabel` / `plannedParts` / `description` columns yet, default them server-side (`context: "AM Worship"`, `scopeLabel` derived, `plannedParts: null`, `description: ""`). The page degrades gracefully — see edge cases.

---

## API changes

Extend `sermonSeriesApi` in `@/lib/api/sermonSeries`:

```ts
// Either replace listPublic()'s return type with the richer DTO, or add:
listPublicWithStats(): Promise<PublicSermonSeriesWithStats[]>;
```

- Returns **all** published series, both active and complete, ordered by `startDate` desc.
- The page splits the list client-side: `active = items.filter(s => s.status === "active")`, sorted by `latestSermon.publishedAt` desc (hero = first; rest → "Also running"); `complete = items.filter(s => s.status === "complete")`, grouped by start year.
- Public + slow-changing — cache server-side with a 60s TTL, bust on sermon/series publish.

---

## Files to create / modify

```
NEW
  app/src/components/sermons/NowPreachingHero.tsx
  app/src/components/sermons/AlsoRunning.tsx
  app/src/components/sermons/SeriesArchive.tsx        (+ ArchiveYearGroup, ArchiveIndexRow)
  app/src/components/sermons/ActiveProgress.tsx
  app/src/components/sermons/CoverageBar.tsx          (shared; lift from the by-book hero if not already extracted)
  app/src/components/sermons/ContextLabel.tsx
  app/src/components/sermons/SeriesViewBar.tsx        (only if not already shared from the archive/by-book work)

MODIFY
  app/src/pages/public/SermonSeriesListPage.tsx       (full rewrite of SermonSeriesListPublicPage)
  app/src/lib/api/sermonSeries.ts                     (add PublicSermonSeriesWithStats + listPublicWithStats)
  (DB / admin) SermonSeries                           (add description, context, scopeLabel, plannedParts columns + editor fields)

NO CHANGE NEEDED
  app/src/pages/public/SermonSeriesDetailPage.tsx     (series detail — separate ticket if it gets a refresh)
  app/src/pages/public/SermonsByBookIndexPage.tsx     (already shipped from the by-book handoff)
```

Keep `SeoTags` + `useSiteSettings` exactly as the current page uses them (title `Sermon Series · {churchName}`).

---

## Tailwind / token usage

All colors via existing CSS custom properties / token classes. **No new hex codes.**

| Var / class | Use |
|---|---|
| `var(--bg)` | page background |
| `var(--panel)` / `bg-card` | hero card art surface, "also running" cards |
| `var(--panel-alt)` / `bg-panel-alt` | view bar + "also running" band + image placeholder |
| `var(--fg)` | primary text, `AM Worship` dot |
| `var(--fg-soft)` | description paragraphs |
| `var(--muted)` / `text-muted` | mono captions, eyebrows, `Bible Class` dot |
| `var(--border)` | section dividers, year-group rule |
| `var(--border-soft)` | row dividers, progress track |
| `var(--accent)` | counts, chips, progress fill, play buttons, `PM/Wed` dot |
| `var(--accent-soft)` | play-badge background |
| `var(--accent-fg)` | text/icon on accent fills |
| `var(--inset)` / `var(--inset-fg)` | active tab, art background |
| `var(--font-heading)` | titles & headlines |
| `var(--font-mono)` | counts, eyebrows, meta strips, scope labels |

---

## Edge cases

| Case | Behavior |
|---|---|
| Series has no banner image | Left half = `aspect-video w-full bg-panel-alt` placeholder (match current page). Chip + play overlay still render. |
| Series has no `description` | Hide the paragraph entirely. No filler. |
| `plannedParts == null` on an active series | Progress shows a solid accent bar + `{n} PARTS · ONGOING` (no percentage). |
| Exactly **one** active series | Render the hero only; **omit** the "Also running" band entirely. |
| **No** active series (all have `endDate`) | Replace the hero with a quiet "Between series" inset block in the same slot (eyebrow "Between series", 28px headline "We're between teaching series right now.", muted body, secondary CTA → `/sermons`). Keep the slot — don't collapse the page. |
| `sermonCount === 1` | Secondary CTA reads "View the series" rather than "All 1 so far". |
| Series spans multiple start/end years | Group by **start** year in the archive. `scopeLabel` still describes the focus (e.g. `Genesis 1–11`). |
| Two series tie on most-recent sermon | Stable-sort by `startDate` desc as the tiebreaker for which becomes the hero. |
| Very long completed archive | Year-grouping + two columns absorbs it; no pagination needed in v1. |
| `< 768px` viewport | Hero stacks (art on top, content below); "also running" → single column; archive → single column. |

---

## Acceptance criteria

- [ ] Page header reads `{n} SERIES · {m} MESSAGES · {k} RUNNING NOW`, headline "Follow the thread.", eyebrow "SERMONS · BY SERIES".
- [ ] The shared search + `[Latest] [By Series*] [By Book]` bar renders, with **By Series** in the active (inset) state, and is visually identical to the bar on the archive / by-book pages.
- [ ] Hero shows the flagship active series: art + context chip + centered play overlay on the left; context label, 48px title, scope·speaker, description, progress bar with `PART n OF ~m · p%`, and two CTAs on the right.
- [ ] Hero card links to `/sermons/series/{slug}`; "Latest message" links to the latest sermon's detail page.
- [ ] "Also running" band appears only when ≥2 active series; each card links to its series and shows context, title, and scope/progress.
- [ ] Completed series render as a year-grouped two-column index, newest year first, whole year-groups never split across columns.
- [ ] Each archive row shows title, `{scope} · {startMonth}`, and a mono accent part-count; links to the series.
- [ ] Active flag is derived from `endDate == null`; `status` is correct for every series.
- [ ] Progress percentage = `round(sermonCount / plannedParts * 100)`; the bar fill matches.
- [ ] "Between series" empty state renders when no series is active; the page does not collapse.
- [ ] All colors/fonts come from existing tokens — no new hex codes.
- [ ] `listPublicWithStats` is cached server-side and busted on publish.
- [ ] Responsive: hero stacks and both grids collapse to one column below 768px.
- [ ] Keyboard order: hero card → Latest message → All in series → search → tabs → also-running cards → archive rows.

---

## Out of scope (follow-ups)

- Series-detail page (`SermonSeriesDetailPage.tsx`) refresh — separate ticket.
- Admin UI for the new `context` / `scopeLabel` / `plannedParts` / `description` fields — needed to fully drive this, but can ship with sensible server defaults first.
- The other four explored directions (Series Shelf, The Index, The Timeline, By Track) — see `sermon-byseries-ideas.jsx` if priorities change.

---

## Live design reference

Open `by-series-source.html` in a browser at ~1280px to see the design with the real React code. Inspect any element for exact spacing/type. The React source is in `sermon-byseries-ideas.jsx`; the chosen component is **`FeaturedArchiveDirection`** plus its helpers (`NowPreachingHero` logic, `ArchiveIndexRow`, `ActiveProgress`, `SeriesCoverageBar`, `ContextLabel`, `SeriesFilterBar`, `SeriesPageHeader`, `seriesByYear`). Treat it as a reference implementation — port the structure to TS + Tailwind, don't import it. Remember the dark "IDEA" strip in the render is exploration chrome and is not part of the page.
