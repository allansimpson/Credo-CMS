import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type { PagedResult } from "@/types/api";

export interface BlogPostListItem {
  id: string;
  slug: string;
  title: string;
  excerpt: string | null;
  heroImageBlobUrl: string | null;
  heroImageWebpBlobUrl: string | null;
  heroImageAltText: string | null;
  category: string;
  authorDisplayName: string;
  isPublished: boolean;
  isMembersOnly: boolean;
  isPinned: boolean;
  publishedAt: string | null;
  readingTimeMinutes: number;
  modifiedAt: string;
}

export interface BlogPostDetail {
  id: string;
  slug: string;
  title: string;
  bodyJson: string;
  excerpt: string | null;
  heroImageBlobUrl: string | null;
  heroImageWebpBlobUrl: string | null;
  heroImageAltText: string | null;
  category: string;
  authorUserId: string;
  authorDisplayName: string;
  relatedSermonId: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  isPinned: boolean;
  publishedAt: string | null;
  scheduledPublishAt: string | null;
  readingTimeMinutes: number;
  metaDescription: string | null;
  tags: string[];
  createdAt: string;
  modifiedAt: string;
}

export interface CreateBlogPostRequest {
  slug: string;
  title: string;
  bodyJson: string;
  excerpt: string | null;
  heroImageBlobUrl: string | null;
  heroImageWebpBlobUrl: string | null;
  heroImageAltText: string | null;
  category: string;
  relatedSermonId: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  isPinned: boolean;
  publishedAt: string | null;
  scheduledPublishAt: string | null;
  metaDescription: string | null;
  tags: string[] | null;
}
export type UpdateBlogPostRequest = CreateBlogPostRequest;

export interface AdminBlogQuery {
  search?: string;
  category?: string;
  authorUserId?: string;
  isPublished?: boolean;
  includeDeleted?: boolean;
  page?: number;
  pageSize?: number;
}

export const publicBlogApi = {
  list: (category?: string, page = 1, pageSize = 12) => {
    const params = new URLSearchParams();
    if (category) params.set("category", category);
    params.set("page", String(page));
    params.set("pageSize", String(pageSize));
    return apiGet<PagedResult<BlogPostListItem>>(`/api/public/blog?${params.toString()}`);
  },
  get: (slug: string) => apiGet<BlogPostDetail>(`/api/public/blog/${encodeURIComponent(slug)}`),
  byAuthor: (userId: string) => apiGet<BlogPostListItem[]>(`/api/public/blog/authors/${userId}`),
};

export const adminBlogApi = {
  list: (q: AdminBlogQuery = {}) => {
    const params = new URLSearchParams();
    if (q.search) params.set("search", q.search);
    if (q.category) params.set("category", q.category);
    if (q.authorUserId) params.set("authorUserId", q.authorUserId);
    if (q.isPublished !== undefined) params.set("isPublished", String(q.isPublished));
    if (q.includeDeleted) params.set("includeDeleted", "true");
    params.set("page", String(q.page ?? 1));
    params.set("pageSize", String(q.pageSize ?? 20));
    return apiGet<PagedResult<BlogPostListItem>>(`/api/admin/blog?${params.toString()}`);
  },
  get: (id: string) => apiGet<BlogPostDetail>(`/api/admin/blog/${id}`),
  create: (req: CreateBlogPostRequest) => apiPost<BlogPostDetail>("/api/admin/blog", req),
  update: (id: string, req: UpdateBlogPostRequest) => apiPut<BlogPostDetail>(`/api/admin/blog/${id}`, req),
  softDelete: (id: string) => apiDelete<void>(`/api/admin/blog/${id}`),
};
