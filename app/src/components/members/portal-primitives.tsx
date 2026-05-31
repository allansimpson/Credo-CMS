import { type ReactNode } from "react";
import { ArrowLeft, Info, RotateCcw } from "lucide-react";

/**
 * Shared visual primitives for the Member Portal. Inherits the editorial
 * admin palette — zero border-radius, Inter Tight headings, JetBrains Mono
 * for meta/tabular. One consistent treatment across all sections.
 */

// ─── Page header ────────────────────────────────────────────────────────────

export interface PageHeadProps {
  title: string;
  count?: ReactNode;
  sub?: ReactNode;
  actions?: ReactNode;
  onBack?: () => void;
}

export function PageHead({ title, count, sub, actions, onBack }: PageHeadProps) {
  return (
    <div className="mb-5 flex flex-wrap items-start justify-between gap-4">
      <div className="min-w-0">
        {onBack && (
          <button
            type="button"
            onClick={onBack}
            className="mb-3 inline-flex items-center gap-1.5 font-mono text-[11px] uppercase tracking-[0.08em] text-muted hover:text-foreground"
          >
            <ArrowLeft strokeWidth={1.75} className="h-3.5 w-3.5" /> Back
          </button>
        )}
        <div className="flex flex-wrap items-baseline gap-3">
          <h1 className="font-heading text-2xl font-semibold leading-tight tracking-tight md:text-[27px]">
            {title}
          </h1>
          {count != null && (
            <span className="font-mono text-xs text-muted">{count}</span>
          )}
        </div>
        {sub && (
          <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">{sub}</p>
        )}
      </div>
      {actions && <div className="flex shrink-0 flex-wrap gap-2">{actions}</div>}
    </div>
  );
}

// ─── Content wrapper ───────────────────────────────────────────────────────

export interface ContentProps {
  children: ReactNode;
  maxWidth?: string;
}

export function Content({ children, maxWidth = "max-w-[940px]" }: ContentProps) {
  return (
    <div className="px-4 py-5 md:px-8 md:py-7">
      <div className={`mx-auto ${maxWidth}`}>{children}</div>
    </div>
  );
}

// ─── Panel + cards ─────────────────────────────────────────────────────────

export function Panel({
  children,
  className = "",
  noPad = false,
}: { children: ReactNode; className?: string; noPad?: boolean }) {
  return (
    <div className={`border border-border bg-panel ${noPad ? "" : "p-4"} ${className}`}>
      {children}
    </div>
  );
}

export function CardLink({
  children,
  onClick,
  className = "",
  ariaLabel,
}: {
  children: ReactNode;
  onClick?: () => void;
  className?: string;
  ariaLabel?: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={ariaLabel}
      className={`block w-full border border-border bg-panel text-left text-foreground transition-colors hover:bg-panel-alt ${className}`}
    >
      {children}
    </button>
  );
}

// ─── Tabs ──────────────────────────────────────────────────────────────────

export interface SegTab { id: string; label: string }

export function SegTabs({
  tabs,
  active,
  onChange,
}: {
  tabs: SegTab[];
  active: string;
  onChange: (id: string) => void;
}) {
  return (
    <div className="mb-5 flex overflow-x-auto border-b border-border">
      {tabs.map((t) => {
        const isActive = t.id === active;
        return (
          <button
            key={t.id}
            type="button"
            onClick={() => onChange(t.id)}
            aria-current={isActive ? "page" : undefined}
            className={`relative whitespace-nowrap px-4 py-2.5 text-sm transition-colors ${
              isActive
                ? "font-semibold text-foreground"
                : "font-medium text-muted hover:text-foreground"
            }`}
          >
            {t.label}
            {isActive && (
              <span
                aria-hidden="true"
                className="absolute inset-x-0 bottom-[-1px] h-[2px] bg-accent"
              />
            )}
          </button>
        );
      })}
    </div>
  );
}

// ─── Banner ────────────────────────────────────────────────────────────────

type BannerTone = "warn" | "danger" | "accent" | "success" | "info";

export function Banner({
  tone = "info",
  icon,
  children,
  action,
}: {
  tone?: BannerTone;
  icon?: ReactNode;
  children: ReactNode;
  action?: ReactNode;
}) {
  const palette: Record<BannerTone, { border: string; bg: string; text: string }> = {
    warn:    { border: "border-l-warn",    bg: "bg-warn/10",    text: "text-warn" },
    danger:  { border: "border-l-danger",  bg: "bg-danger/10",  text: "text-danger" },
    accent:  { border: "border-l-accent",  bg: "bg-accent/10",  text: "text-accent" },
    success: { border: "border-l-success", bg: "bg-success/10", text: "text-success" },
    info:    { border: "border-l-muted",   bg: "bg-panel-alt",  text: "text-muted" },
  };
  const c = palette[tone];
  return (
    <div className={`mb-4 flex items-center gap-3 border-l-2 ${c.border} ${c.bg} p-3`}>
      {icon && <span className={`shrink-0 ${c.text}`}>{icon}</span>}
      <span className="flex-1 text-sm leading-relaxed text-fg-soft">{children}</span>
      {action}
    </div>
  );
}

