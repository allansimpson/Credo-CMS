import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type { ScriptureReferenceDto, ScriptureReferenceInputDto } from "@/lib/api/sermonSeries";
import type { TagDto } from "@/lib/api/tags";
import type { PagedResult } from "@/types/api";

export interface SermonListItem {
  id: string;
  slug: string;
  title: string;
  thumbnailBlobUrl: string | null;
  thumbnailWebpBlobUrl: string | null;
  publishedAt: string;
  isPublished: boolean;
  isMembersOnly: boolean;
  isDeleted: boolean;
  speakerName: string | null;
  sermonSeriesTitle: string | null;
  sermonSeriesId: string | null;
}

export interface SermonAttachmentDto {
  documentId: string;
  title: string;
  displayOrder: number;
}

export interface SermonDetail {
  id: string;
  slug: string;
  title: string;
  descriptionJson: string | null;
  youTubeVideoId: string;
  youTubeChannelId: string | null;
  thumbnailBlobUrl: string | null;
  thumbnailWebpBlobUrl: string | null;
  publishedAt: string;
  youTubePublishedAt: string;
  durationSeconds: number | null;
  transcript: string | null;
  transcriptSource: 0 | 1 | 2;
  speakerLeaderId: string | null;
  speakerNameFreeText: string | null;
  sermonSeriesId: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  isDeleted: boolean;
  tags: TagDto[];
  attachments: SermonAttachmentDto[];
  scriptureReferences: ScriptureReferenceDto[];
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
  deletedAt: string | null;
}

export interface PublicSermon {
  id: string;
  slug: string;
  title: string;
  descriptionJson: string | null;
  youTubeVideoId: string;
  thumbnailBlobUrl: string | null;
  thumbnailWebpBlobUrl: string | null;
  publishedAt: string;
  durationSeconds: number | null;
  transcript: string | null;
  speakerLeaderId: string | null;
  speakerName: string | null;
  sermonSeriesId: string | null;
  sermonSeriesTitle: string | null;
  sermonSeriesSlug: string | null;
  isMembersOnly: boolean;
  tags: TagDto[];
  attachments: SermonAttachmentDto[];
  scriptureReferences: ScriptureReferenceDto[];
}

export interface SermonTagInput {
  id: string | null;
  name: string;
}

export interface CreateSermonRequest {
  slug: string;
  title: string;
  descriptionJson: string | null;
  youTubeVideoId: string;
  youTubeChannelId: string | null;
  thumbnailBlobUrl: string | null;
  thumbnailWebpBlobUrl: string | null;
  publishedAt: string;
  youTubePublishedAt: string;
  durationSeconds: number | null;
  transcript: string | null;
  transcriptSource: 0 | 1 | 2;
  speakerLeaderId: string | null;
  speakerNameFreeText: string | null;
  sermonSeriesId: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  tags: SermonTagInput[];
  attachmentDocumentIds: string[];
  scriptureReferences: ScriptureReferenceInputDto[];
}

export interface UpdateSermonRequest {
  slug: string;
  title: string;
  descriptionJson: string | null;
  thumbnailBlobUrl: string | null;
  thumbnailWebpBlobUrl: string | null;
  publishedAt: string;
  transcript: string | null;
  transcriptSource: 0 | 1 | 2;
  speakerLeaderId: string | null;
  speakerNameFreeText: string | null;
  sermonSeriesId: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  tags: SermonTagInput[];
  attachmentDocumentIds: string[];
  scriptureReferences: ScriptureReferenceInputDto[];
}

export interface SermonListQuery {
  search?: string;
  sermonSeriesId?: string;
  tagSlug?: string;
  speakerLeaderId?: string;
  bookFilter?: number;
  publishedOnly?: boolean;
  includeDeleted?: boolean;
  page?: number;
  pageSize?: number;
}

function buildQuery(q: SermonListQuery): string {
  const usp = new URLSearchParams();
  if (q.search) usp.set("search", q.search);
  if (q.sermonSeriesId) usp.set("sermonSeriesId", q.sermonSeriesId);
  if (q.tagSlug) usp.set("tagSlug", q.tagSlug);
  if (q.speakerLeaderId) usp.set("speakerLeaderId", q.speakerLeaderId);
  if (q.bookFilter !== undefined) usp.set("bookFilter", String(q.bookFilter));
  if (q.publishedOnly !== undefined) usp.set("publishedOnly", String(q.publishedOnly));
  if (q.includeDeleted) usp.set("includeDeleted", "true");
  if (q.page) usp.set("page", String(q.page));
  if (q.pageSize) usp.set("pageSize", String(q.pageSize));
  const s = usp.toString();
  return s ? `?${s}` : "";
}

export const sermonsApi = {
  list: (query: SermonListQuery = {}) =>
    apiGet<PagedResult<SermonListItem>>(`/api/admin/sermons${buildQuery(query)}`),
  get: (id: string) => apiGet<SermonDetail>(`/api/admin/sermons/${id}`),
  create: (req: CreateSermonRequest) => apiPost<SermonDetail>("/api/admin/sermons", req),
  update: (id: string, req: UpdateSermonRequest) =>
    apiPut<SermonDetail>(`/api/admin/sermons/${id}`, req),
  softDelete: (id: string) => apiDelete<void>(`/api/admin/sermons/${id}`),
  restore: (id: string) => apiPost<SermonDetail>(`/api/admin/sermons/${id}/restore`),
  hardDelete: (id: string) => apiDelete<void>(`/api/admin/sermons/${id}/hard`),

  // Manual import + sync
  import: (urlOrVideoId: string) =>
    apiPost<SermonDetail>("/api/admin/sermons/import", { urlOrVideoId }),
  triggerSync: () => apiPost<void>("/api/admin/sermons/sync"),
};
