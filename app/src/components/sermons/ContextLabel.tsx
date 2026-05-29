/**
 * Uppercase mono caps label with a leading color dot, used to surface the
 * teaching track a sermon series ran in ("AM Worship", "Wednesday Night",
 * etc.). Dots are colored by the context's position in the church's
 * configured contexts list, so the palette is stable per church even when
 * the list is customized.
 */

const PALETTE = [
  "hsl(var(--accent))",
  "hsl(var(--foreground))",
  "hsl(var(--muted))",
  "hsl(var(--success))",
  "hsl(var(--warn))",
  "hsl(var(--danger))",
];

const FALLBACK_COLOR = "hsl(var(--muted))";

export interface ContextLabelProps {
  context: string;
  /** Ordered list of all contexts the church has configured (from
   * Site Settings → Content → Sermons). The dot color is keyed off this
   * list's index. Pass an empty array to fall back to the muted color. */
  contexts: string[];
  /** Font size in px. Defaults to 11; the "Also running" compact row
   * uses 10. */
  size?: number;
  className?: string;
}

export function ContextLabel({ context, contexts, size = 11, className }: ContextLabelProps) {
  const index = contexts.findIndex((c) => c === context);
  const color = index >= 0 ? PALETTE[index % PALETTE.length] : FALLBACK_COLOR;
  return (
    <span
      className={["inline-flex items-center gap-2 font-mono uppercase tracking-[0.14em] text-muted", className ?? ""].join(" ")}
      style={{ fontSize: `${size}px` }}
    >
      <span
        aria-hidden="true"
        className="inline-block h-2 w-2 rounded-full"
        style={{ backgroundColor: color }}
      />
      <span>{context}</span>
    </span>
  );
}
