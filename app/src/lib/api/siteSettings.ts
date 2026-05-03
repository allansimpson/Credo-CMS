import { apiGet, apiPut } from "@/lib/apiClient";
import type {
  PublicSiteSettings,
  SiteSettings,
  UpdateSiteSettingsRequest,
} from "@/types/api";

export const siteSettingsApi = {
  /** Anonymous endpoint, called from the SPA root layout. */
  getPublic: () =>
    apiGet<PublicSiteSettings>("/api/site-settings/public", {
      emitUnauthorized: false,
    }),

  getAdmin: () => apiGet<SiteSettings>("/api/admin/site-settings"),

  update: (req: UpdateSiteSettingsRequest) =>
    apiPut<SiteSettings>("/api/admin/site-settings", req),
};
