import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { sermonSeriesApi, type PublicSermonSeriesWithStats } from "@/lib/api/sermonSeries";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { Eyebrow, Headline } from "@/components/public";
import { SeriesPageHeader } from "@/components/sermons/SeriesPageHeader";
import { SeriesViewBar } from "@/components/sermons/SeriesViewBar";
import { NowPreachingHero } from "@/components/sermons/NowPreachingHero";
import { AlsoRunning } from "@/components/sermons/AlsoRunning";
import { SeriesArchive } from "@/components/sermons/SeriesArchive";

/**
 * Public Sermons → By Series. Rewritten per the design handoff to lead
 * with a "Now preaching" hero for the flagship active series, surface
 * the remaining active series as a compact "Also running" band, and
 * present the completed series as a year-grouped two-column index.
 * The shared search + view-switch bar is mounted by this page; the
 * archive + by-book pages will adopt it as a fast-follow.
 */
export function SermonSeriesListPublicPage() {
  const { settings } = useSiteSettings();
  const [items, setItems] = useState<PublicSermonSeriesWithStats[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [searchText, setSearchText] = useState("");

  useEffect(() => {
    let cancelled = false;
    sermonSeriesApi.listPublicWithStats()
      .then((d) => { if (!cancelled) { setItems(d); setError(false); } })
      .catch(() => { if (!cancelled) setError(true); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  // Sermon contexts list — drives the colored dot lookup. Lives on the
  // admin DTO so it's not exposed via the public settings endpoint;
  // we hydrate from the per-church configured list at runtime via the
  // PublicSiteSettings shape. Fall back to a single inert entry so the
  // page still renders coherently if the field is empty.
  const contexts = useMemo(() => {
    // The public bootstrap doesn't currently surface this — derive the
    // implied palette ordering from the unique contexts present in the
    // returned series. Order by first occurrence, which is stable
    // because the API returns series ordered by StartDate desc.
    const seen = new Set<string>();
    const ordered: string[] = [];
    for (const s of items) {
      if (!seen.has(s.context)) {
        seen.add(s.context);
        ordered.push(s.context);
      }
    }
    return ordered;
  }, [items]);

  // In-page search filters series by title or scope label — stays on
  // this tab instead of bouncing to Latest's full-text sermon search.
  const visibleItems = useMemo(() => {
    const needle = searchText.trim().toLowerCase();
    if (!needle) return items;
    return items.filter((s) => {
      return s.title.toLowerCase().includes(needle)
        || (s.scopeLabel?.toLowerCase().includes(needle) ?? false);
    });
  }, [items, searchText]);
  const hasTextFilter = searchText.trim().length > 0;

  const active = useMemo(() => visibleItems.filter((s) => s.status === "active"), [visibleItems]);
  const complete = useMemo(() => visibleItems.filter((s) => s.status === "complete"), [visibleItems]);

  // Flagship = the active series with the most-recent published sermon.
  // Stable-sort by startDate desc when latestSermon ties or is null.
  const flagship = useMemo(() => {
    if (active.length === 0) return null;
    const ranked = active.slice().sort((a, b) => {
      const at = a.latestSermon ? new Date(a.latestSermon.publishedAt).getTime() : 0;
      const bt = b.latestSermon ? new Date(b.latestSermon.publishedAt).getTime() : 0;
      if (bt !== at) return bt - at;
      return new Date(b.startDate).getTime() - new Date(a.startDate).getTime();
    });
    return ranked[0];
  }, [active]);

  const otherActive = useMemo(() => {
    if (!flagship) return [];
    return active.filter((s) => s.id !== flagship.id);
  }, [active, flagship]);

  const totalMessages = useMemo(
    () => items.reduce((sum, s) => sum + s.sermonCount, 0),
    [items],
  );

  return (
    <div>
      <SeoTags
        title={`Sermon Series · ${settings?.churchName ?? ""}`}
        description="Browse our sermon series — what we're preaching now and what we've covered before."
      />

      <SeriesPageHeader
        totalSeries={items.length}
        totalMessages={totalMessages}
        activeCount={active.length}
      />

      <SeriesViewBar
        active="by-series"
        placeholder="Search series — 'Hebrews', 'Advent', 'Psalms'"
        value={searchText}
        onChange={setSearchText}
        onSubmit={() => { /* filtering is real-time */ }}
        hasAppliedQuery={hasTextFilter}
        onClear={() => setSearchText("")}
      />

      {loading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState />
      ) : hasTextFilter && visibleItems.length === 0 ? (
        <NoSeriesMatches onClear={() => setSearchText("")} />
      ) : hasTextFilter ? (
        // Filtered view: show one combined list (no hero, no archive
        // grouping). Keeps the result density tight when search returns
        // a small set spanning active + complete.
        <FilteredSeriesList items={visibleItems} contexts={contexts} />
      ) : (
        <>
          {flagship ? (
            <NowPreachingHero series={flagship} contexts={contexts} />
          ) : (
            <BetweenSeries />
          )}

          {otherActive.length > 0 && (
            <AlsoRunning series={otherActive} contexts={contexts} />
          )}

          <SeriesArchive series={complete} />
        </>
      )}
    </div>
  );
}

function NoSeriesMatches({ onClear }: { onClear: () => void }) {
  return (
    <div className="mx-auto max-w-[1180px] px-6 py-14 md:px-14">
      <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">No matches</p>
      <p className="mt-3 text-fg-soft">There are no series matching that search.</p>
      <button
        type="button"
        onClick={onClear}
        className="mt-4 inline-flex items-center border border-border-soft bg-background px-4 py-2 text-sm font-medium hover:bg-panel-alt"
      >
        Clear Search
      </button>
    </div>
  );
}

/** Combined list rendered during a text-filter search — strips the hero
 * + archive grouping so the matches read as a focused result set. */
function FilteredSeriesList({
  items,
  contexts,
}: {
  items: PublicSermonSeriesWithStats[];
  contexts: string[];
}) {
  return (
    <section className="mx-auto max-w-[1180px] px-6 py-10 md:px-14">
      <p className="mb-4 font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
        {items.length} {items.length === 1 ? "match" : "matches"}
      </p>
      <ul className="grid gap-3 sm:grid-cols-2">
        {items.map((s) => <FilteredSeriesRow key={s.id} series={s} contexts={contexts} />)}
      </ul>
    </section>
  );
}

function FilteredSeriesRow({
  series,
  contexts,
}: {
  series: PublicSermonSeriesWithStats;
  contexts: string[];
}) {
  // Lightweight; just enough to identify the series. ContextLabel + a
  // status hint (active vs completed) keep parity with the hero/archive
  // rows the user sees outside of search.
  const isActive = series.status === "active";
  const contextIndex = contexts.indexOf(series.context);
  return (
    <li>
      <Link
        to={`/sermons/series/${encodeURIComponent(series.slug)}`}
        className="block border border-border bg-panel p-4 transition-colors hover:bg-panel-alt"
      >
        <p className="font-mono text-[10px] uppercase tracking-[0.14em] text-muted">
          <span aria-hidden="true">{contextIndex >= 0 ? series.context : "Series"}</span>
          {" · "}
          <span className={isActive ? "text-accent" : "text-muted"}>
            {isActive ? "Active" : "Completed"}
          </span>
        </p>
        <h3 className="mt-1 truncate font-heading text-base font-semibold">{series.title}</h3>
        <p className="mt-1 truncate font-mono text-[10.5px] uppercase tracking-[0.10em] text-muted">
          {series.scopeLabel} · {series.sermonCount} {series.sermonCount === 1 ? "message" : "messages"}
        </p>
      </Link>
    </li>
  );
}

function LoadingState() {
  return (
    <div className="mx-auto max-w-[1180px] px-6 py-14 md:px-14">
      <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">Loading…</p>
    </div>
  );
}

function ErrorState() {
  return (
    <div className="mx-auto max-w-[1180px] px-6 py-14 md:px-14">
      <p className="text-danger">Could not load the series list. Try refreshing.</p>
    </div>
  );
}

/** "Between series" empty state shown when no active series exists. */
function BetweenSeries() {
  return (
    <section className="border-b border-border-soft">
      <div className="mx-auto max-w-[1180px] px-6 py-12 md:px-14">
        <div className="bg-panel-alt px-6 py-10">
          <Eyebrow>Between series</Eyebrow>
          <Headline as="h2" size="h3" className="mt-3">
            We&rsquo;re between teaching series right now.
          </Headline>
          <p className="mt-3 max-w-prose text-fg-soft">
            New series will appear here as soon as they begin. In the meantime, you can browse our
            latest stand-alone sermons.
          </p>
          <div className="mt-5">
            <Link
              to="/sermons"
              className="inline-flex items-center gap-2 border border-border bg-panel px-5 py-2.5 text-sm font-medium hover:bg-panel"
            >
              Browse latest sermons
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
