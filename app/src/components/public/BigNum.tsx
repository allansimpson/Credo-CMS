import type { ReactNode } from "react";

export type BigNumSize = "sm" | "md" | "lg" | "xl";

export interface BigNumProps {
  /** Visual size. Match `lg` for stats rows, `xl` for hero dates,
   * `md` for inline pull-numbers, `sm` for chrome (length, etc.). */
  size?: BigNumSize;
  /** Tone — `default` uses --foreground; `accent` for highlighted
   * numbers (e.g., featured event); `muted` for chrome. */
  tone?: "default" | "accent" | "muted" | "inverse";
  children: ReactNode;
  className?: string;
}

const SIZE_CLASSES: Record<BigNumSize, string> = {
  sm: "text-base leading-none",
  md: "text-2xl leading-none",
  lg: "text-4xl leading-none",
  xl: "text-6xl leading-none",
};

/**
 * Big tabular numerals — Editorial's signature element, also used in
 * Quiet for stats. Uses font-variant-numeric: tabular-nums so columns
 * of numbers align cleanly. Always rendered with the heading font so
 * the weight matches surrounding type.
 */
export function BigNum({ size = "lg", tone = "default", children, className }: BigNumProps) {
  const toneClass =
    tone === "accent"
      ? "text-accent"
      : tone === "muted"
        ? "text-muted"
        : tone === "inverse"
          ? "text-inset-foreground"
          : "text-foreground";
  return (
    <span
      className={[
        "font-heading font-semibold tabular-nums",
        SIZE_CLASSES[size],
        toneClass,
        className ?? "",
      ].join(" ")}
    >
      {children}
    </span>
  );
}
