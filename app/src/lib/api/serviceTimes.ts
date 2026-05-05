import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type {
  CreateServiceTimeRequest,
  PublicServiceTime,
  ServiceTime,
  UpdateServiceTimeRequest,
} from "@/types/api";

export const serviceTimesApi = {
  list: (includeDeleted = false) =>
    apiGet<ServiceTime[]>(`/api/admin/service-times${includeDeleted ? "?includeDeleted=true" : ""}`),
  get: (id: string) => apiGet<ServiceTime>(`/api/admin/service-times/${id}`),
  create: (req: CreateServiceTimeRequest) =>
    apiPost<ServiceTime>("/api/admin/service-times", req),
  update: (id: string, req: UpdateServiceTimeRequest) =>
    apiPut<ServiceTime>(`/api/admin/service-times/${id}`, req),
  softDelete: (id: string) => apiDelete<void>(`/api/admin/service-times/${id}`),
  restore: (id: string) => apiPost<ServiceTime>(`/api/admin/service-times/${id}/restore`),
  hardDelete: (id: string) => apiDelete<void>(`/api/admin/service-times/${id}/hard`),
  listPublic: () => apiGet<PublicServiceTime[]>("/api/public/service-times", { emitUnauthorized: false }),
};
