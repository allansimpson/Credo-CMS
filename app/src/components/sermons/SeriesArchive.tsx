import { Link } from "react-router-dom";
import { Headline } from "@/components/public";
import type { PublicSermonSeriesWithStats } from "@/lib/api/sermonSeries";

export interface SeriesArchiveProps {
  series: PublicSermonSeriesWithStats[];
}

/**
 * Completed-series index — two-column year-grouped list. Whole year
 * groups stay intact; no group is split across columns. The "Between
 * series" empty state is rendered here so the slot never collapses
 * (per resolution #6 of the handoff).
 */
export function SeriesArchive({ series }: SeriesArchiveProps) {
  const completedCount = series.length;
  const yearGroups = groupByStartYear(series);
  const [colA, colB] = splitColumns(yearGroups);

  return (
    <section>
      <div className="mx-auto max-w-[1180px] px-6 pb-16 pt-10 md:px-14">
        <div className="flex flex-wrap items-baseline justify-between gap-3 border-b border-border-soft pb-4">
          <Headline as="h2" size="h3">The archive</Headline>
          <span className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
            {completedCount} Completed {completedCount === 1 ? "Series" : "Series"}
          </span>
        </div>

        {completedCount === 0 ? (
          <EmptyArchive />
        ) : (
          <div className="mt-8 grid gap-x-16 gap-y-8 md:grid-cols-2">
            <ArchiveColumn groups={colA} />
            <ArchiveColumn groups={colB} />
          </div>
        )}
      </div>
    </section>
  );
}

interface YearGroup {
  year: number;
  items: PublicSermonSeriesWithStats[];
}

function groupByStartYear(items: PublicSermonSeriesWithStats[]): YearGroup[] {
  const map = new Map<number, PublicSermonSeriesWithStats[]>();
  for (const s of items) {
    const year = new Date(s.startDate).getUTCFullYear();
    const bucket = map.get(year);
    if (bucket) bucket.push(s); else map.set(year, [s]);
  }
  return Array.from(map.entries())
    .map(([year, list]) => ({
      year,
      items: list.slice().sort((a, b) =>
        new Date(b.startDate).getTime() - new Date(a.startDate).getTime()),
    }))
    .sort((a, b) => b.year - a.year);
}

function splitColumns(groups: YearGroup[]): [YearGroup[], YearGroup[]] {
  if (groups.length === 0) return [[], []];
  const half = Math.ceil(groups.length / 2);
  return [groups.slice(0, half), groups.slice(half)];
}

function ArchiveColumn({ groups }: { groups: YearGroup[] }) {
  return (
    <div className="space-y-7">
      {groups.map((g) => <ArchiveYearGroup key={g.year} group={g} />)}
    </div>
  );
}

function ArchiveYearGroup({ group }: { group: YearGroup }) {
  return (
    <div>
      <div className="flex items-baseline justify-between border-b border-border pb-2.5 font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
        <span>{group.year}</span>
        <span className="opacity-60">· {group.items.length}</span>
      </div>
      <ul className="divide-y divide-border-soft">
        {group.items.map((s) => <ArchiveIndexRow key={s.id} series={s} />)}
      </ul>
    </div>
  );
}

function ArchiveIndexRow({ series }: { series: PublicSermonSeriesWithStats }) {
  const startMonth = new Date(series.startDate).toLocaleDateString("en-US", {
    month: "short", year: "numeric", timeZone: "UTC",
  });
  return (
    <li>
      <Link
        to={`/sermons/series/${encodeURIComponent(series.slug)}`}
        className="grid items-baseline gap-4 py-3.5 hover:opacity-90"
        style={{ gridTemplateColumns: "1fr auto" }}
      >
        <div className="min-w-0">
          <p className="truncate font-heading text-[17px] font-medium leading-snug">
            {series.title}
          </p>
          <p className="mt-1 truncate font-mono text-[10.5px] uppercase tracking-[0.12em] text-muted">
            {series.scopeLabel} · {startMonth}
          </p>
        </div>
        <span className="font-mono text-[13px] font-semibold tabular-nums text-accent">
          {series.sermonCount}
        </span>
      </Link>
    </li>
  );
}

function EmptyArchive() {
  return (
    <div className="mt-8 border bg-panel-alt px-6 py-10 text-center">
      <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
        Archive
      </p>
      <p className="mt-3 text-fg-soft">
        No completed series yet — they&rsquo;ll be archived here once a current series wraps.
      </p>
    </div>
  );
}
