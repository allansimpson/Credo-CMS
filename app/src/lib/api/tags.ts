import { apiGet } from "@/lib/apiClient";

export interface TagDto {
  id: string;
  name: string;
  slug: string;
  usageCount: number;
}

export const tagsApi = {
  search: (q: string, limit = 20) =>
    apiGet<TagDto[]>(`/api/admin/tags/search?q=${encodeURIComponent(q)}&limit=${limit}`),
};
