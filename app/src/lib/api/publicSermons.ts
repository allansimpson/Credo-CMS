import { apiGet } from "@/lib/apiClient";
import type { SermonListItem, PublicSermon, ServiceType } from "@/lib/api/sermons";
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

export interface ServiceDay {
  date: string;
  dayOfWeek: number;
  kind: "sunday" | "wednesday" | "other";
  sermons: SermonListItem[];
}

export interface YearStats {
  year: number;
  count: number;
  /** Three-letter lowercase month slug → count. Months with zero entries
   * are omitted. Sender: backend `YearStatsDto`. */
  monthCounts: Record<string, number>;
}

export interface YearsResponse {
  /** Year of the most recently published sermon — drives the /sermons redirect. */
  currentYear: number;
  /** Sorted descending. */
  years: YearStats[];
}

export interface SermonsByDayResponse {
  days: ServiceDay[];
  page: number;
  pageSize: number;
  totalDays: number;
  totalPages: number;
  /** Populated when a filter (search OR tag) is active. Drives the side-rail's
   * rescoped match counts in those modes. Null/undefined in unfiltered
   * browse mode — use `/years` for the comprehensive year list. */
  yearStats?: YearStats[] | null;
}

export interface SermonsByDayQuery {
  search?: string;
  tagSlug?: string;
  serviceType?: ServiceType;
  /** Calendar year filter. Ignored by the backend when `search` is set —
   * search exits year-browse entirely. */
  year?: number;
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

function buildDayQuery(q: SermonsByDayQuery): string {
  const usp = new URLSearchParams();
  if (q.search) usp.set("search", q.search);
  if (q.tagSlug) usp.set("tagSlug", q.tagSlug);
  if (q.serviceType) usp.set("serviceType", q.serviceType);
  if (q.year) usp.set("year", String(q.year));
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
  byDay: (query: SermonsByDayQuery = {}) =>
    apiGet<SermonsByDayResponse>(`/api/public/sermons/by-day${buildDayQuery(query)}`,
      { emitUnauthorized: false }),
  years: () =>
    apiGet<YearsResponse>("/api/public/sermons/years", { emitUnauthorized: false }),
  byBookIndex: () => apiGet<BookCount[]>("/api/public/sermons/by-book", { emitUnauthorized: false }),
  byBook: (bookSlug: string, page = 1, pageSize = 25) =>
    apiGet<PagedResult<SermonListItem>>(
      `/api/public/sermons/by-book/${encodeURIComponent(bookSlug)}?page=${page}&pageSize=${pageSize}`,
      { emitUnauthorized: false }),
};
