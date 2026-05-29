import type { ButtonHTMLAttributes, ReactNode } from "react";
import { forwardRef } from "react";
import { Link } from "react-router-dom";
import type { LinkProps } from "react-router-dom";

export type PublicBtnVariant = "primary" | "secondary" | "ghost" | "inverse" | "inverseFilled";
export type PublicBtnSize = "sm" | "md" | "lg";

interface BaseProps {
  variant?: PublicBtnVariant;
  size?: PublicBtnSize;
  /** Icon on the leading edge of the label. */
  icon?: ReactNode;
  /** Icon on the trailing edge — typical "Read more →" treatment. */
  iconRight?: ReactNode;
}

const VARIANT_CLASSES: Record<PublicBtnVariant, string> = {
  // Tenant-overridable accent for filled primaries. Foreground locks to
  // --accent-foreground (template-fixed) so the CTA reads correctly even
  // when the tenant repaints --accent.
  primary:
    "bg-accent text-accent-foreground hover:bg-[color:hsl(var(--accent)/0.92)] " +
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2",
  secondary:
    "border border-border text-foreground hover:border-foreground " +
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-foreground focus-visible:ring-offset-2",
  ghost:
    "text-foreground underline underline-offset-4 hover:text-accent " +
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2",
  inverse:
    "text-inset-foreground border border-inset-foreground/30 hover:border-inset-foreground " +
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset-foreground focus-visible:ring-offset-2 focus-visible:ring-offset-inset",
  inverseFilled:
    "bg-accent text-accent-foreground hover:bg-[color:hsl(var(--accent)/0.92)] " +
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-inset",
};

const SIZE_CLASSES: Record<PublicBtnSize, string> = {
  sm: "h-9 px-3 text-sm",
  md: "h-11 px-5 text-sm",
  lg: "h-12 px-6 text-base",
};

function buildClassName(variant: PublicBtnVariant, size: PublicBtnSize, className?: string) {
  return [
    "inline-flex items-center justify-center gap-2 font-semibold transition-colors",
    VARIANT_CLASSES[variant],
    SIZE_CLASSES[size],
    className ?? "",
  ].join(" ");
}

type ButtonProps = BaseProps & ButtonHTMLAttributes<HTMLButtonElement>;

export const Btn = forwardRef<HTMLButtonElement, ButtonProps>(function Btn(
  { variant = "primary", size = "md", icon, iconRight, children, className, ...rest },
  ref,
) {
  return (
    <button ref={ref} type="button" {...rest} className={buildClassName(variant, size, className)}>
      {icon}
      <span>{children}</span>
      {iconRight}
    </button>
  );
});

type LinkBtnProps = BaseProps & LinkProps & { children: ReactNode };

/**
 * Link-styled variant of <Btn>. Uses react-router's <Link> so SPA
 * navigation works. Same variant / size / icon API.
 */
export function BtnLink({
  variant = "primary",
  size = "md",
  icon,
  iconRight,
  children,
  className,
  ...rest
}: LinkBtnProps) {
  return (
    <Link {...rest} className={buildClassName(variant, size, className)}>
      {icon}
      <span>{children}</span>
      {iconRight}
    </Link>
  );
}
