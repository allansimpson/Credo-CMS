import { Eyebrow, Headline } from "@/components/public";

export interface SeriesPageHeaderProps {
  totalSeries: number;
  totalMessages: number;
  activeCount: number;
}

/**
 * The top eyebrow + headline strip on the public by-series page. The
 * meta count line on the right ("12 series · 184 messages · 2 running
 * now") is the single source of truth for those numbers — derive from
 * the same list the page renders so nothing drifts.
 */
export function SeriesPageHeader({ totalSeries, totalMessages, activeCount }: SeriesPageHeaderProps) {
  return (
    <header className="mx-auto max-w-[1180px] px-6 py-10 md:px-14 md:py-12">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <Eyebrow accent>Sermons · By Series</Eyebrow>
          <Headline as="h1" size="display" className="mt-3">
            Follow the thread.
          </Headline>
        </div>
        <p className="hidden font-mono text-[11px] uppercase tracking-[0.14em] text-muted md:block">
          {totalSeries} Series · {totalMessages} Messages · {activeCount} Running Now
        </p>
      </div>
    </header>
  );
}
