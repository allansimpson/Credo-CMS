/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ["class"],
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    container: {
      center: true,
      padding: "1rem",
      screens: {
        "2xl": "1400px",
      },
    },
    extend: {
      colors: {
        // Driven by CSS variables emitted by the active theme layout
        // (church-theme.css or system-theme.css). Variables follow the shadcn
        // "<channel>: <hsl values without hsl()>" convention so we can use
        // /<alpha> modifiers on bg-primary/50, etc.
        background: "hsl(var(--background) / <alpha-value>)",
        foreground: "hsl(var(--foreground) / <alpha-value>)",
        // Editorial tokens (system theme — see themes/system-theme.css §3).
        panel: {
          DEFAULT: "hsl(var(--panel) / <alpha-value>)",
          alt: "hsl(var(--panel-alt) / <alpha-value>)",
        },
        sidebar: "hsl(var(--sidebar) / <alpha-value>)",
        fg: {
          DEFAULT: "hsl(var(--foreground) / <alpha-value>)",
          soft: "hsl(var(--fg-soft) / <alpha-value>)",
        },
        success: "hsl(var(--success) / <alpha-value>)",
        warn: "hsl(var(--warn) / <alpha-value>)",
        primary: {
          DEFAULT: "hsl(var(--primary) / <alpha-value>)",
          foreground: "hsl(var(--primary-foreground) / <alpha-value>)",
        },
        accent: {
          DEFAULT: "hsl(var(--accent) / <alpha-value>)",
          foreground: "hsl(var(--accent-foreground) / <alpha-value>)",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary) / <alpha-value>)",
          foreground: "hsl(var(--secondary-foreground) / <alpha-value>)",
        },
        // Renamed from `destructive` per Claude Design clarification #1.
        // Tailwind utilities: text-danger, bg-danger, border-danger,
        // text-danger-foreground, bg-danger/10, etc.
        danger: {
          DEFAULT: "hsl(var(--danger) / <alpha-value>)",
          foreground: "hsl(var(--danger-foreground) / <alpha-value>)",
        },
        // Renamed from `muted-foreground` per Claude Design clarification #1.
        // `--muted` is now the tertiary text token (used by `text-muted`).
        // Sites that previously used `bg-muted` (subtle surface) should use
        // `bg-panel-alt` — same value, more semantic name.
        muted: "hsl(var(--muted) / <alpha-value>)",
        card: {
          DEFAULT: "hsl(var(--card) / <alpha-value>)",
          foreground: "hsl(var(--card-foreground) / <alpha-value>)",
        },
        popover: {
          DEFAULT: "hsl(var(--popover) / <alpha-value>)",
          foreground: "hsl(var(--popover-foreground) / <alpha-value>)",
        },
        border: {
          DEFAULT: "hsl(var(--border) / <alpha-value>)",
          soft: "hsl(var(--border-soft) / <alpha-value>)",
        },
        input: "hsl(var(--input) / <alpha-value>)",
        ring: "hsl(var(--ring) / <alpha-value>)",
        // Public Site design handoff — dark inset blocks (Editorial uses
        // them for "I'm New" / "Give" / footer; Quiet uses them sparingly).
        inset: {
          DEFAULT: "hsl(var(--inset) / <alpha-value>)",
          foreground: "hsl(var(--inset-fg) / <alpha-value>)",
        },
      },
      fontFamily: {
        heading: "var(--font-heading)",
        body: "var(--font-body)",
        mono: "var(--font-mono)",
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
};
