import { useMemo, useState, type ReactNode } from "react";
import { ChevronLeft, ChevronRight, Search } from "lucide-react";
import { useBreakpoint } from "@/hooks/useBreakpoint";
import { cn } from "@/lib/utils";

export interface ColumnDef<TRow> {
  /** Stable identifier; used for sort state. */
  id: string;
  /** Header label. */
  header: string;
  /** Returns the cell's display value (string, number, or React node). */
  accessor: (row: TRow) => ReactNode;
  /** Optional comparator for sorting. Defaults to a string-coerced comparator on the accessor result. */
  sortBy?: (row: TRow) => string | number | null | undefined;
  /**
   * Mobile-card-priority hint. Columns with a number render on mobile cards in
   * ascending priority order; columns without a value are omitted from cards.
   */
  mobilePriority?: number;
  /** Hidden on a given breakpoint; CSS class applied to the cell wrapper. */
  className?: string;
}

interface ResponsiveTableProps<TRow> {
  data: TRow[];
  columns: ColumnDef<TRow>[];
  /** Stable key extractor. */
  rowKey: (row: TRow) => string;
  pageSize?: number;
  /** When provided, an input above the table filters by accessor text values. */
  searchable?: boolean;
  searchPlaceholder?: string;
  emptyMessage?: string;
  onRowClick?: (row: TRow) => void;
}

export function ResponsiveTable<TRow>({
  data,
  columns,
  rowKey,
  pageSize = 25,
  searchable = true,
  searchPlaceholder = "Search…",
  emptyMessage = "Nothing to show.",
  onRowClick,
}: ResponsiveTableProps<TRow>) {
  const breakpoint = useBreakpoint();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [sortId, setSortId] = useState<string | null>(null);
  const [sortAsc, setSortAsc] = useState(true);

  const filtered = useMemo(() => {
    if (!search.trim()) return data;
    const needle = search.trim().toLowerCase();
    return data.filter((row) =>
      columns.some((col) => {
        const v = col.accessor(row);
        return typeof v === "string" || typeof v === "number"
          ? String(v).toLowerCase().includes(needle)
          : false;
      }),
    );
  }, [data, columns, search]);

  const sorted = useMemo(() => {
    if (!sortId) return filtered;
    const col = columns.find((c) => c.id === sortId);
    if (!col) return filtered;
    const get = col.sortBy ?? ((r: TRow) => {
      const v = col.accessor(r);
      return typeof v === "string" || typeof v === "number" ? v : String(v ?? "");
    });
    const out = [...filtered];
    out.sort((a, b) => {
      const av = get(a);
      const bv = get(b);
      if (av == null && bv == null) return 0;
      if (av == null) return sortAsc ? -1 : 1;
      if (bv == null) return sortAsc ? 1 : -1;
      if (av < bv) return sortAsc ? -1 : 1;
      if (av > bv) return sortAsc ? 1 : -1;
      return 0;
    });
    return out;
  }, [filtered, sortId, sortAsc, columns]);

  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize));
  const safePage = Math.min(page, totalPages);
  const pageRows = sorted.slice((safePage - 1) * pageSize, safePage * pageSize);

  const mobileColumns = useMemo(
    () =>
      [...columns]
        .filter((c) => typeof c.mobilePriority === "number")
        .sort((a, b) => (a.mobilePriority ?? 0) - (b.mobilePriority ?? 0)),
    [columns],
  );

  const toggleSort = (id: string) => {
    if (sortId === id) {
      setSortAsc((prev) => !prev);
    } else {
      setSortId(id);
      setSortAsc(true);
    }
  };

  return (
    <div className="space-y-4">
      {searchable && (
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="search"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder={searchPlaceholder}
            className="h-10 w-full rounded-md border border-input bg-background pl-10 pr-3 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          />
        </div>
      )}

      {breakpoint === "mobile" ? (
        <div className="space-y-3">
          {pageRows.length === 0 ? (
            <div className="rounded-md border border-dashed p-6 text-center text-sm text-muted-foreground">
              {emptyMessage}
            </div>
          ) : (
            pageRows.map((row) => (
              <button
                key={rowKey(row)}
                type="button"
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                className={cn(
                  "block w-full rounded-lg border bg-card p-4 text-left shadow-sm",
                  onRowClick && "cursor-pointer hover:bg-muted",
                )}
              >
                <dl className="grid grid-cols-1 gap-2">
                  {mobileColumns.map((col) => (
                    <div key={col.id}>
                      <dt className="text-xs uppercase tracking-wide text-muted-foreground">
                        {col.header}
                      </dt>
                      <dd className="mt-0.5 text-sm">{col.accessor(row)}</dd>
                    </div>
                  ))}
                </dl>
              </button>
            ))
          )}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                {columns.map((col) => (
                  <th
                    key={col.id}
                    className={cn(
                      "px-4 py-2 text-left font-medium text-muted-foreground",
                      col.className,
                    )}
                  >
                    <button
                      type="button"
                      onClick={() => toggleSort(col.id)}
                      className="flex items-center gap-1 hover:text-foreground"
                    >
                      {col.header}
                      {sortId === col.id ? (sortAsc ? "▲" : "▼") : ""}
                    </button>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {pageRows.length === 0 ? (
                <tr>
                  <td colSpan={columns.length} className="p-6 text-center text-muted-foreground">
                    {emptyMessage}
                  </td>
                </tr>
              ) : (
                pageRows.map((row) => (
                  <tr
                    key={rowKey(row)}
                    className={cn(
                      "border-t",
                      onRowClick && "cursor-pointer hover:bg-muted",
                    )}
                    onClick={onRowClick ? () => onRowClick(row) : undefined}
                  >
                    {columns.map((col) => (
                      <td key={col.id} className={cn("px-4 py-3", col.className)}>
                        {col.accessor(row)}
                      </td>
                    ))}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>
            Page {safePage} of {totalPages} · {sorted.length} item{sorted.length === 1 ? "" : "s"}
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={safePage <= 1}
              className="inline-flex h-9 items-center rounded-md border bg-background px-3 disabled:opacity-50"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={safePage >= totalPages}
              className="inline-flex h-9 items-center rounded-md border bg-background px-3 disabled:opacity-50"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
