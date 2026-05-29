import { apiGet, apiPost } from "@/lib/apiClient";

export interface SearchResultItem {
  entityType: string;
  entityId: string;
  title: string;
  snippet: string;
  url: string;
  isMembersOnly: boolean;
}

export interface SearchResults {
  items: SearchResultItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const searchApi = {
  search: (q: string, page = 1, pageSize = 20) => {
    const usp = new URLSearchParams({ q, page: String(page), pageSize: String(pageSize) });
    return apiGet<SearchResults>(`/api/public/search?${usp}`, { emitUnauthorized: false });
  },
  /** Admin global search — includes drafts and unpublished entries. */
  searchAdmin: (q: string, page = 1, pageSize = 10) => {
    const usp = new URLSearchParams({ q, page: String(page), pageSize: String(pageSize) });
    return apiGet<SearchResults>(`/api/admin/search?${usp}`);
  },
  rebuild: () => apiPost<void>("/api/admin/search/rebuild"),
};
