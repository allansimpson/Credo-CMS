import { useEffect, useRef } from "react";
import { X } from "lucide-react";

export interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: "default" | "danger";
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  variant = "default",
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const el = dialogRef.current;
    if (!el) return;
    if (open && !el.open) el.showModal();
    else if (!open && el.open) el.close();
  }, [open]);

  if (!open) return null;

  const confirmClass =
    variant === "danger"
      ? "bg-danger text-danger-foreground hover:bg-danger/90"
      : "bg-accent text-accent-foreground hover:bg-accent/90";

  return (
    <dialog
      ref={dialogRef}
      onCancel={onCancel}
      className="fixed left-1/2 top-1/2 z-50 w-[calc(100%-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2 border-0 bg-transparent p-0 backdrop:bg-black/40"
    >
      <div className="border border-border bg-panel shadow-lg">
        <div className="flex items-center justify-between border-b border-border-soft px-5 py-3.5">
          <h2 className="font-heading text-sm font-semibold">{title}</h2>
          <button
            type="button"
            onClick={onCancel}
            className="flex h-7 w-7 items-center justify-center text-muted hover:text-foreground"
            aria-label="Close"
          >
            <X size={16} strokeWidth={1.5} />
          </button>
        </div>
        <div className="px-5 py-4">
          <p className="text-sm text-fg-soft leading-relaxed">{message}</p>
        </div>
        <div className="flex justify-end gap-2 border-t border-border-soft px-5 py-3">
          <button
            type="button"
            onClick={onCancel}
            className="h-9 border border-border px-4 text-sm font-medium hover:bg-panel-alt"
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            onClick={onConfirm}
            className={`h-9 px-4 text-sm font-semibold ${confirmClass}`}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </dialog>
  );
}
