import type { ReactNode } from "react";
import { useEffect } from "react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { usePageViewTracking } from "@/lib/useAnalytics";
import { CookieConsentBanner } from "@/components/shared/CookieConsentBanner";

/**
 * Wraps any subtree in the church theme. Applies the church's configured primary
 * and accent colours by setting CSS custom properties on the wrapping element,
 * which the Tailwind config consumes via hsl(var(--primary) / ...) etc.
 */
export function ChurchThemeLayout({ children }: { children: ReactNode }) {
  usePageViewTracking();
  const { settings } = useSiteSettings();

  useEffect(() => {
    if (!settings) return;

    const root = document.querySelector<HTMLElement>("[data-theme='church']");
    if (!root) return;

    const primaryHsl = hexToHsl(settings.primaryColor);
    const accentHsl = hexToHsl(settings.accentColor);
    if (primaryHsl) root.style.setProperty("--primary", primaryHsl);
    if (accentHsl) root.style.setProperty("--accent", accentHsl);

    // Public Site design handoff: set data-template on the theme root so
    // the CSS variable cascade picks up the right token block. Default
    // 'editorial' if the bootstrap hasn't populated it yet.
    const template = settings.template === 1 ? "quiet" : "editorial";
    root.dataset.template = template;
  }, [settings]);

  // First-paint default (before useSiteSettings resolves) — pick from the
  // bootstrap value if available, otherwise editorial.
  const initialTemplate = settings?.template === 1 ? "quiet" : "editorial";

  return (
    <div data-theme="church" data-template={initialTemplate} className="min-h-full">
      {/* Phase 6 — accessibility skip-to-main-content link. sr-only by
          default; visible on keyboard focus. The public layout's <main>
          carries id="main-content". */}
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50 focus:bg-primary focus:text-primary-foreground focus:px-3 focus:py-2"
      >
        Skip to main content
      </a>
      <div id="main-content" tabIndex={-1}>
        {children}
      </div>
      {/* Phase 6 — cookie consent banner. Self-gates on
          settings.analyticsProvider === Ga4 + cms_consent absence. */}
      <CookieConsentBanner />
    </div>
  );
}

/**
 * Converts a "#rrggbb" string to the "h s% l%" form used by Tailwind's
 * hsl(var(--primary)) consumption pattern.
 */
function hexToHsl(hex: string): string | null {
  if (!/^#[0-9a-fA-F]{6}$/.test(hex)) return null;

  const r = parseInt(hex.slice(1, 3), 16) / 255;
  const g = parseInt(hex.slice(3, 5), 16) / 255;
  const b = parseInt(hex.slice(5, 7), 16) / 255;

  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const l = (max + min) / 2;
  let h = 0;
  let s = 0;

  if (max !== min) {
    const d = max - min;
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
    switch (max) {
      case r:
        h = (g - b) / d + (g < b ? 6 : 0);
        break;
      case g:
        h = (b - r) / d + 2;
        break;
      case b:
        h = (r - g) / d + 4;
        break;
    }
    h /= 6;
  }

  return `${Math.round(h * 360)} ${Math.round(s * 100)}% ${Math.round(l * 100)}%`;
}
