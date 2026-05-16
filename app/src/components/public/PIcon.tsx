import type { LucideIcon, LucideProps } from "lucide-react";

export type PIconSize = "xs" | "sm" | "md" | "lg" | "xl";

export interface PIconProps extends Omit<LucideProps, "size" | "strokeWidth"> {
  /** The Lucide icon component (e.g. `import { ArrowRight } from "lucide-react"`). */
  icon: LucideIcon;
  size?: PIconSize;
  /** Decorative icons sit alongside text and should be aria-hidden.
   * Standalone icon buttons must supply aria-label on the wrapper, not
   * on this component. Default: true. */
  decorative?: boolean;
}

const SIZE_PX: Record<PIconSize, number> = {
  xs: 14,
  sm: 16,
  md: 18,
  lg: 22,
  xl: 28,
};

/**
 * Wraps a Lucide icon at the line-style stroke width the design handoff
 * specifies (1.6). Centralizing the stroke width here means the rest of
 * the codebase doesn't have to remember to override it everywhere.
 */
export function PIcon({ icon: Icon, size = "sm", decorative = true, ...rest }: PIconProps) {
  return (
    <Icon
      size={SIZE_PX[size]}
      strokeWidth={1.6}
      aria-hidden={decorative ? true : undefined}
      {...rest}
    />
  );
}
