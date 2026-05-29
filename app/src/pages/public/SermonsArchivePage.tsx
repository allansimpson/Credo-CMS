import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { ListFilter, ArrowDown, ArrowUp } from "lucide-react";
import {
  publicSermonsApi,
  type ServiceDay,
  type SermonsByDayQuery,
  type SermonsByDayResponse,
  type YearsResponse,
  type YearStats,
} from "@/lib/api/publicSermons";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { Eyebrow, Headline } from "@/components/public";
import { ServiceDayBlock } from "@/components/sermons/ServiceDayBlock";
import { SideRail } from "@/components/sermons/SideRail";
import { MobileRailDrawer } from "@/components/sermons/MobileRailDrawer";
import { SeriesViewBar } from "@/components/sermons/SeriesViewBar";
import { useActiveMonth } from "@/hooks/useActiveMonth";
import { monthSlugFor } from "@/lib/months";

interface SermonsArchivePageProps {
  /** Set by SermonsSegmentDispatcher when the URL is `/sermons/{4-digit-year}`. */
  yearParam?: number;
}

const PAGE_SIZE = 60; // ~one calendar year of Sundays + midweek services

export function SermonsArchivePage({ yearParam }: SermonsArchivePageProps = {}) {
  const navigate = useNavigate();
  const location = useLocation();
  const { settings } = useSiteSettings();
  const [searchParams, setSearchParams] = useSearchParams();
  const appliedQuery = searchParams.get("q") ?? "";
  const tagSlug = searchParams.get("tag") ?? undefined;
  const isSearchMode = appliedQuery.length > 0;

  // Comprehensive year index for the rail (drives the year list in both
  // browse and search modes). Fetched once on mount.
  const [yearsResp, setYearsResp] = useState<YearsResponse | null>(null);

  // Listing payload for the current page.
  const [listing, setListing] = useState<SermonsByDayResponse | null>(null);
  const [days, setDays] = useState<ServiceDay[]>([]);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [searchInput, setSearchInput] = useState(appliedQuery);

  // Mobile drawer state — desktop ignores.
  const [drawerOpen, setDrawerOpen] = useState(false);

  // Measure the sticky filter bar so the rail can stick directly below it
  // (instead of at top-6, which leaves the bar overlapping the rail title).
  // The combined offset is exposed via --sermons-filter-offset so the rail's
  // sticky top can stack public-nav + filter-bar heights. SeriesViewBar
  // owns the stuck-state shadow internally; we only measure for the rail.
  const filterBarRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = filterBarRef.current;
    if (!el) return;
    const update = () => {
      document.documentElement.style.setProperty(
        "--sermons-filter-offset",
        `${el.offsetHeight}px`,
      );
    };
    update();
    const ro = new ResizeObserver(update);
    ro.observe(el);
    window.addEventListener("resize", update);
    return () => {
      ro.disconnect();
      window.removeEventListener("resize", update);
      document.documentElement.style.removeProperty("--sermons-filter-offset");
    };
  }, []);


  // ── Years bootstrap ────────────────────────────────────────────────────
  useEffect(() => {
    let cancelled = false;
    publicSermonsApi.years().then((res) => {
      if (cancelled) return;
      setYearsResp(res);
      // `/sermons` (no yearParam) AND not in search → bounce to the latest
      // year. Per Claude Design: never render an empty /sermons page.
      if (yearParam === undefined && !isSearchMode && res.years.length > 0) {
        const tagQs = tagSlug ? `?tag=${encodeURIComponent(tagSlug)}` : "";
        navigate(`/sermons/${res.currentYear}${tagQs}`, { replace: true });
      }
    }).catch(() => { /* leave yearsResp null, hide rail */ });
    return () => { cancelled = true; };
  }, [yearParam, isSearchMode, tagSlug, navigate]);

  // Keep the search input in sync if the URL query changes externally
  // (e.g. browser back/forward).
  useEffect(() => { setSearchInput(appliedQuery); }, [appliedQuery]);

  // Reset pagination when the filter context changes.
  useEffect(() => { setPage(1); setDays([]); }, [yearParam, appliedQuery, tagSlug]);

  // ── Listing fetch ──────────────────────────────────────────────────────
  useEffect(() => {
    // Skip until we know which year to show (avoid double-fetch during redirect).
    if (yearParam === undefined && !isSearchMode) return;

    let cancelled = false;
    setLoading(true);
    const query: SermonsByDayQuery = {
      search: appliedQuery || undefined,
      tagSlug,
      // Year filter is ignored by the backend when search is active, but
      // we still omit it for clarity / smaller URL.
      year: isSearchMode ? undefined : yearParam,
      page,
      pageSize: PAGE_SIZE,
    };
    publicSermonsApi.byDay(query)
      .then((res) => {
        if (cancelled) return;
        setListing(res);
        setDays((prev) => page === 1 ? res.days : [...prev, ...res.days]);
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [yearParam, appliedQuery, tagSlug, page, isSearchMode]);

  // ── Scroll-spy ─────────────────────────────────────────────────────────
  // Re-subscribes whenever the days array length changes (new month anchors
  // mount) or the year changes.
  const activeMonth = useActiveMonth("[data-month]", true, [days.length, yearParam, isSearchMode]);

  // ── Hash-scroll on every hash change ──────────────────────────────────
  // Re-fires whenever the route's hash changes (rail month click, hash-deep-
  // link landing, browser back/forward over month anchors). Skips while the
  // listing is loading or has no days yet — the anchor element won't exist.
  //
  // useActiveMonth updates the URL hash via raw history.replaceState which
  // does NOT trigger a react-router re-render, so this effect doesn't fire
  // for ambient scroll movement — only for navigation-driven hash changes.
  useEffect(() => {
    if (loading || days.length === 0) return;
    const hash = location.hash.replace("#", "");
    if (!hash) return;
    const target = document.getElementById(hash);
    if (!target) return;
    const prefersReduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    target.scrollIntoView({ behavior: prefersReduced ? "auto" : "smooth", block: "start" });
  }, [location.hash, loading, days.length]);

  // ── Derived view state ─────────────────────────────────────────────────
  const currentYear = yearParam ?? yearsResp?.currentYear;
  const totalSermons = useMemo(() => days.reduce((n, d) => n + d.sermons.length, 0), [days]);
  const totalDays = listing?.totalDays ?? 0;
  const totalPages = listing?.totalPages ?? 0;

  // Spillover-CTA neighbor lookup — only show if the neighbor year exists
  // in the comprehensive years list.
  const knownYears = useMemo(() => new Set(yearsResp?.years.map(y => y.year) ?? []), [yearsResp]);
  const olderNeighbor = currentYear !== undefined && knownYears.has(currentYear - 1) ? currentYear - 1 : null;
  const newerNeighbor = currentYear !== undefined && knownYears.has(currentYear + 1) ? currentYear + 1 : null;

  // ── Handlers ───────────────────────────────────────────────────────────
  const submitSearch = (trimmed: string) => {
    const next = new URLSearchParams(searchParams);
    if (trimmed) {
      next.set("q", trimmed);
      // Search exits year-browse. The /sermons route is the search home.
      setSearchParams(next);
      navigate(`/sermons?${next.toString()}`);
    } else {
      next.delete("q");
      setSearchParams(next);
    }
  };

  const clearSearch = () => {
    setSearchInput("");
    const next = new URLSearchParams(searchParams);
    next.delete("q");
    // Return to the year the user was on (or the most recent year).
    const target = yearParam ?? yearsResp?.currentYear;
    const qs = next.toString();
    if (target !== undefined) navigate(`/sermons/${target}${qs ? `?${qs}` : ""}`);
    else setSearchParams(next);
  };

  // ── Render ─────────────────────────────────────────────────────────────
  const railYears: YearStats[] = yearsResp?.years ?? [];

  return (
    <div>
      <SeoTags
        title={`Sermons · ${settings?.churchName ?? ""}`}
        description="Browse our sermon archive."
      />

      {/* ── Header ────────────────────────────────────────────── */}
      <header className="mx-auto max-w-[1180px] px-6 py-10 md:px-14 md:py-12">
        <div className="flex items-start justify-between gap-6">
          <div>
            <Eyebrow accent>Sermons</Eyebrow>
            <Headline as="h1" size="display" className="mt-3">
              Listen back.
            </Headline>
          </div>
          {currentYear !== undefined && (
            <p className="hidden font-mono text-[11px] uppercase tracking-[0.14em] text-muted md:block">
              {isSearchMode
                ? `${listing?.totalDays ?? 0} matching ${(listing?.totalDays ?? 0) === 1 ? "day" : "days"}`
                : `${currentYear} · ${totalSermons} sermons`}
            </p>
          )}
        </div>
      </header>

      {/* ── Filter bar (shared across all sermon browse tabs) ─── */}
      <SeriesViewBar
        ref={filterBarRef}
        active="latest"
        placeholder="Search sermons, passages, speakers"
        value={searchInput}
        onChange={setSearchInput}
        onSubmit={submitSearch}
        onClear={clearSearch}
        hasAppliedQuery={isSearchMode}
      />

      {tagSlug && (
        <div className="mx-auto max-w-[1180px] px-6 py-2 md:px-14">
          <p className="text-xs text-muted">
            Filtered by tag: <strong>{tagSlug}</strong>{" "}
            <Link
              to={currentYear ? `/sermons/${currentYear}` : "/sermons"}
              className="text-primary hover:underline"
            >
              clear
            </Link>
          </p>
        </div>
      )}

      {/* ── Mobile rail trigger ───────────────────────────────── */}
      <div className="mx-auto max-w-[1180px] px-6 pt-4 md:hidden">
        <button
          type="button"
          onClick={() => setDrawerOpen(true)}
          className="inline-flex items-center gap-2 border border-border-soft bg-background px-3 py-2 text-xs font-medium hover:bg-panel-alt"
        >
          <ListFilter className="h-3.5 w-3.5" />
          Browse archive
          {currentYear !== undefined && (
            <span className="text-muted">· {currentYear}</span>
          )}
        </button>
      </div>

      {/* ── Body: rail + listing ──────────────────────────────── */}
      <section className="mx-auto max-w-[1180px] px-6 py-8 md:px-14 md:py-10">
        <div className="grid grid-cols-1 gap-0 md:grid-cols-[200px_1fr] md:gap-14">
          {/* Rail (desktop only) */}
          <div className="hidden md:block">
            {railYears.length > 0 && (
              <SideRail
                mode={isSearchMode ? "search" : "browse"}
                years={railYears}
                currentYear={currentYear}
                activeMonth={activeMonth}
                searchQuery={isSearchMode ? appliedQuery : undefined}
                yearStats={isSearchMode ? listing?.yearStats ?? [] : undefined}
                onClearSearch={isSearchMode ? clearSearch : undefined}
              />
            )}
          </div>

          {/* Listing column */}
          <div className="relative min-w-0">
            {/* Top spillover: link to the year above (newer) */}
            {!isSearchMode && newerNeighbor !== null && (
              <Link
                to={`/sermons/${newerNeighbor}`}
                className="mb-6 inline-flex items-center gap-2 border-b border-border-soft pb-3 font-mono text-[11px] uppercase tracking-[0.14em] text-muted hover:text-foreground"
              >
                <ArrowUp className="h-3 w-3" />
                Sermons from {newerNeighbor}
              </Link>
            )}

            {/* Skeleton (first load) */}
            {loading && days.length === 0 && (
              <div>
                {[1, 2, 3].map((i) => (
                  <div key={i} className="grid animate-pulse gap-10 border-b border-border-soft py-11 [grid-template-columns:180px_1fr]">
                    <div>
                      <div className="h-3 w-20 bg-border-soft" />
                      <div className="mt-3 h-24 w-20 bg-border-soft" />
                      <div className="mt-2 h-4 w-24 bg-border-soft" />
                    </div>
                    <div className="grid gap-7 [grid-template-columns:320px_1fr]">
                      <div className="aspect-[16/10] bg-border-soft" />
                      <div className="space-y-3 py-4">
                        <div className="h-3 w-32 bg-border-soft" />
                        <div className="h-8 w-64 bg-border-soft" />
                        <div className="h-3 w-48 bg-border-soft" />
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {/* Empty states */}
            {!loading && days.length === 0 && (
              <div className="py-16 text-center">
                {isSearchMode ? (
                  <>
                    <p className="text-fg-soft">No sermons matched &ldquo;{appliedQuery}&rdquo;.</p>
                    <button
                      type="button"
                      onClick={clearSearch}
                      className="mt-2 text-sm text-accent hover:underline"
                    >
                      Clear search
                    </button>
                  </>
                ) : currentYear !== undefined ? (
                  <p className="text-fg-soft">No sermons published in {currentYear}.</p>
                ) : (
                  <p className="text-fg-soft">The archive will appear here once the first sermon is published.</p>
                )}
              </div>
            )}

            {/* Day blocks, with month anchors injected at month boundaries. */}
            <div
              className={`divide-y divide-border-soft transition-opacity duration-[var(--motion-md)] [transition-timing-function:var(--motion-ease)] ${loading && days.length > 0 ? "pointer-events-none opacity-40" : "opacity-100"}`}
            >
              {renderDaysWithMonthAnchors(days)}
            </div>

            {/* Inline loading pill — for subsequent search/filter changes */}
            {loading && days.length > 0 && (
              <div
                aria-live="polite"
                className="pointer-events-none absolute inset-x-0 top-8 z-10 flex justify-center"
              >
                <div className="inline-flex items-center gap-3 border border-border-soft bg-panel-alt px-5 py-2.5 shadow-sm">
                  <span aria-hidden className="flex gap-1">
                    <span className="h-1.5 w-1.5 animate-[pulse_1.2s_ease-in-out_infinite] rounded-full bg-accent" />
                    <span className="h-1.5 w-1.5 animate-[pulse_1.2s_ease-in-out_infinite] rounded-full bg-accent [animation-delay:200ms]" />
                    <span className="h-1.5 w-1.5 animate-[pulse_1.2s_ease-in-out_infinite] rounded-full bg-accent [animation-delay:400ms]" />
                  </span>
                  <span className="font-mono text-[11px] uppercase tracking-[0.18em] text-muted">Loading</span>
                </div>
              </div>
            )}

            {/* Pagination — only relevant in search mode when results overflow
                pageSize. Year mode renders the full year (pageSize=60) so
                this is typically a no-op. */}
            {totalPages > page && (
              <div className="flex flex-col items-center gap-2 py-10">
                <button
                  type="button"
                  onClick={() => setPage((p) => p + 1)}
                  disabled={loading}
                  className="inline-flex items-center gap-2 border border-border-soft px-5 py-2.5 text-sm font-medium hover:bg-panel-alt disabled:opacity-50"
                >
                  {loading ? "Loading…" : (
                    <>
                      Load more
                      <ArrowDown aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
                    </>
                  )}
                </button>
                <p className="font-mono text-[11px] text-muted">
                  Showing {days.length} of {totalDays}
                </p>
              </div>
            )}

            {/* Bottom spillover: CTA card linking to the year below (older) */}
            {!isSearchMode && olderNeighbor !== null && !loading && days.length > 0 && (
              <Link
                to={`/sermons/${olderNeighbor}`}
                className="mt-10 flex items-center justify-between border border-border bg-panel-alt px-6 py-5 transition-colors hover:bg-panel"
              >
                <div>
                  <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-accent">Continue</p>
                  <p className="mt-1 font-heading text-[22px] font-semibold tracking-[-0.02em]">
                    Sermons from {olderNeighbor}
                  </p>
                </div>
                <ArrowDown className="h-5 w-5 text-fg-soft" />
              </Link>
            )}
          </div>
        </div>
      </section>

      {/* Mobile drawer */}
      {railYears.length > 0 && (
        <MobileRailDrawer
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          mode={isSearchMode ? "search" : "browse"}
          years={railYears}
          currentYear={currentYear}
          activeMonth={activeMonth}
          searchQuery={isSearchMode ? appliedQuery : undefined}
          yearStats={isSearchMode ? listing?.yearStats ?? [] : undefined}
          onClearSearch={isSearchMode ? () => { clearSearch(); setDrawerOpen(false); } : undefined}
        />
      )}
    </div>
  );
}

/**
 * Walks the day list (descending date order) and inserts an invisible month
 * anchor before the first day of each new month. The anchor's `id` matches
 * the slug used in URL hashes (e.g. `/sermons/2024#oct`), and `data-month`
 * is what `useActiveMonth` queries.
 */
function renderDaysWithMonthAnchors(days: ServiceDay[]) {
  const out: React.ReactNode[] = [];
  let prevMonth: string | null = null;
  for (const day of days) {
    const d = new Date(day.date + "T00:00:00");
    const month = monthSlugFor(d.getMonth() + 1);
    if (month !== prevMonth) {
      out.push(
        <span
          key={`anchor-${day.date}`}
          id={month}
          data-month={month}
          aria-hidden="true"
          className="block h-0 scroll-mt-[calc(var(--public-header-offset,0px)_+_var(--sermons-filter-offset,0px)_+_1rem)]"
        />,
      );
      prevMonth = month;
    }
    out.push(<ServiceDayBlock key={day.date} day={day} />);
  }
  return out;
}
