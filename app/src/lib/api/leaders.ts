import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type {
  CreateLeaderRequest, Leader, PublicLeader, UpdateLeaderRequest,
} from "@/types/api";

export const leadersApi = {
  list: () => apiGet<Leader[]>("/api/admin/leaders"),
  get: (id: string) => apiGet<Leader>(`/api/admin/leaders/${id}`),
  create: (req: CreateLeaderRequest) => apiPost<Leader>("/api/admin/leaders", req),
  update: (id: string, req: UpdateLeaderRequest) => apiPut<Leader>(`/api/admin/leaders/${id}`, req),
  delete: (id: string) => apiDelete<void>(`/api/admin/leaders/${id}`),
  listPublic: () => apiGet<PublicLeader[]>("/api/public/leaders", { emitUnauthorized: false }),
  getPublic: (id: string) => apiGet<PublicLeader>(`/api/public/leaders/${id}`, { emitUnauthorized: false }),
};
