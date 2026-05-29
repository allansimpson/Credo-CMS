import { Link } from "react-router-dom";

/**
 * One month row inside the side-rail. Has three visual states:
 *
 *  • Active — accent dot on the left, foreground text. Indicates the month
 *    the viewer is currently scrolled to.
 *  • Match (search/tag mode) — count rendered in accent + bold mono.
 *  • Muted (search/tag mode, zero matches) — visible but at 35% opacity, so
 *    the archive's overall shape is still readable.
 *
 * Click behavior:
 *  • Browse mode: smooth-scrolls to `/sermons/{year}#{slug}`. The page's
 *    hash listener handles the scroll; we render an <a href> so middle-click
 *    / open-in-new-tab still work.
 *  • Search mode: disabled — month-click is a v2 enhancement (scroll-to-match
 *    within the results column). Renders as a non-interactive span.
 */
export interface SideRailMonthProps {
  monthSlug: string;
  monthName: string;
  count: number | null;
  active: boolean;
  isMatch?: boolean;
  muted?: boolean;
  mode: "browse" | "search";
  year?: number;
}

export function SideRailMonth({
  monthSlug,
  monthName,
  count,
  active,
  isMatch = false,
  muted = false,
  mode,
  year,
}: SideRailMonthProps) {
  const interactive = mode === "browse" && year !== undefined;
  const baseClass = [
    "relative grid grid-cols-[1fr_auto] items-baseline gap-2 py-1.5 transition-colors",
    muted ? "opacity-[0.35]" : "",
    active ? "pl-3.5 text-foreground font-semibold" : "pl-0 text-fg-soft",
    interactive ? "hover:text-foreground cursor-pointer" : "cursor-default",
  ].filter(Boolean).join(" ");

  const inner = (
    <>
      {active && (
        <span
          aria-hidden="true"
          className="absolute left-0 top-1/2 -translate-y-1/2 h-1.5 w-1.5 rounded-full bg-accent"
        />
      )}
      <span className="text-sm">{monthName}</span>
      {count !== null && (
        <span
          className={
            isMatch
              ? "font-mono text-[11px] font-semibold text-accent tabular-nums"
              : "font-mono text-[11px] text-muted tabular-nums"
          }
        >
          {count}
        </span>
      )}
    </>
  );

  if (interactive) {
    return (
      <Link
        to={`/sermons/${year}#${monthSlug}`}
        className={baseClass}
        aria-current={active ? "true" : undefined}
      >
        {inner}
      </Link>
    );
  }

  return (
    <div
      className={baseClass}
      aria-current={active ? "true" : undefined}
      aria-disabled={mode === "search" ? "true" : undefined}
    >
      {inner}
    </div>
  );
}
