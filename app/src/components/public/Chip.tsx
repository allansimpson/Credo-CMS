import type { ReactNode } from "react";

export type ChipTone = "muted" | "accent" | "inverse";

export interface ChipProps {
  tone?: ChipTone;
  children: ReactNode;
  className?: string;
}

/**
 * Small uppercase letter-spaced label. Used as category chips on cards,
 * status pills, etc. Squared corners (no `rounded-full`) — the only
 * place a filled radius is allowed is the Quiet template's pill-shaped
 * filter chips, which opt in directly via `rounded-full`.
 */
export function Chip({ tone = "muted", children, className }: ChipProps) {
  const toneClass =
    tone === "accent"
      ? "border-accent text-accent bg-[color:hsl(var(--accent)/0.08)]"
      : tone === "inverse"
        ? "border-inset-foreground/30 text-inset-foreground"
        : "border-border text-muted";
  return (
    <span
      className={[
        "inline-flex items-center px-2 py-0.5 text-[10px] font-medium uppercase tracking-[0.14em] border",
        toneClass,
        className ?? "",
      ].join(" ")}
    >
      {children}
    </span>
  );
}
