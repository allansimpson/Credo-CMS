import { forwardRef, useEffect, useRef, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Search, X } from "lucide-react";

export type SeriesViewBarActive = "latest" | "by-series" | "by-book";

export interface SeriesViewBarProps {
  active: SeriesViewBarActive;
  /** Placeholder text for the search input. Each tab keeps its own
   * copy — "Search sermons, passages, speakers" on Latest, the series-
   * flavored prompt on By Series, the book hint on By Book. */
  placeholder?: string;

  // -- Controlled mode --------------------------------------------------
  // Provide `value` (and the handlers) to take ownership of the input.
  // Latest uses this so the bar reflects the URL-applied query and the
  // clear button wires into the page's `clearSearch` flow. By Series
  // and By Book leave these undefined and the bar manages its own
  // state, navigating to `/sermons?q=…` on submit.
  value?: string;
  onChange?: (value: string) => void;
  onSubmit?: (value: string) => void;
  onClear?: () => void;
  /** When true (and `onClear` is set), renders the clear button next to
   * the search field. */
  hasAppliedQuery?: boolean;
}

/**
 * Shared search + tab strip across the three sermon browse surfaces
 * (Latest / By Series / By Book). The bar pins below the public nav
 * via `position: sticky` and a 1px sentinel rendered just above it —
 * an IntersectionObserver watches the sentinel and toggles a soft
 * drop-shadow when the bar is "stuck", so it doesn't bleed into the
 * content beneath. Matches the original Latest-tab behavior so the
 * three tabs feel like one continuous surface.
 */
export const SeriesViewBar = forwardRef<HTMLDivElement, SeriesViewBarProps>(function SeriesViewBar({
  active,
  placeholder = "Search series — 'Hebrews', 'Advent', 'Psalms'",
  value,
  onChange,
  onSubmit,
  onClear,
  hasAppliedQuery = false,
}, ref) {
  const isControlled = value !== undefined;
  const [internalValue, setInternalValue] = useState("");
  const currentValue = isControlled ? value! : internalValue;
  const navigate = useNavigate();

  const sentinelRef = useRef<HTMLDivElement>(null);
  const [stuck, setStuck] = useState(false);

  // Stuck-state detection: a 1px sentinel above the bar; when it leaves
  // the viewport (or scrolls under the public header offset) we flip
  // the shadow on. The observer is rebuilt when the public nav height
  // changes so the threshold stays accurate after a resize.
  useEffect(() => {
    const sentinel = sentinelRef.current;
    if (!sentinel) return;
    const root = document.documentElement;
    const computeMargin = () => {
      const headerH = parseFloat(getComputedStyle(root).getPropertyValue("--public-header-offset")) || 0;
      return `-${headerH}px 0px 0px 0px`;
    };
    let observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) setStuck(!entry.isIntersecting);
      },
      { rootMargin: computeMargin(), threshold: [0, 1] },
    );
    observer.observe(sentinel);
    const headerEl = document.querySelector<HTMLElement>("header.sticky");
    const ro = headerEl
      ? new ResizeObserver(() => {
          observer.disconnect();
          observer = new IntersectionObserver(
            (entries) => {
              for (const entry of entries) setStuck(!entry.isIntersecting);
            },
            { rootMargin: computeMargin(), threshold: [0, 1] },
          );
          observer.observe(sentinel);
        })
      : null;
    if (headerEl && ro) ro.observe(headerEl);
    return () => {
      observer.disconnect();
      ro?.disconnect();
    };
  }, []);

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = currentValue.trim();
    if (onSubmit) {
      onSubmit(trimmed);
      return;
    }
    // Uncontrolled default: browse pages route to the search surface.
    navigate(trimmed.length > 0 ? `/sermons?q=${encodeURIComponent(trimmed)}` : "/sermons");
  };

  const handleChange = (next: string) => {
    if (isControlled) onChange?.(next);
    else setInternalValue(next);
  };

  const showClear = !!onClear && hasAppliedQuery;

  return (
    <>
      <div ref={sentinelRef} aria-hidden="true" className="h-px w-full" />
      <div
        ref={ref}
        className={[
          "sticky top-[var(--public-header-offset,0px)] z-30 border-y border-border-soft bg-panel-alt",
          "transition-shadow duration-[var(--motion-md)] [transition-timing-function:var(--motion-ease)]",
          stuck
            ? "shadow-[0_1px_3px_rgba(0,0,0,0.06),0_4px_8px_-2px_rgba(0,0,0,0.04)]"
            : "shadow-none",
        ].join(" ")}
      >
        <div className="mx-auto flex max-w-[1180px] flex-wrap items-center justify-between gap-3 px-6 py-3 md:px-14">
          <form onSubmit={handleSubmit} className="flex min-w-[220px] flex-1 items-center gap-2" role="search">
            <label className="sr-only" htmlFor="sermon-view-search">Search sermons</label>
            <div className="relative flex-1">
              <Search
                aria-hidden="true"
                strokeWidth={1.75}
                className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted"
              />
              <input
                id="sermon-view-search"
                type="search"
                value={currentValue}
                onChange={(e) => handleChange(e.target.value)}
                placeholder={placeholder}
                className="h-9 w-full border border-border-soft bg-background pl-9 pr-3 text-sm focus-visible:border-accent focus-visible:outline-none"
              />
            </div>
            {showClear && (
              <button
                type="button"
                onClick={onClear}
                aria-label="Clear search"
                title="Clear search"
                className="inline-flex h-9 w-9 shrink-0 items-center justify-center border border-border-soft bg-background text-muted transition-colors hover:bg-panel hover:text-foreground"
              >
                <X aria-hidden="true" strokeWidth={1.75} className="h-4 w-4" />
              </button>
            )}
          </form>
          <nav aria-label="Sermon views" className="flex items-center">
            <Tab to="/sermons" label="Latest" active={active === "latest"} />
            <Tab to="/sermons/series" label="By Series" active={active === "by-series"} />
            <Tab to="/sermons/by-book" label="By Book" active={active === "by-book"} />
          </nav>
        </div>
      </div>
    </>
  );
});

interface TabProps { to: string; label: string; active: boolean }

function Tab({ to, label, active }: TabProps) {
  const baseClass = "inline-flex h-9 items-center px-4 text-xs font-medium uppercase tracking-[0.12em] transition-colors";
  const stateClass = active
    ? "bg-inset text-inset-foreground"
    : "text-fg-soft hover:bg-background";
  return (
    <Link to={to} aria-current={active ? "page" : undefined} className={[baseClass, stateClass].join(" ")}>
      {label}
    </Link>
  );
}
