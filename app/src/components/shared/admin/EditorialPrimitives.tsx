import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

/**
 * Editorial UI primitives — admin shell only. See DESIGN_HANDOFF.md §4.
 *
 * All primitives respect the global `border-radius: 0 !important` rule, use
 * the system theme tokens (so dark mode swaps automatically), and lean on
 * tabular-nums where numbers appear so tables and stat strips stay aligned.
 */

// ---- MetaLabel -----------------------------------------------------------

/** Uppercase eyebrow with letter-spacing. 10–11px / 600 / 0.14em / muted. */
export function MetaLabel({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "text-[11px] font-semibold uppercase leading-none tracking-[0.16em] text-muted",
        className,
      )}
    >
      {children}
    </span>
  );
}

// ---- BigNum --------------------------------------------------------------

/** Tabular-nums big number. Sizes track the §3 type scale. */
export function BigNum({
  children,
  size = "md",
  className,
}: {
  children: ReactNode;
  size?: "sm" | "md" | "lg" | "xl";
  className?: string;
}) {
  const sizeClass =
    size === "sm" ? "text-[22px]"
    : size === "md" ? "text-[28px]"
    : size === "lg" ? "text-[34px]"
    : "text-[42px]";
  return (
    <span
      style={{ fontVariantNumeric: "tabular-nums" }}
      className={cn(
        "block font-heading font-bold leading-none tracking-[-0.02em]",
        sizeClass,
        className,
      )}
    >
      {children}
    </span>
  );
}

// ---- SectionHead ---------------------------------------------------------

/**
 * Section divider with numeric prefix — `01  Identity ─────────`.
 * Hairline rule below; subtitle optional.
 */
export function SectionHead({
  number,
  title,
  subtitle,
  right,
  className,
}: {
  number: string;
  title: string;
  subtitle?: string;
  right?: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex items-end justify-between gap-4 border-b border-border-soft pb-3",
        className,
      )}
    >
      <div className="flex items-baseline gap-4">
        <span
          style={{ fontVariantNumeric: "tabular-nums" }}
          className="font-mono text-xs font-semibold tracking-wider text-muted"
        >
          {number}
        </span>
        <h2 className="font-heading text-lg font-semibold tracking-tight">
          {title}
        </h2>
        {subtitle && <span className="text-sm text-fg-soft">{subtitle}</span>}
      </div>
      {right && <div className="flex items-center gap-2">{right}</div>}
    </div>
  );
}

// ---- PageHeader ----------------------------------------------------------

/**
 * Top-of-page composite. Eyebrow above title, optional kicker beside title,
 * subtitle below, actions on the right.
 */
export function PageHeader({
  eyebrow,
  title,
  kicker,
  subtitle,
  actions,
  className,
}: {
  eyebrow?: ReactNode;
  title: ReactNode;
  kicker?: ReactNode;
  subtitle?: ReactNode;
  actions?: ReactNode;
  className?: string;
}) {
  return (
    <header
      className={cn(
        "flex flex-wrap items-end justify-between gap-4 border-b border-border-soft pb-6",
        className,
      )}
    >
      <div className="min-w-0 flex-1">
        {eyebrow && <MetaLabel>{eyebrow}</MetaLabel>}
        <div className="mt-2 flex flex-wrap items-baseline gap-3">
          <h1 className="font-heading text-3xl font-semibold leading-tight tracking-[-0.02em]">
            {title}
          </h1>
          {kicker && (
            <span className="text-sm italic text-fg-soft">{kicker}</span>
          )}
        </div>
        {subtitle && (
          <p className="mt-2 max-w-2xl text-sm text-fg-soft">{subtitle}</p>
        )}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </header>
  );
}

// ---- Btn -----------------------------------------------------------------

type BtnVariant = "accent" | "secondary" | "ghost" | "danger";
type BtnSize = "xs" | "sm" | "md" | "lg";

const VARIANT_CLASSES: Record<BtnVariant, string> = {
  accent:
    "bg-accent text-accent-foreground hover:bg-accent/90 disabled:bg-accent/40",
  secondary:
    "border border-border bg-panel text-foreground hover:bg-panel-alt disabled:opacity-50",
  ghost:
    "bg-transparent text-foreground hover:bg-panel-alt disabled:opacity-50",
  danger:
    "border border-border bg-panel text-danger hover:bg-danger/10 disabled:opacity-50",
};

const SIZE_CLASSES: Record<BtnSize, string> = {
  xs: "h-6 px-2 text-[11px]",
  sm: "h-7 px-3 text-xs",
  md: "h-9 px-4 text-sm",
  lg: "h-10 px-5 text-sm",
};

export function Btn({
  variant = "secondary",
  size = "md",
  className,
  iconLeft,
  iconRight,
  children,
  ...rest
}: React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: BtnVariant;
  size?: BtnSize;
  iconLeft?: ReactNode;
  iconRight?: ReactNode;
}) {
  return (
    <button
      type="button"
      {...rest}
      className={cn(
        "inline-flex items-center justify-center gap-2 font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        VARIANT_CLASSES[variant],
        SIZE_CLASSES[size],
        className,
      )}
    >
      {iconLeft}
      {children}
      {iconRight}
    </button>
  );
}

// ---- Chip ----------------------------------------------------------------

type ChipTone = "muted" | "success" | "warn" | "danger" | "accent";

const CHIP_TONE_CLASSES: Record<
  ChipTone,
  { filled: string; outlined: string; dot: string }
