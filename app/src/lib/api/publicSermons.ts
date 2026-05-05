import { apiGet } from "@/lib/apiClient";
import type { SermonListItem, PublicSermon } from "@/lib/api/sermons";
import type { PagedResult } from "@/types/api";

export interface BookCount {
  bookValue: number;
  slug: string;
  name: string;
  testament: "OldTestament" | "NewTestament";
  count: number;
}

export interface PublicSermonsQuery {
  search?: string;
  sermonSeriesId?: string;
  tagSlug?: string;
  bookFilter?: number;
  page?: number;
  pageSize?: number;
}

function buildQuery(q: PublicSermonsQuery): string {
  const usp = new URLSearchParams();
  if (q.search) usp.set("search", q.search);
  if (q.sermonSeriesId) usp.set("sermonSeriesId", q.sermonSeriesId);
  if (q.tagSlug) usp.set("tagSlug", q.tagSlug);
  if (q.bookFilter !== undefined) usp.set("bookFilter", String(q.bookFilter));
  if (q.page) usp.set("page", String(q.page));
  if (q.pageSize) usp.set("pageSize", String(q.pageSize));
  const s = usp.toString();
  return s ? `?${s}` : "";
}

export const publicSermonsApi = {
  list: (query: PublicSermonsQuery = {}) =>
    apiGet<PagedResult<SermonListItem>>(`/api/public/sermons${buildQuery(query)}`,
      { emitUnauthorized: false }),
  get: (slug: string) =>
    apiGet<PublicSermon>(`/api/public/sermons/${encodeURIComponent(slug)}`,
      { emitUnauthorized: false }),
  byBookIndex: () => apiGet<BookCount[]>("/api/public/sermons/by-book", { emitUnauthorized: false }),
  byBook: (bookSlug: string, page = 1, pageSize = 25) =>
    apiGet<PagedResult<SermonListItem>>(
      `/api/public/sermons/by-book/${encodeURIComponent(bookSlug)}?page=${page}&pageSize=${pageSize}`,
      { emitUnauthorized: false }),
};
