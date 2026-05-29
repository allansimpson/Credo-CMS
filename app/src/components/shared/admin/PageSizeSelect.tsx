import { ChevronDown } from "lucide-react";

/**
 * Per-page selector chip for paginated admin tables. Mono caps label
 * ("PER PAGE") + a bordered select displaying the current value. Changing
 * the value resets the table to page 1 — that's the caller's job; this
 * component just emits the new size.
 *
 * Generic: defaults to the spec's 25/50/100 set, but any set can be passed.
 */
export interface PageSizeSelectProps {
  value: number;
  onChange: (size: number) => void;
  options?: readonly number[];
  disabled?: boolean;
}

export function PageSizeSelect({
  value,
  onChange,
  options = [25, 50, 100],
  disabled = false,
}: PageSizeSelectProps) {
  return (
    <label className="inline-flex items-center gap-2">
      <span className="font-mono text-[10.5px] font-semibold uppercase tracking-[0.16em] text-muted">
        Per page
      </span>
      <span className="relative inline-flex items-center">
        <select
          aria-label="Rows per page"
          disabled={disabled}
          value={value}
          onChange={(e) => onChange(parseInt(e.target.value, 10))}
          className="h-8 appearance-none border border-border bg-background pl-3 pr-7 font-mono text-xs tabular-nums focus-visible:border-accent focus-visible:outline-none disabled:opacity-50"
        >
          {options.map((opt) => (
            <option key={opt} value={opt}>{opt}</option>
          ))}
        </select>
        <ChevronDown
          aria-hidden="true"
          strokeWidth={1.75}
          className="pointer-events-none absolute right-2 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted"
        />
      </span>
    </label>
  );
}
