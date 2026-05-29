import { CoverageBar } from "./CoverageBar";

export interface ActiveProgressProps {
  sermonCount: number;
  plannedParts: number | null;
  className?: string;
}

/**
 * Hero progress strip for the active "Now preaching" series. When
 * <c>plannedParts</c> is set and not overrun, renders the coverage bar
 * with a "PART n OF ~m · p%" caption. When the planned total is null OR
 * the actual count has caught up to / passed the planned total, falls
 * back to the open-ended "{n} PARTS · ONGOING" form.
 */
export function ActiveProgress({ sermonCount, plannedParts, className }: ActiveProgressProps) {
  const safeCount = Math.max(0, sermonCount);
  const knownTotal = plannedParts !== null && plannedParts > 0 && safeCount < plannedParts;

  return (
    <div className={["space-y-2", className ?? ""].join(" ")}>
      {knownTotal ? (
        <>
          <CoverageBar
            covered={safeCount}
            total={plannedParts!}
            height={8}
            ariaLabel={`Part ${safeCount} of approximately ${plannedParts}`}
          />
          <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
            Part {safeCount} of ~{plannedParts} · {Math.round((safeCount / plannedParts!) * 100)}%
          </p>
        </>
      ) : (
        <>
          <div
            aria-hidden="true"
            className="w-full"
            style={{ height: 8, backgroundColor: "hsl(var(--accent) / 0.85)" }}
          />
          <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
            {safeCount} {safeCount === 1 ? "Part" : "Parts"} · Ongoing
          </p>
        </>
      )}
    </div>
  );
}
