import type { ReactNode } from "react";

export interface EyebrowProps {
  /** When true, prefix the label with a short accent rule. Editorial uses
   * this on most sections; Quiet typically omits the rule. */
  accent?: boolean;
  /** Tone — `default` uses --muted; `accent` switches to --accent;
   * `inverse` is for placement on dark inset bands. */
  tone?: "default" | "accent" | "inverse";
  children: ReactNode;
  className?: string;
}

/**
 * Uppercase letter-spaced label sitting above a headline. Both templates
 * use this; visual differences live in the surrounding spacing + the
 * optional leading rule. The handoff's "11px (Editorial) / 12px (Quiet)"
 * size split is handled by template-level CSS via the `data-template`
 * attribute — we set the smaller font-size and let the cascade upscale
 * for Quiet via the parent.
 */
export function Eyebrow({ accent = false, tone = "default", children, className }: EyebrowProps) {
  const toneClass =
    tone === "accent"
      ? "text-accent"
      : tone === "inverse"
        ? "text-inset-foreground/80"
        : "text-muted";
  return (
    <p
      className={[
        "inline-flex items-center gap-3 text-[11px] font-medium uppercase tracking-[0.18em]",
        toneClass,
        className ?? "",
      ].join(" ")}
    >
      {accent ? (
        <span aria-hidden="true" className="inline-block h-px w-8 bg-accent" />
      ) : null}
      <span>{children}</span>
    </p>
  );
}
