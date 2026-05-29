import { Link } from "react-router-dom";

/**
 * One year header inside the side-rail. Two visual states:
 *
 *  • Expanded — large accent numeral above the month list. In search mode
 *    a "{n} matches" trailing label appears.
 *  • Collapsed — small heading-weight row with the year's total count on the
 *    right. In search mode shows the year's match count in accent.
 *
 * Click behavior:
 *  • Browse: navigates to `/sermons/{year}` — clearing any active search.
 *  • Search: not clickable (search-mode rescoping is informational; clear the
 *    search first to switch years).
 */
export interface SideRailYearProps {
  year: number;
  expanded: boolean;
  /** Total sermons in the year (browse) or match count (search). */
  count: number;
  /** Search-mode only — drives the trailing "{n} matches" label. */
  matchCount?: number;
  mode: "browse" | "search";
}

export function SideRailYear({
  year,
  expanded,
  count,
  matchCount,
  mode,
}: SideRailYearProps) {
  if (expanded) {
    return (
      <div className="mb-3 flex items-baseline justify-between font-heading text-[28px] font-semibold tracking-[-0.025em] text-accent">
        <span>{year}</span>
        {mode === "search" && matchCount !== undefined && matchCount > 0 && (
          <span className="font-mono text-[11px] tracking-[0.06em]">
            {matchCount} {matchCount === 1 ? "match" : "matches"}
          </span>
        )}
      </div>
    );
  }

  const showMatchBadge = mode === "search" && matchCount !== undefined && matchCount > 0;

  // Collapsed: clickable in browse mode; static in search mode.
  const baseClass =
    "flex items-baseline justify-between py-1.5 font-heading text-[17px] font-medium tracking-[-0.01em] text-fg-soft";

  const inner = (
    <>
      <span>{year}</span>
      {showMatchBadge ? (
        <span className="font-mono text-[11px] font-semibold tracking-[0.06em] text-accent tabular-nums">
          {matchCount}
        </span>
      ) : mode === "browse" ? (
        <span className="font-mono text-[11px] text-muted tabular-nums">{count}</span>
      ) : null}
    </>
  );

  if (mode === "browse") {
    return (
      <Link to={`/sermons/${year}`} className={`${baseClass} hover:text-foreground transition-colors`}>
        {inner}
      </Link>
    );
  }

  return <div className={baseClass} aria-disabled="true">{inner}</div>;
}
