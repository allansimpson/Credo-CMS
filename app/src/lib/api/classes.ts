import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";

// ---- enum mirrors -------------------------------------------------------

export const OfferingStatusFilter = {
  All: 0,
  Current: 1,
  Upcoming: 2,
  Past: 3,
} as const;
export type OfferingStatusFilter = (typeof OfferingStatusFilter)[keyof typeof OfferingStatusFilter];

// ---- Public DTOs --------------------------------------------------------

export interface PublicClassOffering {
  id: string;
  subject: string;
  descriptionJson: string | null;
  startDate: string; // YYYY-MM-DD
  endDate: string;
}

export interface PublicClassSlot {
  id: string;
  slug: string;
  name: string;
  audienceAgeGroup: string;
  generalMeetingTime: string | null;
  descriptionJson: string | null;
  imageBlobUrl: string | null;
  imageWebpBlobUrl: string | null;
  imageAltText: string | null;
  displayOrder: number;
  currentOffering: PublicClassOffering | null;
  upcomingOffering: PublicClassOffering | null;
  recentPastOffering: PublicClassOffering | null;
}

// Member-augmented variants. Member fields are optional on the type so the
// SPA can consume either response shape with a single union.
export interface MemberClassOffering extends PublicClassOffering {
  teacherLeaderId: string | null;
  teacherLeaderName: string | null;
  teacherFreeText: string | null;
  detailedScheduleJson: string | null;
  materialsNeeded: string | null;
}

export interface MemberClassSlot extends Omit<PublicClassSlot, "currentOffering" | "upcomingOffering" | "recentPastOffering"> {
  defaultRoom: string | null;
  currentOffering: MemberClassOffering | null;
  upcomingOffering: MemberClassOffering | null;
  recentPastOffering: MemberClassOffering | null;
}

export type ClassSlotResponse = PublicClassSlot | MemberClassSlot;
export type ClassOfferingResponse = PublicClassOffering | MemberClassOffering;

export function isMemberSlot(slot: ClassSlotResponse): slot is MemberClassSlot {
  // Presence of defaultRoom (always present on the member shape, even if null)
  // distinguishes the two. Members with no DefaultRoom set will still have the
  // key present; anonymous responses omit it entirely.
  return "defaultRoom" in slot;
}

// ---- Admin DTOs ---------------------------------------------------------

export interface AdminClassSlotListItem {
  id: string;
  slug: string;
  name: string;
  audienceAgeGroup: string;
  isActive: boolean;
  displayOrder: number;
  offeringCount: number;
  modifiedAt: string;
}

export interface AdminClassSlotDetail {
  id: string;
  slug: string;
  name: string;
  audienceAgeGroup: string;
  generalMeetingTime: string | null;
  defaultRoom: string | null;
  descriptionJson: string | null;
  imageBlobUrl: string | null;
  imageWebpBlobUrl: string | null;
  imageAltText: string | null;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  modifiedAt: string;
}

export interface AdminClassOffering {
  id: string;
  classSlotId: string;
  classSlotName: string;
  subject: string;
  descriptionJson: string | null;
  startDate: string;
  endDate: string;
  teacherLeaderId: string | null;
  teacherFreeText: string | null;
  detailedScheduleJson: string | null;
  materialsNeeded: string | null;
  createdAt: string;
  modifiedAt: string;
}

export interface CreateClassSlotRequest {
  slug: string;
  name: string;
  audienceAgeGroup: string;
  generalMeetingTime: string | null;
  defaultRoom: string | null;
  descriptionJson: string | null;
  imageBlobUrl: string | null;
  imageWebpBlobUrl: string | null;
  imageAltText: string | null;
  isActive: boolean;
  displayOrder: number;
}
export type UpdateClassSlotRequest = CreateClassSlotRequest;

export interface CreateClassOfferingRequest {
  classSlotId: string;
  subject: string;
  descriptionJson: string | null;
  startDate: string;
  endDate: string;
  teacherLeaderId: string | null;
  teacherFreeText: string | null;
  detailedScheduleJson: string | null;
  materialsNeeded: string | null;
}
export type UpdateClassOfferingRequest = CreateClassOfferingRequest;

// ---- API surface --------------------------------------------------------

export const publicClassesApi = {
  list: () => apiGet<ClassSlotResponse[]>("/api/public/classes"),
  get: (slug: string) =>
    apiGet<ClassSlotResponse>(`/api/public/classes/${encodeURIComponent(slug)}`),
};

export const adminClassSlotsApi = {
  list: (search?: string, includeInactive = true) => {
    const params = new URLSearchParams();
    if (search) params.set("search", search);
    params.set("includeInactive", String(includeInactive));
    return apiGet<AdminClassSlotListItem[]>(`/api/admin/class-slots?${params.toString()}`);
  },
  get: (id: string) => apiGet<AdminClassSlotDetail>(`/api/admin/class-slots/${id}`),
  create: (req: CreateClassSlotRequest) =>
    apiPost<AdminClassSlotDetail>("/api/admin/class-slots", req),
  update: (id: string, req: UpdateClassSlotRequest) =>
    apiPut<AdminClassSlotDetail>(`/api/admin/class-slots/${id}`, req),
  softDelete: (id: string) => apiDelete<void>(`/api/admin/class-slots/${id}`),
};

export interface AdminOfferingsQuery {
  classSlotId?: string;
  fromDate?: string;
  toDate?: string;
  status?: OfferingStatusFilter;
}

export const adminClassOfferingsApi = {
  list: (q: AdminOfferingsQuery = {}) => {
    const params = new URLSearchParams();
    if (q.classSlotId) params.set("classSlotId", q.classSlotId);
    if (q.fromDate) params.set("fromDate", q.fromDate);
    if (q.toDate) params.set("toDate", q.toDate);
    if (q.status !== undefined) params.set("status", String(q.status));
    return apiGet<AdminClassOffering[]>(
      `/api/admin/class-offerings${params.size ? `?${params.toString()}` : ""}`,
    );
  },
  get: (id: string) => apiGet<AdminClassOffering>(`/api/admin/class-offerings/${id}`),
  create: (req: CreateClassOfferingRequest) =>
    apiPost<AdminClassOffering>("/api/admin/class-offerings", req),
  update: (id: string, req: UpdateClassOfferingRequest) =>
    apiPut<AdminClassOffering>(`/api/admin/class-offerings/${id}`, req),
  softDelete: (id: string) => apiDelete<void>(`/api/admin/class-offerings/${id}`),
};
