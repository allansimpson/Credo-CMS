import { apiGet } from "@/lib/apiClient";
import type {
  AnnouncementSeverity,
  PublicNewsItem,
  PublicServiceTime,
  PublicSiteSettings,
} from "@/types/api";
import type { SermonListItem } from "@/lib/api/sermons";
import type { PublicEventListItem } from "@/lib/api/events";

export interface PublicAnnouncementBannerForHomepage {
  severity: AnnouncementSeverity;
  message: string;
  linkUrl: string | null;
  linkLabel: string | null;
}

export interface HomepageDto {
  site: PublicSiteSettings;
  serviceTimes: PublicServiceTime[];
  latestNews: PublicNewsItem[];
  membersWelcomeText: string | null;
  banner: PublicAnnouncementBannerForHomepage | null;
  // Home additions. Null/empty when there's no content yet; HomePage
  // degrades to ImageSlot placeholders + empty-state copy so the
  // layout still renders on a fresh deployment.
  latestSermon: SermonListItem | null;
  upcomingEvents: PublicEventListItem[];
}

export const homepageApi = {
  get: () => apiGet<HomepageDto>("/api/public/homepage", { emitUnauthorized: false }),
};
