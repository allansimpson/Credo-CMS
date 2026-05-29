import { ArrowUp } from "lucide-react";
import { PageNumbers } from "./PageNumbers";
import { PageSizeSelect } from "./PageSizeSelect";

/**
 * Sticky bottom-pinned pager bar for paginated admin tables. Three-column
 * grid: result count + "↑ Top" on the left, numbered pager centered, per-
 * page selector right-justified. Soft drop-shadow lifts it off the table.
 *
 * Generic — accepts any paged-result shape via the `page`, `pageSize`,
 * `total`, `totalPages` props. Reusable across Sermons / Members / Events /
 * News once those tables get the same treatment.
 *
 * The "showing X–Y of Z" range is computed from page/pageSize/total. When
 * `total ≤ pageSize` the numbered pager hides automatically (the result
 * count + size selector still render).
 */
export interface StickyTablePagerProps {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  /** Optional handler for the "↑ Top" link. Pass the function that scrolls
   * the table body to its top. Hidden if omitted. */
  onScrollToTop?: () => void;
  /** Search-mode suffix on the count line: "Showing 1–3 of 3 · for '5/3/2026'" */
  query?: string;
  disabled?: boolean;
  pageSizeOptions?: readonly number[];
}

export function StickyTablePager({
  page,
  pageSize,
  total,
  totalPages,
  onPageChange,
  onPageSizeChange,
  onScrollToTop,
  query,
  disabled = false,
  pageSizeOptions,
}: StickyTablePagerProps) {
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  return (
    <footer
      className="grid shrink-0 grid-cols-[1fr_auto_1fr] items-center gap-6 border-t border-border bg-panel px-5 py-3.5 shadow-[0_-4px_18px_rgba(26,24,21,0.07)] lg:px-8"
    >
      {/* Left: result count + Top link */}
      <div className="flex items-center gap-4">
        <p className="font-mono text-[11.5px] tabular-nums text-muted">
          {total === 0 ? (
            <span>Showing 0 of 0</span>
          ) : (
            <>
              Showing{" "}
              <span className="font-semibold text-foreground">{from.toLocaleString()}</span>
              –
              <span className="font-semibold text-foreground">{to.toLocaleString()}</span>{" "}
              of <span className="font-semibold text-foreground">{total.toLocaleString()}</span>
            </>
          )}
          {query && (
            <span className="ml-1.5">· for &ldquo;{query}&rdquo;</span>
          )}
        </p>
        {onScrollToTop && (
          <button
            type="button"
            onClick={onScrollToTop}
            className="inline-flex items-center gap-1 font-mono text-[11px] uppercase tracking-[0.12em] text-muted hover:text-foreground"
          >
            <ArrowUp aria-hidden="true" strokeWidth={1.75} className="h-3.5 w-3.5 translate-y-px" />
            Top
          </button>
        )}
      </div>

      {/* Center: numbered pager */}
      <div className="justify-self-center">
        <PageNumbers
          current={page}
          total={totalPages}
          onChange={onPageChange}
          disabled={disabled}
        />
      </div>

      {/* Right: per-page selector */}
      <div className="justify-self-end">
        <PageSizeSelect
          value={pageSize}
          onChange={onPageSizeChange}
          options={pageSizeOptions}
          disabled={disabled}
        />
      </div>
    </footer>
  );
}
