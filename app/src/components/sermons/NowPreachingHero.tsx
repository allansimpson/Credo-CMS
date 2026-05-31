import { Link } from "react-router-dom";
import { ArrowRight, Play } from "lucide-react";
import { Eyebrow, Headline } from "@/components/public";
import type { PublicSermonSeriesWithStats } from "@/lib/api/sermonSeries";
import { ActiveProgress } from "./ActiveProgress";
import { ContextLabel } from "./ContextLabel";

export interface NowPreachingHeroProps {
  series: PublicSermonSeriesWithStats;
  /** Configured context list from Site Settings — drives the dot color
   * lookup in <ContextLabel>. */
  contexts: string[];
}

/**
 * Flagship hero for the most-recently active series on the by-series
 * page. Two columns on md+ (art on the left, content on the right);
 * stacks below md. Per resolution #4 of the handoff, the play overlay
 * on the art links to the latest sermon and the right column carries
 * one text CTA, "All N in the series". No outer card wrapper — each
 * surface is its own independent anchor so nothing nests.
 */
export function NowPreachingHero({ series, contexts }: NowPreachingHeroProps) {
  const latest = series.latestSermon;
  const seriesHref = `/sermons/series/${encodeURIComponent(series.slug)}`;
  const latestHref = latest ? `/sermons/${encodeURIComponent(latest.slug)}` : seriesHref;
  const allLabel = series.sermonCount === 1 ? "View the series" : `All ${series.sermonCount} in the series`;

  return (
    <section className="border-b border-border-soft">
      <div className="mx-auto max-w-[1180px] px-6 py-10 md:px-14 md:py-14">
        <div className="flex flex-wrap items-baseline justify-between gap-3">
          <Eyebrow accent>Now preaching</Eyebrow>
          {latest && (
            <span className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
              Latest · {latest.dateLabel}
            </span>
          )}
        </div>

        <div className="mt-6 grid gap-10 md:grid-cols-[1.3fr_1fr]">
          {/* Art + play overlay → latest sermon. Independent anchor. */}
          <Link
            to={latestHref}
            className="relative block aspect-video w-full overflow-hidden bg-inset"
            aria-label={latest ? `Play latest sermon: ${latest.title}` : `Browse ${series.title}`}
          >
            {series.bannerImageUrl ? (
              <picture>
                {series.bannerImageWebpUrl && (
                  <source srcSet={series.bannerImageWebpUrl} type="image/webp" />
                )}
                <img
                  src={series.bannerImageUrl}
                  alt={series.bannerImageAlt ?? ""}
                  className="h-full w-full object-cover"
                  loading="eager"
                />
              </picture>
            ) : (
              <div
                aria-hidden="true"
                className="h-full w-full"
                style={{
                  backgroundImage:
                    "repeating-linear-gradient(45deg, transparent 0 14px, hsl(var(--panel-alt)) 14px 15px)",
                }}
              />
            )}
            <span
              aria-hidden="true"
              className="absolute left-4 top-4 inline-flex items-center gap-2 bg-accent px-3 py-1 font-mono text-[10px] uppercase tracking-[0.16em] text-accent-foreground"
            >
              {series.context}
            </span>
            <span
              aria-hidden="true"
              className="absolute left-1/2 top-1/2 inline-flex h-[60px] w-[60px] -translate-x-1/2 -translate-y-1/2 items-center justify-center bg-accent text-accent-foreground transition-transform group-hover:scale-105"
            >
              <Play strokeWidth={1.5} className="h-6 w-6" />
            </span>
          </Link>

          {/* Right column. Title is its own anchor → series detail. */}
          <div className="flex flex-col justify-center">
            <ContextLabel context={series.context} contexts={contexts} />
            <Link to={seriesHref} className="mt-3 inline-block hover:underline">
              <Headline as="h2" size="h1">{series.title}</Headline>
            </Link>
            <p className="mt-2 font-mono text-[12.5px] uppercase tracking-[0.06em] text-accent">
              {series.scopeLabel}
            </p>
            {series.description && (
              <p className="mt-4 max-w-prose text-base leading-relaxed text-fg-soft">
                {series.description}
              </p>
            )}
            <ActiveProgress
              sermonCount={series.sermonCount}
              plannedParts={series.plannedParts}
              className="mt-5 max-w-[380px]"
            />
            <div className="mt-6">
              <Link
                to={seriesHref}
                className="inline-flex items-center gap-2 border border-border bg-panel px-5 py-2.5 text-sm font-semibold hover:bg-panel-alt"
              >
                {allLabel}
                <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
              </Link>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
