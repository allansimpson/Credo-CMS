import { apiGet } from "@/lib/apiClient";
import type { PagedResult } from "@/types/api";

export interface MemberListItem {
  id: string;
  firstName: string;
  lastName: string;
  displayName: string;
  email: string | null;
  phoneNumber: string | null;
  photoBlobUrl: string | null;
  photoWebpBlobUrl: string | null;
  photoAltText: string | null;
}

export interface MemberGroupMembership {
  groupId: string;
  groupSlug: string;
  groupName: string;
  isLeader: boolean;
}

export interface MemberDetail {
  id: string;
  firstName: string;
  lastName: string;
  displayName: string;
  email: string | null;
  phoneNumber: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  stateOrRegion: string | null;
  postalCode: string | null;
  country: string | null;
  photoBlobUrl: string | null;
  photoWebpBlobUrl: string | null;
  photoAltText: string | null;
  publicAuthorBio: string | null;
  groupMemberships: MemberGroupMembership[];
}

export interface MembersListQuery {
  search?: string;
  page?: number;
  pageSize?: number;
}

function toQuery(q: MembersListQuery): string {
  const params = new URLSearchParams();
  if (q.search) params.set("search", q.search);
  if (q.page !== undefined) params.set("page", String(q.page));
  if (q.pageSize !== undefined) params.set("pageSize", String(q.pageSize));
  return params.size > 0 ? `?${params.toString()}` : "";
}

export const membersApi = {
  list: (q: MembersListQuery = {}) =>
    apiGet<PagedResult<MemberListItem>>(`/api/members${toQuery(q)}`),
  get: (userId: string) => apiGet<MemberDetail>(`/api/members/${userId}`),
};
