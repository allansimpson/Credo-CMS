import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type {
  CreateNewsItemRequest,
  NewsDetail,
  NewsListItem,
  PagedResult,
  PublicNewsDetail,
  PublicNewsItem,
  UpdateNewsItemRequest,
} from "@/types/api";

export interface NewsListQuery {
  search?: string;
  includeDeleted?: boolean;
  page?: number;
  pageSize?: number;
}

function buildQuery(q: NewsListQuery): string {
  const usp = new URLSearchParams();
  if (q.search) usp.set("search", q.search);
  if (q.includeDeleted) usp.set("includeDeleted", "true");
  if (q.page) usp.set("page", String(q.page));
  if (q.pageSize) usp.set("pageSize", String(q.pageSize));
  const s = usp.toString();
  return s ? `?${s}` : "";
}

export const newsApi = {
  list: (query: NewsListQuery = {}) =>
    apiGet<PagedResult<NewsListItem>>(`/api/admin/news${buildQuery(query)}`),
  get: (id: string) => apiGet<NewsDetail>(`/api/admin/news/${id}`),
  create: (req: CreateNewsItemRequest) => apiPost<NewsDetail>("/api/admin/news", req),
  update: (id: string, req: UpdateNewsItemRequest) =>
    apiPut<NewsDetail>(`/api/admin/news/${id}`, req),
  softDelete: (id: string) => apiDelete<void>(`/api/admin/news/${id}`),
  restore: (id: string) => apiPost<NewsDetail>(`/api/admin/news/${id}/restore`),
  hardDelete: (id: string) => apiDelete<void>(`/api/admin/news/${id}/hard`),

  // Public
  listPublic: (page = 1, pageSize = 10) =>
    apiGet<PagedResult<PublicNewsItem>>(`/api/public/news?page=${page}&pageSize=${pageSize}`,
      { emitUnauthorized: false }),
  getPublic: (slug: string) =>
    apiGet<PublicNewsDetail>(`/api/public/news/${encodeURIComponent(slug)}`,
      { emitUnauthorized: false }),
};
