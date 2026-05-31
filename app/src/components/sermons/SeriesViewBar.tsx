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

  const handleClear = () => {
    // Reset the input value in either mode...
    if (isControlled) onChange?.("");
    else setInternalValue("");
    // ...and always let the caller act on the clear (e.g. dropping a
    // URL filter — the book filter on By Book, the q param on Latest).
    onClear?.();
  };

  // Show the clear X as soon as the input has text. In controlled mode
  // (Latest), `hasAppliedQuery` also keeps it visible after submit even
  // if the input has been blurred — the caller owns that state. In
  // uncontrolled mode (By Series / By Book), the value alone drives it.
  const showClear = currentValue.length > 0 || (!!onClear && hasAppliedQuery);

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
          <form onSubmit={handleSubmit} className="min-w-[220px] flex-1" role="search">
            <label className="sr-only" htmlFor="sermon-view-search">Search sermons</label>
            <div className="relative w-full">
              <button
                type="submit"
                aria-label="Submit search"
                title="Search"
                className="absolute left-0 top-0 inline-flex h-9 w-9 items-center justify-center text-muted transition-colors hover:text-foreground focus-visible:text-foreground focus-visible:outline-none"
              >
                <Search aria-hidden="true" strokeWidth={1.75} className="h-4 w-4" />
              </button>
              <input
                id="sermon-view-search"
                type="search"
                value={currentValue}
                onChange={(e) => handleChange(e.target.value)}
                placeholder={placeholder}
                className={[
                  "h-9 w-full border border-border-soft bg-background pl-9 text-sm",
                  "focus-visible:border-accent focus-visible:outline-none",
                  // pr-10 reserves space for the inline clear button so
                  // typed text doesn't slide under it. When no clear is
                  // visible, pr-3 is the standard right padding — same
                  // visible input edge across all three browse surfaces.
                  showClear ? "pr-10" : "pr-3",
                ].join(" ")}
              />
              {showClear && (
                <button
                  type="button"
                  onClick={handleClear}
                  aria-label="Clear search"
                  title="Clear search"
                  className="absolute right-1.5 top-1/2 inline-flex h-6 w-6 -translate-y-1/2 items-center justify-center text-muted transition-colors hover:text-foreground"
                >
                  <X aria-hidden="true" strokeWidth={1.75} className="h-4 w-4" />
                </button>
              )}
            </div>
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
