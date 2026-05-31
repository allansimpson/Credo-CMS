import { useEffect } from "react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";

/**
 * Resolves the church's configured primary + accent colours and applies them
 * as CSS custom properties on whichever element carries `data-theme="church"`.
 * The Tailwind config consumes these via `hsl(var(--primary) / ...)` etc.
 *
 * Called from layouts that root a "church theme" subtree — the public site
 * (ChurchThemeLayout) and the member portal (MemberPortalLayout).
 */
export function useChurchColorTokens() {
  const { settings } = useSiteSettings();

  useEffect(() => {
    if (!settings) return;

    const root = document.querySelector<HTMLElement>("[data-theme='church']");
    if (!root) return;

    const primaryHsl = hexToHsl(settings.primaryColor);
    const accentHsl = hexToHsl(settings.accentColor);
    if (primaryHsl) root.style.setProperty("--primary", primaryHsl);
    if (accentHsl) root.style.setProperty("--accent", accentHsl);

    const template = settings.template === 1 ? "quiet" : "editorial";
    root.dataset.template = template;
  }, [settings]);
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