// ─── Empty state ───────────────────────────────────────────────────────────

export function EmptyState({
  icon,
  title,
  body,
  action,
}: {
  icon?: ReactNode;
  title: string;
  body?: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center gap-3 border border-dashed border-border bg-panel px-7 py-10 text-center md:py-14">
      {icon && (
        <div className="flex h-11 w-11 items-center justify-center bg-panel-alt text-muted">
          {icon}
        </div>
      )}
      <div className="font-heading text-base font-semibold">{title}</div>
      {body && (
        <div className="max-w-xs text-sm leading-relaxed text-muted">{body}</div>
      )}
      {action && <div className="mt-1">{action}</div>}
    </div>
  );
}

// ─── Inline error ──────────────────────────────────────────────────────────

export function InlineError({
  message = "Couldn't load this. Check your connection and try again.",
  onRetry,
}: { message?: string; onRetry?: () => void }) {
  return (
    <div className="flex items-center gap-3 border border-danger bg-danger/10 p-3">
      <Info strokeWidth={1.75} className="h-4 w-4 shrink-0 text-danger" />
      <span className="flex-1 text-sm leading-relaxed text-danger">{message}</span>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="inline-flex items-center gap-1.5 text-sm font-semibold text-danger underline underline-offset-2"
        >
          <RotateCcw strokeWidth={1.75} className="h-3.5 w-3.5" /> Retry
        </button>
      )}
    </div>
  );
}

// ─── Skeleton ──────────────────────────────────────────────────────────────

export function Skeleton({ className = "" }: { className?: string }) {
  return (
    <div
      aria-hidden="true"
      className={`animate-pulse bg-border-soft ${className}`}
    />
  );
}

export function SkeletonCard() {
  return (
    <Panel className="flex items-center gap-3">
      <Skeleton className="h-11 w-11 shrink-0" />
      <div className="flex flex-1 flex-col gap-2">
        <Skeleton className="h-3 w-7/12" />
        <Skeleton className="h-2.5 w-4/12" />
      </div>
    </Panel>
  );
}

// ─── Meta row (detail views) ───────────────────────────────────────────────

export function MetaRow({
  icon,
  label,
  value,
  action,
}: {
  icon?: ReactNode;
  label: string;
  value: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="flex items-center gap-3 border-b border-border-soft py-2.5">
      {icon && <span className="text-muted">{icon}</span>}
      <span className="w-20 shrink-0 font-mono text-[10px] font-semibold uppercase tracking-[0.12em] text-muted">
        {label}
      </span>
      <span className="flex-1 text-sm text-foreground">{value}</span>
      {action}
    </div>
  );
}

// ─── Quick action tile (Home) ─────────────────────────────────────────────

export function QuickActionTile({
  icon,
  label,
  onClick,
}: {
  icon: ReactNode;
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="group flex items-center gap-3 border border-border bg-accent/[0.07] px-4 py-3 text-left transition-colors hover:bg-accent/15"
    >
      <span className="flex h-9 w-9 items-center justify-center bg-accent text-accent-foreground">
        {icon}
      </span>
      <span className="font-heading text-sm font-semibold leading-tight">{label}</span>
    </button>
  );
}

// ─── Avatar (initials fallback) ───────────────────────────────────────────

export function Avatar({
  name,
  size = 40,
  src,
  webpSrc,
  alt,
  className = "",
}: {
  name: string;
  size?: number;
  src?: string | null;
  webpSrc?: string | null;
  alt?: string | null;
  className?: string;
}) {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
  const fontSize = Math.round(size * 0.4);
  if (src) {
    return (
      <picture>
        {webpSrc && <source srcSet={webpSrc} type="image/webp" />}
        <img
          src={src}
          alt={alt ?? `${name} avatar`}
          width={size}
          height={size}
          className={`shrink-0 object-cover ${className}`}
          style={{ width: size, height: size }}
        />
      </picture>
    );
  }
  return (
    <span
      aria-hidden="true"
      className={`inline-flex shrink-0 items-center justify-center bg-panel-alt font-heading font-semibold text-muted ${className}`}
      style={{ width: size, height: size, fontSize }}
    >
      {initials || "?"}
    </span>
  );
}
