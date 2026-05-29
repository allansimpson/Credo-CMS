import { apiGet, apiPost, apiPut } from "@/lib/apiClient";
import type {
  EmailProvider,
  PublicSiteSettings,
  SiteSettings,
  UpdateSiteSettingsRequest,
} from "@/types/api";

export interface TestEmailRequest {
  provider: EmailProvider;
  emailFromAddress: string;
  emailFromName: string;
  emailReplyToAddress: string | null;
  sendGridApiKey: string | null;
  smtpHost: string | null;
  smtpPort: number;
  smtpUsername: string | null;
  smtpPassword: string | null;
  smtpUseSsl: boolean;
  testEmailRecipient: string | null;
  overrideToAddress: string | null;
}

export interface TestEmailResult {
  success: boolean;
  errorMessage: string | null;
  note: string | null;
}

export const siteSettingsApi = {
  /** Anonymous endpoint, called from the SPA root layout. */
  getPublic: () =>
    apiGet<PublicSiteSettings>("/api/site-settings/public", {
      emitUnauthorized: false,
    }),

  getAdmin: () => apiGet<SiteSettings>("/api/admin/site-settings"),

  update: (req: UpdateSiteSettingsRequest) =>
    apiPut<SiteSettings>("/api/admin/site-settings", req),

  /** Sends a test message using the supplied (possibly unsaved) provider
   * config to the current admin's email — or to overrideToAddress when set. */
  testEmail: (req: TestEmailRequest) =>
    apiPost<TestEmailResult>("/api/admin/site-settings/test-email", req),
};
