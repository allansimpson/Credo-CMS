import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";

export const ConnectCardStatus = {
  New: 0,
  FollowUpNeeded: 1,
  FollowedUp: 2,
  Closed: 3,
  NotLegit: 4,
} as const;
export type ConnectCardStatus = (typeof ConnectCardStatus)[keyof typeof ConnectCardStatus];

export interface SubmitConnectCardRequest {
  name: string;
  email: string | null;
  phone: string | null;
  isFirstTimeVisitor: boolean;
  serviceDate: string | null;
  howDidYouHear: string;
  comments: string | null;
  interests: string[] | null;
  honeypotValue: string | null;
  clientLoadedAt: string | null;
  turnstileToken: string | null;
}

export interface SubmitConnectCardResult {
  ok: boolean;
  errors: string[] | null;
}

export interface AdminConnectCardListItem {
  id: string;
  name: string;
  email: string | null;
  phone: string | null;
  isFirstTimeVisitor: boolean;
  serviceDate: string | null;
  status: ConnectCardStatus;
  submittedAt: string;
  acknowledgmentEmailSentAt: string | null;
}

export interface AdminConnectCardDetail {
  id: string;
  name: string;
  email: string | null;
  phone: string | null;
  isFirstTimeVisitor: boolean;
  serviceDate: string | null;
  howDidYouHear: string;
  comments: string | null;
  interests: string[];
  status: ConnectCardStatus;
  adminNotes: string | null;
  submittedAt: string;
  acknowledgmentEmailSentAt: string | null;
  statusChangedAt: string | null;
}

export interface AdminConnectCardQuery {
  status?: ConnectCardStatus;
  isFirstTimeVisitor?: boolean;
  search?: string;
}

export const publicConnectCardApi = {
  submit: (req: SubmitConnectCardRequest) =>
    apiPost<SubmitConnectCardResult>("/api/public/connect-card", req),
};

export const adminConnectCardApi = {
  list: (q: AdminConnectCardQuery = {}) => {
    const params = new URLSearchParams();
    if (q.status !== undefined) params.set("status", String(q.status));
    if (q.isFirstTimeVisitor !== undefined) params.set("isFirstTimeVisitor", String(q.isFirstTimeVisitor));
    if (q.search) params.set("search", q.search);
    return apiGet<AdminConnectCardListItem[]>(
      `/api/admin/connect-cards${params.size ? `?${params.toString()}` : ""}`,
    );
  },
  get: (id: string) => apiGet<AdminConnectCardDetail>(`/api/admin/connect-cards/${id}`),
  updateStatus: (id: string, status: ConnectCardStatus) =>
    apiPut<AdminConnectCardDetail>(`/api/admin/connect-cards/${id}/status`, { status }),
  updateNotes: (id: string, adminNotes: string | null) =>
    apiPut<AdminConnectCardDetail>(`/api/admin/connect-cards/${id}/notes`, { adminNotes }),
  resend: (id: string) => apiPost<void>(`/api/admin/connect-cards/${id}/resend`),
  delete: (id: string) => apiDelete<void>(`/api/admin/connect-cards/${id}`),
};
