/**
 * Slim track + accent fill for any "covered/total" ratio. Lifted out so
 * the by-series active-progress and the by-book book-coverage rails can
 * share the same primitive.
 */
export interface CoverageBarProps {
  covered: number;
  total: number;
  /** Track height in px. Defaults to 6 — bump to 8 for hero contexts. */
  height?: number;
  ariaLabel?: string;
}

export function CoverageBar({ covered, total, height = 6, ariaLabel }: CoverageBarProps) {
  const safeTotal = Math.max(1, total);
  const pct = Math.min(100, Math.max(0, Math.round((covered / safeTotal) * 100)));
  return (
    <div
      role="progressbar"
      aria-valuenow={pct}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-label={ariaLabel ?? `${covered} of ${total}`}
      className="w-full overflow-hidden"
      style={{ height, backgroundColor: "hsl(var(--border-soft))" }}
    >
      <div
        className="h-full transition-[width] duration-300"
        style={{ width: `${pct}%`, backgroundColor: "hsl(var(--accent))" }}
      />
    </div>
  );
}
