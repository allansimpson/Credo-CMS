import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { readConsent } from "@/components/shared/CookieConsentBanner";

declare global {
  interface Window {
    gtag?: (...args: unknown[]) => void;
    dataLayer?: unknown[];
  }
}

/**
 * GA4 page-view tracker. Fires gtag('event', 'page_view', ...)
 * on every React Router location change, but only when:
 *   - settings.analyticsProvider === Ga4
 *   - the visitor has accepted cookies
 *   - gtag is loaded (the consent banner injects it)
 *
 * Mounted once high in the public tree.
 */
export function usePageViewTracking() {
  const { settings } = useSiteSettings();
  const location = useLocation();

  useEffect(() => {
    if (!settings || settings.analyticsProvider !== 1) return;
    if (readConsent() !== "accepted") return;
    if (!settings.ga4MeasurementId) return;
    if (typeof window === "undefined" || !window.gtag) return;

    window.gtag("event", "page_view", {
      page_path: location.pathname + location.search,
      page_location: window.location.href,
    });
  }, [settings, location.pathname, location.search]);
}
