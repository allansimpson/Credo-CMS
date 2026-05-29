import { ChevronRight } from "lucide-react";

/**
 * Truncated numbered pager. Always renders pages 1, 2, last-1, last, and
 * current ±1; replaces gaps with `…`. Active page is rendered in the accent
 * style, inactive pages are bordered chips.
 *
 *   Page 3 of 19  →  ‹ Prev  1 2 [3] 4 … 18 19  Next ›
 *   Page 10 of 19 →  ‹ Prev  1 2 … 9 [10] 11 … 18 19  Next ›
 *
 * Generic: no sermons-specific copy or styling. Reusable across Members /
 * Events / News when their pagers ship.
 */
export interface PageNumbersProps {
  current: number;
  total: number;
  onChange: (page: number) => void;
  disabled?: boolean;
}

export function PageNumbers({ current, total, onChange, disabled = false }: PageNumbersProps) {
  if (total <= 1) return null;

  const items = buildPagerItems(current, total);

  const baseBtn =
    "inline-flex h-[34px] min-w-[34px] items-center justify-center border border-border bg-transparent px-2 font-mono text-[12.5px] tabular-nums transition-colors hover:bg-panel-alt disabled:opacity-40 disabled:hover:bg-transparent";

  return (
    <nav aria-label="Pagination" className="flex items-center gap-1">
      <button
        type="button"
        aria-label="Previous page"
        disabled={disabled || current <= 1}
        onClick={() => onChange(current - 1)}
        className={baseBtn}
      >
        <ChevronRight aria-hidden="true" strokeWidth={1.75} className="h-3.5 w-3.5 -scale-x-100" />
        <span className="ml-1 hidden sm:inline">Prev</span>
      </button>

      {items.map((item, i) =>
        item === "ellipsis" ? (
          <span
            key={`ell-${i}`}
            aria-hidden="true"
            className="inline-flex h-[34px] min-w-[20px] items-center justify-center text-muted"
          >
            …
          </span>
        ) : item === current ? (
          <button
            key={item}
            type="button"
            aria-current="page"
            aria-label={`Page ${item}, current`}
            disabled={disabled}
            className="inline-flex h-[34px] min-w-[34px] items-center justify-center border border-accent bg-accent px-2 font-mono text-[12.5px] font-bold tabular-nums text-accent-foreground"
          >
            {item}
          </button>
        ) : (
          <button
            key={item}
            type="button"
            aria-label={`Page ${item}`}
            disabled={disabled}
            onClick={() => onChange(item)}
            className={baseBtn}
          >
            {item}
          </button>
        ),
      )}

      <button
        type="button"
        aria-label="Next page"
        disabled={disabled || current >= total}
        onClick={() => onChange(current + 1)}
        className={baseBtn}
      >
        <span className="mr-1 hidden sm:inline">Next</span>
        <ChevronRight aria-hidden="true" strokeWidth={1.75} className="h-3.5 w-3.5" />
      </button>
    </nav>
  );
}

type PagerItem = number | "ellipsis";

/**
 * Builds the visible page-number sequence. Rules (per the handoff):
 *  • Always show pages 1, 2, last-1, last, and current ±1.
 *  • Collapse runs of skipped pages into a single "ellipsis" sentinel.
 *  • For totals ≤ 7 just emit every page (no ellipses needed).
 */
function buildPagerItems(current: number, total: number): PagerItem[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const pages = new Set<number>([1, 2, total - 1, total, current - 1, current, current + 1]);
  const sorted = Array.from(pages)
    .filter((p) => p >= 1 && p <= total)
    .sort((a, b) => a - b);

  const out: PagerItem[] = [];
  for (let i = 0; i < sorted.length; i++) {
    out.push(sorted[i]);
    if (i < sorted.length - 1 && sorted[i + 1] - sorted[i] > 1) {
      out.push("ellipsis");
    }
  }
  return out;
}
