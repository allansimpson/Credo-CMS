import { Link } from "react-router-dom";
import { ArrowRight, Play } from "lucide-react";
import { Eyebrow } from "@/components/public";
import type { PublicSermonSeriesWithStats } from "@/lib/api/sermonSeries";
import { ContextLabel } from "./ContextLabel";

export interface AlsoRunningProps {
  series: PublicSermonSeriesWithStats[];
  contexts: string[];
}

/**
 * "Also running" band shown only when ≥2 active series exist on the
 * by-series page — the flagship hero series above is filtered out by
 * the caller before passing the list here. Two-column grid of compact
 * cards with the same context-dot + scope·progress meta the hero uses.
 */
export function AlsoRunning({ series, contexts }: AlsoRunningProps) {
  if (series.length === 0) return null;

  return (
    <section className="border-b border-border-soft bg-panel-alt">
      <div className="mx-auto max-w-7xl px-6 py-9">
        <Eyebrow>Also running</Eyebrow>
        <div className="mt-5 grid gap-6 sm:grid-cols-2">
          {series.map((s) => <AlsoRunningCard key={s.id} series={s} contexts={contexts} />)}
        </div>
      </div>
    </section>
  );
}

interface AlsoRunningCardProps {
  series: PublicSermonSeriesWithStats;
  contexts: string[];
}

function AlsoRunningCard({ series, contexts }: AlsoRunningCardProps) {
  const href = `/sermons/series/${encodeURIComponent(series.slug)}`;
  const progressMeta = series.plannedParts
    ? `part ${series.sermonCount} of ~${series.plannedParts}`
    : `${series.sermonCount} ${series.sermonCount === 1 ? "part" : "parts"}`;
  return (
    <Link
      to={href}
      className="group grid items-center gap-4 border border-border bg-panel px-5 py-4 transition-colors hover:bg-panel-alt"
      style={{ gridTemplateColumns: "auto 1fr auto" }}
    >
      <span
        aria-hidden="true"
        className="inline-flex h-10 w-10 items-center justify-center"
        style={{ backgroundColor: "hsl(var(--accent-soft, var(--accent) / 0.13))" }}
      >
        <Play strokeWidth={1.5} className="h-4 w-4 text-accent" />
      </span>
      <div className="min-w-0">
        <ContextLabel context={series.context} contexts={contexts} size={10} />
        <h3 className="mt-1 truncate font-heading text-lg font-semibold leading-tight tracking-tight">
          {series.title}
        </h3>
        <p className="mt-1 truncate font-mono text-[11px] uppercase tracking-[0.12em] text-muted">
          {series.scopeLabel} · {progressMeta}
        </p>
      </div>
      <ArrowRight
        aria-hidden="true"
        strokeWidth={1.75}
        className="h-4 w-4 text-muted transition-transform group-hover:translate-x-0.5"
      />
    </Link>
  );
}
