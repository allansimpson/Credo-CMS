import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type {
  CreateUserRequest,
  HardDeleteUserRequest,
  PagedResult,
  UpdateUserRequest,
  UserDetail,
  UserListItem,
} from "@/types/api";

export interface UserListQuery {
  search?: string;
  role?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

function toQuery(q: UserListQuery): string {
  const params = new URLSearchParams();
  if (q.search) params.set("search", q.search);
  if (q.role) params.set("role", q.role);
  if (q.isActive !== undefined) params.set("isActive", String(q.isActive));
  if (q.page !== undefined) params.set("page", String(q.page));
  if (q.pageSize !== undefined) params.set("pageSize", String(q.pageSize));
  return params.size > 0 ? `?${params.toString()}` : "";
}

export const usersApi = {
  list: (q: UserListQuery = {}) =>
    apiGet<PagedResult<UserListItem>>(`/api/admin/users${toQuery(q)}`),

  get: (id: string) => apiGet<UserDetail>(`/api/admin/users/${id}`),

  create: (req: CreateUserRequest) =>
    apiPost<UserDetail>("/api/admin/users", req),

  update: (id: string, req: UpdateUserRequest) =>
    apiPut<UserDetail>(`/api/admin/users/${id}`, req),

  deactivate: (id: string) =>
    apiPost<UserDetail>(`/api/admin/users/${id}/deactivate`),

  reactivate: (id: string) =>
    apiPost<UserDetail>(`/api/admin/users/${id}/reactivate`),

  forceLogout: (id: string) =>
    apiPost<UserDetail>(`/api/admin/users/${id}/force-logout`),

  sendPasswordReset: (id: string) =>
    apiPost<UserDetail>(`/api/admin/users/${id}/send-password-reset`),

  hardDelete: (id: string, req: HardDeleteUserRequest) =>
    apiDelete<void>(`/api/admin/users/${id}`, req),
};
