import type { YearStats } from "@/lib/api/publicSermons";
import { MONTH_SLUGS, MONTH_NAMES } from "@/lib/months";
import { SideRailYear } from "./SideRailYear";
import { SideRailMonth } from "./SideRailMonth";

/**
 * Date-archive side-rail for the public Sermons page. Renders one row per
 * year (descending), with the currently-browsed year expanded inline as a
 * 12-month accordion.
 *
 * Two modes:
 *
 *  • `browse` — year-as-route navigation. `years` is the unfiltered
 *    `/api/public/sermons/years` payload; `currentYear` controls which year
 *    is expanded; `activeMonth` drives the dot.
 *
 *  • `search` — rescoped per the active query/tag. `yearStats` (from the
 *    `by-day` response when a filter is active) overlays match counts onto
 *    the comprehensive year list; every year with `matchCount > 0` is auto-
 *    expanded showing all 12 months — matched ones in accent, non-matched
 *    ones at 35% opacity. Years with no matches stay collapsed with no count.
 */
export interface SideRailProps {
  mode: "browse" | "search";

  /** Comprehensive list of years from `/api/public/sermons/years`. Always
   * present — drives the year list in both modes. */
  years: YearStats[];

  // Browse-mode props
  currentYear?: number;
  activeMonth?: string | null;

  // Search-mode props
  /** Per-year match data from `by-day?search=…`. */
  yearStats?: YearStats[];
}

export function SideRail({
  mode,
  years,
  currentYear,
  activeMonth,
  yearStats,
}: SideRailProps) {
  // In search mode, build a year→YearStats lookup so we can render every
  // year row with its rescoped count. Years not in `yearStats` are
  // collapsed and countless.
  const searchByYear = new Map<number, YearStats>();
  if (mode === "search" && yearStats) {
    for (const ys of yearStats) searchByYear.set(ys.year, ys);
  }

  return (
    <aside className="sticky top-[calc(var(--public-header-offset,0px)_+_var(--sermons-filter-offset,0px)_+_1.5rem)] self-start">
      {/* Header row */}
      <div className="mb-4 border-b border-border pb-3 font-mono text-[11px] font-semibold uppercase tracking-[0.18em] text-muted">
        <span>{mode === "search" ? "Search results" : "Archive index"}</span>
      </div>

      {/* Year list */}
      {years.map((y, i) => {
        const isCurrent = mode === "browse" && currentYear === y.year;
        const searchYear = searchByYear.get(y.year);
        const isExpanded =
          mode === "browse" ? isCurrent : Boolean(searchYear && searchYear.count > 0);

        const showSeparator = i > 0 && (isExpanded || (mode === "browse" && currentYear !== y.year));

        return (
          <div
            key={y.year}
            className={
              showSeparator
                ? "mt-5 border-t border-dashed border-border pt-5"
                : "mt-0"
            }
          >
            <SideRailYear
              year={y.year}
              expanded={isExpanded}
              count={mode === "browse" ? y.count : (searchYear?.count ?? 0)}
              matchCount={mode === "search" ? (searchYear?.count ?? 0) : undefined}
              mode={mode}
            />

            {isExpanded && (
              <div>
                {MONTH_SLUGS.map((slug, mi) => {
                  // Per-month count source depends on mode.
                  const browseCount = y.monthCounts[slug];
                  const matchCount = searchYear?.monthCounts?.[slug];

                  if (mode === "browse") {
                    // Only render months that actually have sermons in browse mode.
                    if (browseCount === undefined || browseCount === 0) return null;
                    return (
                      <SideRailMonth
                        key={slug}
                        monthSlug={slug}
                        monthName={MONTH_NAMES[mi]}
                        count={browseCount}
                        active={activeMonth === slug}
                        mode="browse"
                        year={y.year}
                      />
                    );
                  }

                  // search mode — render all 12 months; mute zero-match ones.
                  const has = matchCount !== undefined && matchCount > 0;
                  return (
                    <SideRailMonth
                      key={slug}
                      monthSlug={slug}
                      monthName={MONTH_NAMES[mi]}
                      count={has ? matchCount : null}
                      active={false}
                      isMatch={has}
                      muted={!has}
                      mode="search"
                    />
                  );
                })}
              </div>
            )}
          </div>
        );
      })}
    </aside>
  );
}
