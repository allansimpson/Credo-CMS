import { apiGet } from "@/lib/apiClient";
import type { AuditLogEntry, PagedResult } from "@/types/api";

export interface AuditLogQuery {
  fromUtc?: string;
  toUtc?: string;
  userId?: string;
  action?: string;
  entityType?: string;
  page?: number;
  pageSize?: number;
}

function toQuery(q: AuditLogQuery): string {
  const params = new URLSearchParams();
  if (q.fromUtc) params.set("fromUtc", q.fromUtc);
  if (q.toUtc) params.set("toUtc", q.toUtc);
  if (q.userId) params.set("userId", q.userId);
  if (q.action) params.set("action", q.action);
  if (q.entityType) params.set("entityType", q.entityType);
  if (q.page !== undefined) params.set("page", String(q.page));
  if (q.pageSize !== undefined) params.set("pageSize", String(q.pageSize));
  return params.size > 0 ? `?${params.toString()}` : "";
}

export const auditLogApi = {
  list: (q: AuditLogQuery = {}) =>
    apiGet<PagedResult<AuditLogEntry>>(`/api/admin/audit-log${toQuery(q)}`),

  get: (id: string) => apiGet<AuditLogEntry>(`/api/admin/audit-log/${id}`),
};
