import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type { PagedResult } from "@/types/api";

export type EventVisibility = 0 | 1; // Public | MembersOnly
export type EventRegistrationMode = 0 | 1 | 2; // None | RsvpOptional | RegistrationRequired

export interface EventListItem {
  id: string;
  slug: string;
  title: string;
  startsAt: string;
  endsAt: string | null;
  allDay: boolean;
  location: string | null;
  category: string | null;
  visibility: EventVisibility | null;
  registrationMode: EventRegistrationMode;
  hasRecurrence: boolean;
  isPublished: boolean;
  isDeleted: boolean;
  modifiedAt: string;
}

export interface EventDetail {
  id: string;
  slug: string;
  title: string;
  descriptionJson: string | null;
  startsAt: string;
  endsAt: string | null;
  allDay: boolean;
  location: string | null;
  category: string | null;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  visibility: EventVisibility | null;
  recurrenceRule: string | null;
  recurrenceEndDate: string | null;
  recurrenceCount: number | null;
  registrationMode: EventRegistrationMode;
  capacity: number | null;
  waitlistEnabled: boolean;
  registrationOpensAt: string | null;
  registrationClosesAt: string | null;
  registrationConfirmationMessageJson: string | null;
  externalRegistrationUrl: string | null;
  isPublished: boolean;
  isDeleted: boolean;
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
  deletedAt: string | null;
}

export interface PublicEventListItem {
  id: string;
  slug: string;
  title: string;
  startsAt: string;
  endsAt: string | null;
  allDay: boolean;
  location: string | null;
  category: string | null;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  visibility: EventVisibility | null;
  registrationMode: EventRegistrationMode;
  recurrenceRule: string | null;
  nextOccurrenceAt: string;
}

export interface PublicEvent {
  id: string;
  slug: string;
  title: string;
  descriptionJson: string | null;
  startsAt: string;
  endsAt: string | null;
  allDay: boolean;
  location: string | null;
  category: string | null;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  visibility: EventVisibility | null;
  recurrenceRule: string | null;
  recurrenceEndDate: string | null;
  recurrenceCount: number | null;
  registrationMode: EventRegistrationMode;
  capacity: number | null;
  waitlistEnabled: boolean;
  registrationOpensAt: string | null;
  registrationClosesAt: string | null;
  registrationConfirmationMessageJson: string | null;
  externalRegistrationUrl: string | null;
  nextOccurrences: string[];
}

export type EventRequest = Omit<EventDetail, "id" | "createdAt" | "modifiedAt" | "modifiedByUserId" | "deletedAt" | "isDeleted">;

export interface EventListQuery {
  search?: string;
  category?: string;
  visibility?: EventVisibility;
  registrationMode?: EventRegistrationMode;
  hasRecurrence?: boolean;
  includeDeleted?: boolean;
  page?: number;
  pageSize?: number;
}

function buildQuery(q: EventListQuery): string {
  const usp = new URLSearchParams();
  if (q.search) usp.set("search", q.search);
  if (q.category) usp.set("category", q.category);
  if (q.visibility !== undefined) usp.set("visibility", String(q.visibility));
  if (q.registrationMode !== undefined) usp.set("registrationMode", String(q.registrationMode));
  if (q.hasRecurrence !== undefined) usp.set("hasRecurrence", String(q.hasRecurrence));
  if (q.includeDeleted) usp.set("includeDeleted", "true");
  if (q.page) usp.set("page", String(q.page));
  if (q.pageSize) usp.set("pageSize", String(q.pageSize));
  const s = usp.toString();
  return s ? `?${s}` : "";
}

export const eventsApi = {
  list: (query: EventListQuery = {}) =>
    apiGet<PagedResult<EventListItem>>(`/api/admin/events${buildQuery(query)}`),
  get: (id: string) => apiGet<EventDetail>(`/api/admin/events/${id}`),
  create: (req: EventRequest) => apiPost<EventDetail>("/api/admin/events", req),
  update: (id: string, req: EventRequest) => apiPut<EventDetail>(`/api/admin/events/${id}`, req),
  softDelete: (id: string) => apiDelete<void>(`/api/admin/events/${id}`),
  restore: (id: string) => apiPost<EventDetail>(`/api/admin/events/${id}/restore`),
  hardDelete: (id: string) => apiDelete<void>(`/api/admin/events/${id}/hard`),
  skipOccurrence: (id: string, date: string, reason?: string) =>
    apiPost<void>(`/api/admin/events/${id}/skip-occurrence`, { date, reason }),

  // Public
  listPublic: (page = 1, pageSize = 12, category?: string) => {
    const usp = new URLSearchParams();
    usp.set("page", String(page));
    usp.set("pageSize", String(pageSize));
    if (category) usp.set("category", category);
    return apiGet<PagedResult<PublicEventListItem>>(`/api/public/events?${usp.toString()}`,
      { emitUnauthorized: false });
  },
  getPublic: (slug: string) =>
    apiGet<PublicEvent>(`/api/public/events/${encodeURIComponent(slug)}`,
      { emitUnauthorized: false }),
};
