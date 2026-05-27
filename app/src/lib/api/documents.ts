import { apiDelete, apiGet, apiPost, apiPut, apiUpload } from "@/lib/apiClient";
import type {
  DocumentDto, PublicDocument, UpdateDocumentMetadataRequest,
} from "@/types/api";

export const documentsApi = {
  list: (category?: string, includeDeleted = false) => {
    const usp = new URLSearchParams();
    if (category) usp.set("category", category);
    if (includeDeleted) usp.set("includeDeleted", "true");
    const s = usp.toString();
    return apiGet<DocumentDto[]>(`/api/admin/documents${s ? `?${s}` : ""}`);
  },
  get: (id: string) => apiGet<DocumentDto>(`/api/admin/documents/${id}`),
  upload: (file: File, fields: {
    title: string; category: string; description: string;
    isPublished: boolean; isMembersOnly: boolean;
  }) => {
    const form = new FormData();
    form.append("file", file);
    form.append("title", fields.title);
    form.append("category", fields.category);
    if (fields.description) form.append("description", fields.description);
    form.append("isPublished", String(fields.isPublished));
    form.append("isMembersOnly", String(fields.isMembersOnly));
    return apiUpload<DocumentDto>("/api/admin/documents/upload", form);
  },
  updateMetadata: (id: string, req: UpdateDocumentMetadataRequest) =>
    apiPut<DocumentDto>(`/api/admin/documents/${id}/metadata`, req),
  replace: (id: string, file: File) => {
    const form = new FormData();
    form.append("file", file);
    return apiUpload<DocumentDto>(`/api/admin/documents/${id}/replace`, form);
  },
  softDelete: (id: string) => apiDelete<void>(`/api/admin/documents/${id}`),
  restore: (id: string) => apiPost<DocumentDto>(`/api/admin/documents/${id}/restore`),
  hardDelete: (id: string) => apiDelete<void>(`/api/admin/documents/${id}/hard`),

  listPublic: () => apiGet<PublicDocument[]>("/api/public/documents", { emitUnauthorized: false }),
};

/** Public streaming URL for an in-browser <embed>/<iframe> preview. */
export function publicDocumentFileUrl(id: string): string {
  return `/api/public/documents/${id}/file`;
}
