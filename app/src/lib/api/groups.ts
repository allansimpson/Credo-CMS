import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";

// ---- enum mirrors -------------------------------------------------------
// The backend uses System.Text.Json JsonStringEnumConverter, so these
// arrive on the wire as the enum NAMES ("Public", "Open", "Pending"),
// not the numeric values. Comparisons stay readable when the constants
// hold strings rather than numbers.

export const GroupVisibility = {
  Public: "Public",
  MembersOnly: "MembersOnly",
  Hidden: "Hidden",
} as const;
export type GroupVisibility = (typeof GroupVisibility)[keyof typeof GroupVisibility];

export const GroupJoinability = {
  Open: "Open",
  InviteOnly: "InviteOnly",
  Closed: "Closed",
} as const;
export type GroupJoinability = (typeof GroupJoinability)[keyof typeof GroupJoinability];

export const MessageOnJoinRequest = {
  Hidden: "Hidden",
  Optional: "Optional",
  Required: "Required",
} as const;
export type MessageOnJoinRequest = (typeof MessageOnJoinRequest)[keyof typeof MessageOnJoinRequest];

export const RosterVisibility = {
  LeadersOnly: "LeadersOnly",
  AllGroupMembers: "AllGroupMembers",
} as const;
export type RosterVisibility = (typeof RosterVisibility)[keyof typeof RosterVisibility];

export const GroupMembershipStatus = {
  Pending: "Pending",
  Active: "Active",
  Declined: "Declined",
  Removed: "Removed",
} as const;
export type GroupMembershipStatus = (typeof GroupMembershipStatus)[keyof typeof GroupMembershipStatus];

// ---- DTOs ---------------------------------------------------------------

export interface PublicGroupListItem {
  id: string;
  slug: string;
  name: string;
  imageBlobUrl: string | null;
  imageWebpBlobUrl: string | null;
  imageAltText: string | null;
  meetingInfo: string | null;
  visibility: GroupVisibility;
  joinability: GroupJoinability;
}

export interface GroupRosterEntry {
  userId: string;
  displayName: string;
  isLeader: boolean;
  photoBlobUrl: string | null;
  photoWebpBlobUrl: string | null;
  photoAltText: string | null;
}

export interface PublicGroupDetail {
  id: string;
  slug: string;
  name: string;
  descriptionJson: string | null;
  imageBlobUrl: string | null;
  imageWebpBlobUrl: string | null;
  imageAltText: string | null;
  contactEmail: string | null;
  meetingInfo: string | null;
  visibility: GroupVisibility;
  joinability: GroupJoinability;
  requiresMessageOnJoinRequest: MessageOnJoinRequest;
  roster: GroupRosterEntry[] | null;
  viewerIsMember: boolean;
  viewerHasPendingRequest: boolean;
}

export interface AdminGroupListItem {
  id: string;
  slug: string;
  name: string;
  visibility: GroupVisibility;
  joinability: GroupJoinability;
  isActive: boolean;
  activeMemberCount: number;
  pendingRequestCount: number;
  modifiedAt: string;
}

export interface AdminGroupDetail {
  id: string;
  slug: string;
  name: string;
  descriptionJson: string | null;
  imageBlobUrl: string | null;
  imageWebpBlobUrl: string | null;
  imageAltText: string | null;
  contactEmail: string | null;
  meetingInfo: string | null;
  visibility: GroupVisibility;
  joinability: GroupJoinability;
  requiresMessageOnJoinRequest: MessageOnJoinRequest;
  rosterVisibility: RosterVisibility;
  isActive: boolean;
  createdAt: string;
  modifiedAt: string;
}

export interface AdminMembership {
  id: string;
  groupId: string;
  userId: string;
  userDisplayName: string;
  userEmail: string | null;
  status: GroupMembershipStatus;
  isLeader: boolean;
  joinRequestMessage: string | null;
  requestedAt: string | null;
  joinedAt: string | null;
  processedAt: string | null;
  processedByUserId: string | null;
}

export interface ProfileMembership {
  groupId: string;
  groupSlug: string;
  groupName: string;
  isLeader: boolean;
  status: GroupMembershipStatus;
  joinedAt: string | null;
  requestedAt: string | null;
}

export interface CreateGroupRequest {
  slug: string;
  name: string;
  descriptionJson: string | null;
  imageBlobUrl: string | null;
  imageWebpBlobUrl: string | null;
  imageAltText: string | null;
  contactEmail: string | null;
  meetingInfo: string | null;
  visibility: GroupVisibility;
  joinability: GroupJoinability;
  requiresMessageOnJoinRequest: MessageOnJoinRequest;
  rosterVisibility: RosterVisibility;
  isActive: boolean;
}
export type UpdateGroupRequest = CreateGroupRequest;

export interface JoinRequestRequest {
  message: string | null;
}

export interface AddMemberRequest {
  userId: string;
  isLeader: boolean;
}

// ---- API surface ---------------------------------------------------------

export const publicGroupsApi = {
  list: () => apiGet<PublicGroupListItem[]>("/api/public/groups"),
  get: (slug: string) =>
    apiGet<PublicGroupDetail>(`/api/public/groups/${encodeURIComponent(slug)}`),
  requestJoin: (slug: string, req: JoinRequestRequest) =>
    apiPost<{ requested: boolean }>(`/api/public/groups/${encodeURIComponent(slug)}/request-join`, req),
};

export const adminGroupsApi = {
  list: (search?: string, includeInactive = true) => {
    const params = new URLSearchParams();
    if (search) params.set("search", search);
    params.set("includeInactive", String(includeInactive));
    return apiGet<AdminGroupListItem[]>(`/api/admin/groups?${params.toString()}`);
  },
  get: (id: string) => apiGet<AdminGroupDetail>(`/api/admin/groups/${id}`),
  create: (req: CreateGroupRequest) =>
    apiPost<AdminGroupDetail>("/api/admin/groups", req),
  update: (id: string, req: UpdateGroupRequest) =>
    apiPut<AdminGroupDetail>(`/api/admin/groups/${id}`, req),
  softDelete: (id: string) => apiDelete<void>(`/api/admin/groups/${id}`),
  listMemberships: (id: string, status?: GroupMembershipStatus) => {
    const q = status !== undefined ? `?status=${status}` : "";
    return apiGet<AdminMembership[]>(`/api/admin/groups/${id}/memberships${q}`);
  },
  addMember: (id: string, req: AddMemberRequest) =>
    apiPost<AdminMembership>(`/api/admin/groups/${id}/members`, req),
  removeMember: (id: string, userId: string) =>
    apiDelete<void>(`/api/admin/groups/${id}/members/${userId}`),
  setLeader: (id: string, userId: string, isLeader: boolean) =>
    apiPut<AdminMembership>(`/api/admin/groups/${id}/members/${userId}/leader`, { isLeader }),
  approve: (membershipId: string) =>
    apiPost<AdminMembership>(`/api/admin/groups/memberships/${membershipId}/approve`),
  decline: (membershipId: string) =>
    apiPost<AdminMembership>(`/api/admin/groups/memberships/${membershipId}/decline`),
};

export const profileGroupsApi = {
  listMine: () => apiGet<ProfileMembership[]>("/api/profile/groups"),
  leave: (groupId: string) =>
    apiPost<void>(`/api/profile/groups/leave/${groupId}`),
};
