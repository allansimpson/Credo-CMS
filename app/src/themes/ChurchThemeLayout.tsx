import type { ReactNode } from "react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { usePageViewTracking } from "@/lib/useAnalytics";
import { CookieConsentBanner } from "@/components/shared/CookieConsentBanner";
import { useChurchColorTokens } from "@/themes/useChurchColorTokens";

/**
 * Wraps any subtree in the church theme. Applies the church's configured primary
 * and accent colours by setting CSS custom properties on the wrapping element,
 * which the Tailwind config consumes via hsl(var(--primary) / ...) etc.
 */
export function ChurchThemeLayout({ children }: { children: ReactNode }) {
  usePageViewTracking();
  useChurchColorTokens();
  const { settings } = useSiteSettings();

  // First-paint default (before useSiteSettings resolves) — pick from the
  // bootstrap value if available, otherwise editorial.
  const initialTemplate = settings?.template === 1 ? "quiet" : "editorial";

  return (
    <div data-theme="church" data-template={initialTemplate} className="min-h-full bg-background text-foreground">
      {/* Accessibility skip-to-main-content link. sr-only by default;
          visible on keyboard focus. The public layout's <main> carries
          id="main-content". */}
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50 focus:bg-primary focus:text-primary-foreground focus:px-3 focus:py-2"
      >
        Skip to main content
      </a>
      <div id="main-content" tabIndex={-1}>
        {children}
      </div>
      {/* Cookie consent banner. Self-gates on
          settings.analyticsProvider === Ga4 + cms_consent absence. */}
      <CookieConsentBanner />
    </div>
  );
}
