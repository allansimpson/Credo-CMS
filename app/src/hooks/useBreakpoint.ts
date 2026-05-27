import { useEffect, useState } from "react";

export type Breakpoint = "mobile" | "tablet" | "desktop";

const TABLET_MIN_WIDTH = 768;
const DESKTOP_MIN_WIDTH = 1280;

function resolveBreakpoint(width: number): Breakpoint {
  if (width >= DESKTOP_MIN_WIDTH) return "desktop";
  if (width >= TABLET_MIN_WIDTH) return "tablet";
  return "mobile";
}

/**
 * Returns the current breakpoint as one of `'mobile' | 'tablet' | 'desktop'`.
 * Breakpoints: mobile (<768), tablet (768–1279), desktop (≥1280).
 *
 * SSR-safe: returns 'desktop' when window is undefined. The post-mount effect
 * resolves the actual breakpoint and subscribes to resize events.
 */
export function useBreakpoint(): Breakpoint {
  const [breakpoint, setBreakpoint] = useState<Breakpoint>(() => {
    if (typeof window === "undefined") return "desktop";
    return resolveBreakpoint(window.innerWidth);
  });

  useEffect(() => {
    if (typeof window === "undefined") return;

    const handler = () => setBreakpoint(resolveBreakpoint(window.innerWidth));

    handler();
    window.addEventListener("resize", handler);
    window.addEventListener("orientationchange", handler);

    return () => {
      window.removeEventListener("resize", handler);
      window.removeEventListener("orientationchange", handler);
    };
  }, []);

  return breakpoint;
}
