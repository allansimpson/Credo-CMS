import { apiGet, apiPost } from "@/lib/apiClient";

export interface FeedTokenStatus {
  hasToken: boolean;
  createdAt: string | null;
  lastUsedAt: string | null;
}
export interface IssuedFeedToken {
  token: string;
  url: string;
}

export const profileCalendarFeedApi = {
  status: () => apiGet<FeedTokenStatus>("/api/profile/calendar-feed"),
  issue: () => apiPost<IssuedFeedToken>("/api/profile/calendar-feed/issue"),
  revoke: () => apiPost<void>("/api/profile/calendar-feed/revoke"),
};
