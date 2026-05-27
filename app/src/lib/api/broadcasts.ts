import { apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type { PagedResult } from "@/types/api";

export type BroadcastStatus = 0 | 1 | 2 | 3 | 4 | 5;
export type BroadcastTargetMode = 0 | 1;
export type BroadcastSendMode = 0 | 1;
export type EmailCategory = 0 | 1 | 2 | 3 | 4;
export type RecipientStatus = 0 | 1 | 2 | 3 | 4 | 5;

export const BROADCAST_STATUS_LABELS: Record<BroadcastStatus, string> = {
  0: "Draft",
  1: "Scheduled",
  2: "Sending",
  3: "Sent",
  4: "Canceled",
  5: "Failed",
};

export const RECIPIENT_STATUS_LABELS: Record<RecipientStatus, string> = {
  0: "Pending",
  1: "Delivered",
  2: "Bounced",
  3: "Complained Spam",
  4: "Suppressed",
  5: "Failed",
};

export interface EmailBroadcast {
  id: string;
  subject: string;
  body: string;
  plainTextBody: string | null;
  targetMode: BroadcastTargetMode;
  targetGroupIdsJson: string | null;
  sendMode: BroadcastSendMode;
  scheduledSendAt: string | null;
  status: BroadcastStatus;
  sentAt: string | null;
  failureReason: string | null;
  recipientCountAtSend: number | null;
  deliveredCount: number;
  bouncedCount: number;
  complaintCount: number;
  openCount: number;
  category: EmailCategory;
  sourceEntityId: string | null;
  createdAt: string;
  modifiedAt: string;
}

export interface EmailBroadcastRecipient {
  id: string;
  broadcastId: string;
  userId: string | null;
  emailAddressSnapshot: string;
  displayNameSnapshot: string;
  status: RecipientStatus;
  deliveredAt: string | null;
  openedAt: string | null;
  clickedAt: string | null;
  bouncedAt: string | null;
  bounceReason: string | null;
  sendGridMessageId: string | null;
}

export interface BroadcastDraftInput {
  subject: string;
  body: string;
  plainTextBody: string | null;
  targetMode: BroadcastTargetMode;
  targetGroupIds: string[] | null;
  category: EmailCategory;
}

export interface RecipientPreviewItem {
  displayName: string;
  emailAddress: string;
}

export interface RecipientPreview {
  totalCount: number;
  sample: RecipientPreviewItem[];
}

export const broadcastsApi = {
  list: (params?: { status?: BroadcastStatus; page?: number; pageSize?: number }) => {
    const search = new URLSearchParams();
    if (params?.status !== undefined) search.set("status", String(params.status));
    if (params?.page) search.set("page", String(params.page));
    if (params?.pageSize) search.set("pageSize", String(params.pageSize));
    const qs = search.toString();
    return apiGet<PagedResult<EmailBroadcast>>(`/api/admin/broadcasts${qs ? `?${qs}` : ""}`);
  },
  get: (id: string) => apiGet<EmailBroadcast>(`/api/admin/broadcasts/${id}`),
  recipients: (id: string, params?: { status?: RecipientStatus; page?: number; pageSize?: number }) => {
    const search = new URLSearchParams();
    if (params?.status !== undefined) search.set("status", String(params.status));
    if (params?.page) search.set("page", String(params.page));
    if (params?.pageSize) search.set("pageSize", String(params.pageSize));
    const qs = search.toString();
    return apiGet<PagedResult<EmailBroadcastRecipient>>(`/api/admin/broadcasts/${id}/recipients${qs ? `?${qs}` : ""}`);
  },
  preview: (id: string) => apiPost<RecipientPreview>(`/api/admin/broadcasts/${id}/preview-recipients`, {}),
  create: (input: BroadcastDraftInput) => apiPost<EmailBroadcast>("/api/admin/broadcasts", input),
  update: (id: string, input: BroadcastDraftInput) => apiPut<EmailBroadcast>(`/api/admin/broadcasts/${id}`, input),
  send: (id: string) => apiPost<EmailBroadcast>(`/api/admin/broadcasts/${id}/send`, {}),
  schedule: (id: string, sendAt: string) =>
    apiPost<EmailBroadcast>(`/api/admin/broadcasts/${id}/schedule`, { sendAt }),
  cancel: (id: string) => apiPost<void>(`/api/admin/broadcasts/${id}/cancel`, {}),
};
