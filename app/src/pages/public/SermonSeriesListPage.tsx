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

  const active = useMemo(() => items.filter((s) => s.status === "active"), [items]);
  const complete = useMemo(() => items.filter((s) => s.status === "complete"), [items]);

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

      <SeriesViewBar active="by-series" />

      {loading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState />
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

function LoadingState() {
  return (
    <div className="mx-auto max-w-7xl px-6 py-14">
      <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">Loading…</p>
    </div>
  );
}

function ErrorState() {
  return (
    <div className="mx-auto max-w-7xl px-6 py-14">
      <p className="text-danger">Could not load the series list. Try refreshing.</p>
    </div>
  );
}

/** "Between series" empty state shown when no active series exists. */
function BetweenSeries() {
  return (
    <section className="border-b border-border-soft">
      <div className="mx-auto max-w-7xl px-6 py-12">
        <div className="bg-panel-alt px-6 py-10">
          <Eyebrow>Between series</Eyebrow>
          <Headline as="h2" size="h3" className="mt-3">
            We&rsquo;re between teaching series right now.
          </Headline>
          <p className="mt-3 max-w-prose text-fg-soft">
            New series will appear here as soon as they begin. In the meantime, you can browse our
            latest stand-alone messages.
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
