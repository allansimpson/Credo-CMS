import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";

// ---- enum mirrors -------------------------------------------------------

export const PrayerRequestStatus = {
  Active: 0,
  Answered: 1,
  Archived: 2,
} as const;
export type PrayerRequestStatus = (typeof PrayerRequestStatus)[keyof typeof PrayerRequestStatus];

// ---- DTOs ---------------------------------------------------------------

export interface PrayerRequestUpdate {
  id: string;
  prayerRequestId: string;
  bodyJson: string;
  postedByLabel: string;
  createdAt: string;
}

export interface PrayerRequestListItem {
  id: string;
  title: string;
  bodyJson: string;
  submitterDisplayName: string | null;
  isAnonymous: boolean;
  status: PrayerRequestStatus;
  createdAt: string;
  prayedForCount: number;
  viewerHasPrayed: boolean;
  viewerCanEdit: boolean;
  updateCount: number;
}

export interface MemberPrayerRequest {
  id: string;
  title: string;
  bodyJson: string;
  submitterDisplayName: string | null;
  isAnonymous: boolean;
  status: PrayerRequestStatus;
  createdAt: string;
  prayedForCount: number;
  viewerHasPrayed: boolean;
  viewerCanEdit: boolean;
  updates: PrayerRequestUpdate[];
}

export interface AdminPrayerRequest {
  id: string;
  title: string;
  bodyJson: string;
  submittedByUserId: string;
  submitterDisplayName: string;
  isAnonymous: boolean;
  status: PrayerRequestStatus;
  createdAt: string;
  modifiedAt: string;
  prayedForCount: number;
  updates: PrayerRequestUpdate[];
}

export interface SubmitPrayerRequestRequest {
  title: string;
  bodyJson: string;
  isAnonymous: boolean;
}

export interface EditPrayerRequestRequest {
  title: string;
  bodyJson: string;
  isAnonymous: boolean;
}

export interface AddPrayerUpdateRequest {
  bodyJson: string;
}

export interface ChangePrayerStatusRequest {
  status: PrayerRequestStatus;
}

// ---- API surface --------------------------------------------------------

export const memberPrayerApi = {
  list: () => apiGet<PrayerRequestListItem[]>("/api/prayer-requests"),
  get: (id: string) => apiGet<MemberPrayerRequest>(`/api/prayer-requests/${id}`),
  submit: (req: SubmitPrayerRequestRequest) =>
    apiPost<{ id: string }>("/api/prayer-requests", req),
  edit: (id: string, req: EditPrayerRequestRequest) =>
    apiPut<{ id: string }>(`/api/prayer-requests/${id}`, req),
  delete: (id: string) => apiDelete<void>(`/api/prayer-requests/${id}`),
  markPrayed: (id: string) =>
    apiPost<{ count: number }>(`/api/prayer-requests/${id}/prayed`),
  unmarkPrayed: (id: string) =>
    apiDelete<{ count: number }>(`/api/prayer-requests/${id}/prayed`),
};

export interface AdminPrayerListQuery {
  status?: PrayerRequestStatus;
  isAnonymous?: boolean;
  search?: string;
}

export const adminPrayerApi = {
  list: (q: AdminPrayerListQuery = {}) => {
    const params = new URLSearchParams();
    if (q.status !== undefined) params.set("status", String(q.status));
    if (q.isAnonymous !== undefined) params.set("isAnonymous", String(q.isAnonymous));
    if (q.search) params.set("search", q.search);
    return apiGet<AdminPrayerRequest[]>(
      `/api/admin/prayer-requests${params.size ? `?${params.toString()}` : ""}`,
    );
  },
  get: (id: string) => apiGet<AdminPrayerRequest>(`/api/admin/prayer-requests/${id}`),
  addUpdate: (id: string, req: AddPrayerUpdateRequest) =>
    apiPost<AdminPrayerRequest>(`/api/admin/prayer-requests/${id}/updates`, req),
  changeStatus: (id: string, req: ChangePrayerStatusRequest) =>
    apiPut<AdminPrayerRequest>(`/api/admin/prayer-requests/${id}/status`, req),
  bulkArchive: (ids: string[]) =>
    apiPost<{ count: number }>(`/api/admin/prayer-requests/bulk-archive`, { ids }),
  delete: (id: string) => apiDelete<void>(`/api/admin/prayer-requests/${id}`),
};
