import { useCallback, useMemo } from "react";
import { useSearchParams } from "react-router-dom";

/**
 * Generic URL-state hook for paginated admin tables. Persists `page`, `size`,
 * `q`, and `sort` in the route's query string so refresh / back / forward
 * restore the exact list state.
 *
 * Behavior baked in (per Claude Design's spec):
 *  • Defaults are omitted from the URL (e.g. `?` stays clean while on page 1).
 *  • Changing `q`, `sort`, or `size` resets `page` to 1 — never strand the
 *    user on "page 7" of a result set that just shrank to 2 pages.
 *
 * Reusable across Sermons, Members, Events, News — pass per-table defaults
 * via the options argument.
 */
export interface TableQueryOptions {
  defaultPageSize?: number;
  defaultSort?: string;
}

export interface TableQueryState {
  page: number;
  pageSize: number;
  q: string;
  sort: string;
  setPage: (p: number) => void;
  setPageSize: (s: number) => void;
  setQuery: (q: string) => void;
  setSort: (s: string) => void;
}

export function useTableQuery(options: TableQueryOptions = {}): TableQueryState {
  const defaultPageSize = options.defaultPageSize ?? 50;
  const defaultSort = options.defaultSort ?? "date:desc";
  const [params, setParams] = useSearchParams();

  const page = Math.max(1, parseInt(params.get("page") ?? "1", 10) || 1);
  const pageSize = parseInt(params.get("size") ?? String(defaultPageSize), 10) || defaultPageSize;
  const q = params.get("q") ?? "";
  const sort = params.get("sort") ?? defaultSort;

  const updateParams = useCallback(
    (mutate: (next: URLSearchParams) => void) => {
      const next = new URLSearchParams(params);
      mutate(next);
      setParams(next, { replace: false });
    },
    [params, setParams],
  );

  const setPage = useCallback(
    (p: number) => {
      updateParams((next) => {
        if (p <= 1) next.delete("page");
        else next.set("page", String(p));
      });
    },
    [updateParams],
  );

  const setPageSize = useCallback(
    (s: number) => {
      updateParams((next) => {
        if (s === defaultPageSize) next.delete("size");
        else next.set("size", String(s));
        next.delete("page");
      });
    },
    [updateParams, defaultPageSize],
  );

  const setQuery = useCallback(
    (newQ: string) => {
      updateParams((next) => {
        if (newQ.length === 0) next.delete("q");
        else next.set("q", newQ);
        next.delete("page");
      });
    },
    [updateParams],
  );

  const setSort = useCallback(
    (s: string) => {
      updateParams((next) => {
        if (s === defaultSort) next.delete("sort");
        else next.set("sort", s);
        next.delete("page");
      });
    },
    [updateParams, defaultSort],
  );

  return useMemo(
    () => ({ page, pageSize, q, sort, setPage, setPageSize, setQuery, setSort }),
    [page, pageSize, q, sort, setPage, setPageSize, setQuery, setSort],
  );
}
