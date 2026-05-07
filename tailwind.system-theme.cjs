/**
 * Phase 6 — shared system-theme tokens consumed by both the SPA's Tailwind
 * config and the Astro docs site's. Keeping the values here avoids drift
 * between the SPA's admin UI and the docs site appearance. Both consumers
 * import this file via require().
 *
 * The tokens are intentionally minimal — colors and a few spacing/radius
 * values that the system-theme actually uses. Church-theme tokens stay
 * in the SPA's tailwind.config.js since the docs site is system-themed
 * only.
 */
module.exports = {
  colors: {
    // System-theme palette — neutral, designed to read alongside the
    // back-office admin shell.
    bg: "#ffffff",
    "bg-alt": "#f8fafc",
    fg: "#0f172a",
    muted: "#64748b",
    border: "#e2e8f0",
    primary: {
      DEFAULT: "#1e3a8a",
      foreground: "#ffffff",
    },
    accent: {
      DEFAULT: "#f59e0b",
      foreground: "#0f172a",
    },
    info: "#0ea5e9",
    warning: "#f59e0b",
    danger: "#dc2626",
    success: "#16a34a",
  },
  borderRadius: {
    none: "0",
    sm: "0",
    DEFAULT: "0",
    md: "0",
    lg: "0",
    full: "9999px",
  },
  fontFamily: {
    sans: ["system-ui", "-apple-system", "Segoe UI", "Roboto", "sans-serif"],
    mono: ["ui-monospace", "SFMono-Regular", "Menlo", "monospace"],
  },
};
