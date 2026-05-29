import { useEffect, useState, createContext, useContext, useCallback, type ReactNode } from "react";
import { X, CheckCircle, AlertTriangle, Info, AlertCircle } from "lucide-react";

export type ToastType = "success" | "warning" | "error" | "info";

interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

interface ToastContextValue {
  toast: (type: ToastType, message: string) => void;
}

const ToastContext = createContext<ToastContextValue>({ toast: () => {} });

export function useToast() {
  return useContext(ToastContext);
}

let nextId = 0;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const addToast = useCallback((type: ToastType, message: string) => {
    const id = ++nextId;
    setToasts((prev) => [...prev, { id, type, message }]);
  }, []);

  const dismiss = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  return (
    <ToastContext.Provider value={{ toast: addToast }}>
      {children}
      <div className="fixed bottom-6 right-6 z-[100] flex flex-col gap-2.5">
        {toasts.map((t) => (
          <ToastItem key={t.id} toast={t} onDismiss={dismiss} />
        ))}
      </div>
    </ToastContext.Provider>
  );
}

const TOAST_STYLES: Record<ToastType, { borderClass: string; iconClass: string; Icon: typeof CheckCircle }> = {
  success: { borderClass: "border-l-accent", iconClass: "text-accent", Icon: CheckCircle },
  warning: { borderClass: "border-l-[hsl(38_92%_50%)]", iconClass: "text-[hsl(38_92%_50%)]", Icon: AlertTriangle },
  error: { borderClass: "border-l-danger", iconClass: "text-danger", Icon: AlertCircle },
  info: { borderClass: "border-l-foreground", iconClass: "text-foreground", Icon: Info },
};

function ToastItem({ toast, onDismiss }: { toast: Toast; onDismiss: (id: number) => void }) {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    requestAnimationFrame(() => setVisible(true));
    const timer = setTimeout(() => onDismiss(toast.id), 5000);
    return () => clearTimeout(timer);
  }, [toast.id, onDismiss]);

  const { borderClass, iconClass, Icon } = TOAST_STYLES[toast.type];

  return (
    <div
      role="alert"
      className={[
        "flex w-80 items-start gap-3 border border-border-soft border-l-4 bg-panel px-4 py-3 shadow-lg transition-all duration-300",
        borderClass,
        visible ? "translate-x-0 opacity-100" : "translate-x-4 opacity-0",
      ].join(" ")}
    >
      <Icon size={18} strokeWidth={1.5} className={`mt-0.5 shrink-0 ${iconClass}`} />
      <p className="flex-1 text-sm text-foreground leading-snug">{toast.message}</p>
      <button
        type="button"
        onClick={() => onDismiss(toast.id)}
        className="shrink-0 text-muted hover:text-foreground"
        aria-label="Dismiss"
      >
        <X size={14} strokeWidth={1.5} />
      </button>
    </div>
  );
}
