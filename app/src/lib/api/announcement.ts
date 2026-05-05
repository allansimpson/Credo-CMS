import { ApiError, apiGet, apiPut } from "@/lib/apiClient";
import type {
  AnnouncementBanner,
  PublicAnnouncementBanner,
  UpdateAnnouncementBannerRequest,
} from "@/types/api";

export const announcementApi = {
  get: () => apiGet<AnnouncementBanner>("/api/admin/announcement"),
  update: (req: UpdateAnnouncementBannerRequest) =>
    apiPut<AnnouncementBanner>("/api/admin/announcement", req),
  getPublic: async (): Promise<PublicAnnouncementBanner | null> => {
    try {
      return await apiGet<PublicAnnouncementBanner>("/api/public/banner", { emitUnauthorized: false });
    } catch (err) {
      // 204 NoContent surfaces as `undefined`/null; ApiError of any other
      // status means the bar is silently hidden.
      if (err instanceof ApiError) return null;
      return null;
    }
  },
};
