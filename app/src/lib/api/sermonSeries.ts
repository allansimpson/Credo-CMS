import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type { ScriptureReference } from "@/lib/bible/scripture";
import type { PagedResult } from "@/types/api";

export interface ScriptureReferenceDto extends ScriptureReference {
  id: string;
  displayOrder: number;
}

export interface ScriptureReferenceInputDto {
  book: number;
  chapterStart: number;
  verseStart: number | null;
  chapterEnd: number | null;
  verseEnd: number | null;
}

export interface SermonSeriesListItem {
  id: string;
  slug: string;
  title: string;
  bannerImageUrl: string | null;
  bannerImageWebpUrl: string | null;
  bannerImageAlt: string | null;
  startDate: string; // ISO date
  endDate: string | null;
  isDeleted: boolean;
  modifiedAt: string;
}

export interface SermonSeriesDetail {
  id: string;
  slug: string;
  title: string;
  descriptionJson: string | null;
  bannerImageUrl: string | null;
  bannerImageWebpUrl: string | null;
  bannerImageAlt: string | null;
  startDate: string;
  endDate: string | null;
  isDeleted: boolean;
  scriptureReferences: ScriptureReferenceDto[];
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
  deletedAt: string | null;
}

export interface PublicSermonSeries {
  id: string;
  slug: string;
  title: string;
  descriptionJson: string | null;
  bannerImageUrl: string | null;
  bannerImageWebpUrl: string | null;
  bannerImageAlt: string | null;
  startDate: string;
  endDate: string | null;
  scriptureReferences: ScriptureReferenceDto[];
}

export interface SermonSeriesRequest {
  slug: string;
  title: string;
  descriptionJson: string | null;
  bannerImageUrl: string | null;
  bannerImageWebpUrl: string | null;
  bannerImageAlt: string | null;
  startDate: string;
  endDate: string | null;
  scriptureReferences: ScriptureReferenceInputDto[];
}

export interface SermonSeriesListQuery {
  includeDeleted?: boolean;
  page?: number;
  pageSize?: number;
}

function buildQuery(q: SermonSeriesListQuery): string {
  const usp = new URLSearchParams();
  if (q.includeDeleted) usp.set("includeDeleted", "true");
  if (q.page) usp.set("page", String(q.page));
  if (q.pageSize) usp.set("pageSize", String(q.pageSize));
  const s = usp.toString();
  return s ? `?${s}` : "";
}

export const sermonSeriesApi = {
  list: (query: SermonSeriesListQuery = {}) =>
    apiGet<PagedResult<SermonSeriesListItem>>(`/api/admin/sermon-series${buildQuery(query)}`),
  get: (id: string) => apiGet<SermonSeriesDetail>(`/api/admin/sermon-series/${id}`),
  create: (req: SermonSeriesRequest) => apiPost<SermonSeriesDetail>("/api/admin/sermon-series", req),
  update: (id: string, req: SermonSeriesRequest) => apiPut<SermonSeriesDetail>(`/api/admin/sermon-series/${id}`, req),
  softDelete: (id: string) => apiDelete<void>(`/api/admin/sermon-series/${id}`),
  restore: (id: string) => apiPost<SermonSeriesDetail>(`/api/admin/sermon-series/${id}/restore`),
  hardDelete: (id: string) => apiDelete<void>(`/api/admin/sermon-series/${id}/hard`),

  listPublic: () => apiGet<PublicSermonSeries[]>("/api/public/sermons/series", { emitUnauthorized: false }),
  getPublic: (slug: string) =>
    apiGet<PublicSermonSeries>(`/api/public/sermons/series/${encodeURIComponent(slug)}`,
      { emitUnauthorized: false }),
};
