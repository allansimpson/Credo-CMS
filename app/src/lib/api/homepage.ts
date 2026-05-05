import { apiGet } from "@/lib/apiClient";
import type {
  AnnouncementSeverity,
  PublicNewsItem,
  PublicServiceTime,
  PublicSiteSettings,
} from "@/types/api";

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
}

export const homepageApi = {
  get: () => apiGet<HomepageDto>("/api/public/homepage", { emitUnauthorized: false }),
};
