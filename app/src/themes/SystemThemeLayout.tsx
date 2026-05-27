import type { ReactNode } from "react";

/**
 * Wraps any subtree in the system theme. Used at /admin/* and /docs/*. Fixed
 * across all churches forever — only the church name and logo swap in for
 * visual continuity.
 *
 * Includes a skip-to-main-content link for keyboard users. The
 * AdminLayout's <main> carries id="admin-main-content"; auth pages (login,
 * forgot-password, etc.) wrap themselves in SystemThemeLayout but don't
 * provide an explicit main-content anchor — the link points to a wrapping
 * div that focuses the page's content area regardless of inner structure.
 */
export function SystemThemeLayout({ children }: { children: ReactNode }) {
  return (
    <div data-theme="system" className="min-h-full">
      <a
        href="#system-main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50 focus:bg-primary focus:text-primary-foreground focus:px-3 focus:py-2"
      >
        Skip to main content
      </a>
      <div id="system-main-content" tabIndex={-1}>
        {children}
      </div>
    </div>
  );
}
