import { useEffect, useRef } from "react";
import { X } from "lucide-react";
import { SideRail, type SideRailProps } from "./SideRail";

/**
 * Off-canvas drawer that hosts the side-rail on mobile (< 768px). Same
 * `<SideRail>` content as desktop; the drawer just wraps it with a focus
 * trap, backdrop, and slide-in animation that respects
 * `prefers-reduced-motion`.
 *
 * Tap backdrop, click the close button, or press Escape to dismiss.
 */
export interface MobileRailDrawerProps extends SideRailProps {
  open: boolean;
  onClose: () => void;
}

export function MobileRailDrawer({ open, onClose, ...railProps }: MobileRailDrawerProps) {
  const drawerRef = useRef<HTMLDivElement>(null);
  const closeBtnRef = useRef<HTMLButtonElement>(null);
  const previouslyFocusedRef = useRef<HTMLElement | null>(null);

  // Open lifecycle: lock body scroll, move focus into the drawer, listen
  // for Esc, restore focus on close.
  useEffect(() => {
    if (!open) return;

    previouslyFocusedRef.current = document.activeElement as HTMLElement | null;
    closeBtnRef.current?.focus();

    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const handleKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        e.preventDefault();
        onClose();
        return;
      }
      // Cycle focus inside the drawer (rudimentary Tab trap).
      if (e.key === "Tab" && drawerRef.current) {
        const focusables = drawerRef.current.querySelectorAll<HTMLElement>(
          'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])',
        );
        if (focusables.length === 0) return;
        const first = focusables[0];
        const last = focusables[focusables.length - 1];
        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    };

    document.addEventListener("keydown", handleKey);
    return () => {
      document.removeEventListener("keydown", handleKey);
      document.body.style.overflow = prevOverflow;
      previouslyFocusedRef.current?.focus();
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 md:hidden">
      {/* Backdrop */}
      <button
        type="button"
        aria-label="Close archive index"
        onClick={onClose}
        className="absolute inset-0 bg-foreground/45 transition-opacity duration-[var(--motion-md)] [transition-timing-function:var(--motion-ease)] motion-reduce:duration-0"
      />

      {/* Drawer panel */}
      <div
        ref={drawerRef}
        role="dialog"
        aria-modal="true"
        aria-label="Archive index"
        className="absolute left-0 top-0 flex h-full w-80 max-w-[100vw] flex-col bg-background shadow-xl transition-transform duration-[var(--motion-md)] [transition-timing-function:var(--motion-ease)] motion-reduce:duration-0"
      >
        <div className="flex items-center justify-between border-b border-border-soft px-5 py-3">
          <span className="font-mono text-[11px] font-semibold uppercase tracking-[0.18em] text-muted">
            Browse archive
          </span>
          <button
            ref={closeBtnRef}
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="inline-flex h-8 w-8 items-center justify-center border border-border-soft hover:bg-panel-alt"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-5 py-4">
          <SideRail {...railProps} />
        </div>
      </div>
    </div>
  );
}
