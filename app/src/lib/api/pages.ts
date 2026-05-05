import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type {
  CreatePageRequest,
  PageDetail,
  PageListItem,
  PagedResult,
  PublicPage,
  UpdatePageRequest,
} from "@/types/api";

export interface PageListQuery {
  search?: string;
  includeDeleted?: boolean;
  page?: number;
  pageSize?: number;
}

function buildQuery(q: PageListQuery): string {
  const usp = new URLSearchParams();
  if (q.search) usp.set("search", q.search);
  if (q.includeDeleted) usp.set("includeDeleted", "true");
  if (q.page) usp.set("page", String(q.page));
  if (q.pageSize) usp.set("pageSize", String(q.pageSize));
  const s = usp.toString();
  return s ? `?${s}` : "";
}

export const pagesApi = {
  list: (query: PageListQuery = {}) =>
    apiGet<PagedResult<PageListItem>>(`/api/admin/pages${buildQuery(query)}`),

  get: (id: string) => apiGet<PageDetail>(`/api/admin/pages/${id}`),

  create: (req: CreatePageRequest) =>
    apiPost<PageDetail>("/api/admin/pages", req),

  update: (id: string, req: UpdatePageRequest) =>
    apiPut<PageDetail>(`/api/admin/pages/${id}`, req),

  softDelete: (id: string) => apiDelete<void>(`/api/admin/pages/${id}`),

  restore: (id: string) =>
    apiPost<PageDetail>(`/api/admin/pages/${id}/restore`),

  hardDelete: (id: string) =>
    apiDelete<void>(`/api/admin/pages/${id}/hard`),

  // Public
  listPublic: () => apiGet<PublicPage[]>("/api/public/pages", { emitUnauthorized: false }),

  getPublic: (slug: string) =>
    apiGet<PublicPage>(`/api/public/pages/${encodeURIComponent(slug)}`, { emitUnauthorized: false }),
};
