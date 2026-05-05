import { apiGet } from "@/lib/apiClient";

export interface CalendarItem {
  entityType: "Event" | "News" | string;
  entityId: string;
  title: string;
  start: string;
  end: string | null;
  allDay: boolean;
  url: string;
  location: string | null;
  heroImageUrl: string | null;
  membersOnly: boolean;
}

export const calendarApi = {
  list: (start: Date, end: Date) =>
    apiGet<CalendarItem[]>(
      `/api/public/calendar?start=${encodeURIComponent(start.toISOString())}&end=${encodeURIComponent(end.toISOString())}`,
      { emitUnauthorized: false }
    ),
};
