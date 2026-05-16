import type { ElementType, ReactNode } from "react";

export type HeadlineSize = "display" | "h1" | "h2" | "h3";

export interface HeadlineProps {
  /** Size token. Maps to template-specific tracking + font-size. */
  size?: HeadlineSize;
  /** Heading element — defaults to `h1`. Provide `h2`/`h3` when nested
   * under a hero. */
  as?: ElementType;
  /** Tone — `default` uses --foreground (template-fixed); `inverse` for
   * dark insets; `accent` for the rare moment a heading wants the
   * accent color. */
  tone?: "default" | "inverse" | "accent";
  children: ReactNode;
  className?: string;
}

const SIZE_CLASSES: Record<HeadlineSize, string> = {
  // Display is the full hero size. Editorial 84-88, Quiet 88-120.
  // Using a clamp() so the same component scales nicely without two
  // separate render paths.
  display: "text-[clamp(3.5rem,8vw,7rem)] leading-[0.95] tracking-[-0.025em]",
  h1: "text-[clamp(2.5rem,5vw,4.5rem)] leading-[1.0] tracking-[-0.022em]",
  h2: "text-[clamp(1.875rem,3vw,3rem)] leading-[1.1] tracking-[-0.018em]",
  h3: "text-[clamp(1.25rem,2vw,1.75rem)] leading-[1.2] tracking-[-0.012em]",
};

/**
 * Headline component with template-aware sizing. Per the design handoff
 * (Q4 token-scoping decision): headlines use --foreground (template-
 * fixed), NOT --primary. A tenant's chosen primary color must not
 * accidentally repaint every headline.
 */
export function Headline({
  size = "h1",
  as: Tag = "h1",
  tone = "default",
  children,
  className,
}: HeadlineProps) {
  const toneClass =
    tone === "inverse"
      ? "text-inset-foreground"
      : tone === "accent"
        ? "text-accent"
        : "text-foreground";
  return (
    <Tag
      className={[
        "font-heading font-semibold",
        SIZE_CLASSES[size],
        toneClass,
        className ?? "",
      ].join(" ")}
    >
      {children}
    </Tag>
  );
}