> = {
  muted: {
    filled: "bg-panel-alt text-fg-soft",
    outlined: "border border-border text-fg-soft",
    dot: "bg-muted",
  },
  success: {
    filled: "bg-success/15 text-success",
    outlined: "border border-success/40 text-success",
    dot: "bg-success",
  },
  warn: {
    filled: "bg-warn/15 text-warn",
    outlined: "border border-warn/40 text-warn",
    dot: "bg-warn",
  },
  danger: {
    filled: "bg-danger/15 text-danger",
    outlined: "border border-danger/40 text-danger",
    dot: "bg-danger",
  },
  accent: {
    filled: "bg-accent/15 text-accent",
    outlined: "border border-accent/40 text-accent",
    dot: "bg-accent",
  },
};

export function Chip({
  tone = "muted",
  variant = "filled",
  dot = false,
  children,
  className,
}: {
  tone?: ChipTone;
  variant?: "filled" | "outlined";
  dot?: boolean;
  children: ReactNode;
  className?: string;
}) {
  const tones = CHIP_TONE_CLASSES[tone];
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 px-2 py-0.5 text-[11px] font-medium uppercase tracking-wider",
        variant === "filled" ? tones.filled : tones.outlined,
        className,
      )}
    >
      {dot && (
        <span
          aria-hidden
          className={cn("h-1.5 w-1.5 shrink-0", tones.dot)}
        />
      )}
      {children}
    </span>
  );
}

// ---- Avatar --------------------------------------------------------------

type AvatarTone = "accent" | "muted" | "inverse";
type AvatarSize = "sm" | "md" | "lg";

const AVATAR_TONE_CLASSES: Record<AvatarTone, string> = {
  accent: "bg-accent text-accent-foreground",
  muted: "bg-panel-alt text-fg-soft",
  inverse: "bg-foreground text-background",
};

const AVATAR_SIZE_CLASSES: Record<AvatarSize, string> = {
  sm: "h-7 w-7 text-[10px]",
  md: "h-9 w-9 text-xs",
  lg: "h-11 w-11 text-sm",
};

export function Avatar({
  name,
  tone = "muted",
  size = "md",
  className,
}: {
  name: string;
  tone?: AvatarTone;
  size?: AvatarSize;
  className?: string;
}) {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((n) => n[0]?.toUpperCase() ?? "")
    .join("");
  return (
    <span
      aria-hidden
      className={cn(
        "grid shrink-0 place-items-center font-bold leading-none",
        AVATAR_TONE_CLASSES[tone],
        AVATAR_SIZE_CLASSES[size],
        className,
      )}
    >
      {initials || "?"}
    </span>
  );
}

// ---- Switch (square) -----------------------------------------------------

export function SwitchFlat({
  checked,
  onChange,
  disabled,
  label,
  className,
}: {
  checked: boolean;
  onChange: (next: boolean) => void;
  disabled?: boolean;
  label?: string;
  className?: string;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={cn(
        "relative inline-flex h-5 w-10 items-center border border-border bg-panel-alt transition-colors disabled:opacity-50",
        checked && "border-accent bg-accent/20",
        className,
      )}
    >
      <span
        aria-hidden
        className={cn(
          "absolute top-0 h-full w-1/2 transition-transform",
          checked ? "translate-x-full bg-accent" : "translate-x-0 bg-foreground",
        )}
      />
    </button>
  );
}

// ---- FilterPills ---------------------------------------------------------

export interface FilterPillItem {
  label: string;
  value: string;
  count?: number;
}

export function FilterPills({
  items,
  activeValue,
  onChange,
  className,
}: {
  items: FilterPillItem[];
  activeValue: string;
  onChange: (value: string) => void;
  className?: string;
}) {
  return (
    <div
      role="tablist"
      className={cn("inline-flex border border-border bg-panel", className)}
    >
      {items.map((item, i) => {
        const active = item.value === activeValue;
        return (
          <button
            key={item.value}
            type="button"
            role="tab"
            aria-selected={active}
            onClick={() => onChange(item.value)}
            className={cn(
              "inline-flex h-8 items-center gap-2 px-3 text-xs font-medium transition-colors",
              i > 0 && "border-l border-border",
              active
                ? "bg-accent text-accent-foreground"
                : "text-fg-soft hover:bg-panel-alt",
            )}
          >
            <span>{item.label}</span>
            {typeof item.count === "number" && (
              <span
                style={{ fontVariantNumeric: "tabular-nums" }}
                className="font-mono text-[11px] opacity-80"
              >
                {item.count}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}

// ---- Field ---------------------------------------------------------------

export function Field({
  label,
  required,
  hint,
  htmlFor,
  children,
  className,
}: {
  label: string;
  required?: boolean;
  hint?: ReactNode;
  htmlFor?: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("space-y-1.5", className)}>
      <label
        htmlFor={htmlFor}
        className="block text-xs font-medium text-muted"
      >
        {label}
        {required && <span className="ml-1 text-danger">*</span>}
      </label>
      {children}
      {hint && <p className="text-xs text-fg-soft">{hint}</p>}
    </div>
  );
}

// ---- AccentStrip wrapper -------------------------------------------------

export function AccentStrip({
  tone = "accent",
  className,
}: {
  tone?: "accent" | "warn" | "danger" | "success";
  className?: string;
}) {
  const bg =
    tone === "warn" ? "bg-warn"
    : tone === "danger" ? "bg-danger"
    : tone === "success" ? "bg-success"
    : "bg-accent";
  return (
    <span
      aria-hidden
      className={cn("absolute inset-y-0 left-0 w-[3px]", bg, className)}
    />
  );
}
