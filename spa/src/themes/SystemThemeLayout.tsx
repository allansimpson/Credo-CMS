import type { ReactNode } from "react";

/**
 * Wraps any subtree in the system theme. Used at /admin/* and /docs/*. Fixed
 * across all churches forever — only the church name and logo swap in for
 * visual continuity.
 */
export function SystemThemeLayout({ children }: { children: ReactNode }) {
  return (
    <div data-theme="system" className="min-h-full">
      {children}
    </div>
  );
}
