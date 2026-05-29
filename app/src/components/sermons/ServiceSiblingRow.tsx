import { Link } from "react-router-dom";
import type { SermonListItem } from "@/lib/api/sermons";
import { getServiceTypeInfo } from "./serviceTypeLabels";

export function ServiceSiblingRow({ sermon }: { sermon: SermonListItem }) {
  const info = getServiceTypeInfo(sermon.serviceType);
  const d = new Date(sermon.publishedAt);
  const time = d.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" });
  const durationMin = sermon.durationSeconds
    ? `${Math.round(sermon.durationSeconds / 60)} min`
    : null;

  return (
    <Link
      to={`/sermons/${sermon.slug}`}
      className="group -mx-3 grid items-center gap-x-5 px-3 py-4 transition-colors hover:bg-panel-alt/50"
      style={{ gridTemplateColumns: "160px 1fr auto auto" }}
    >
      {/* Type + time */}
      <div className="flex items-center gap-2.5">
        <span className={`h-2 w-2 shrink-0 rounded-full ${info.dotClass}`} />
        <span className="font-mono text-[10.5px] font-semibold uppercase tracking-[0.14em] text-muted">
          {info.shortLabel}
        </span>
        <span className="font-mono text-[10.5px] text-muted">{time}</span>
      </div>

      {/* Title + speaker */}
      <div className="min-w-0">
        <h4 className="font-heading text-[17px] font-semibold leading-snug group-hover:underline" style={{ textWrap: "pretty" }}>
          {sermon.title}
        </h4>
        {sermon.speakerName && (
          <p className="mt-0.5 text-xs text-muted">{sermon.speakerName}</p>
        )}
      </div>

      {/* Duration */}
      <div className="text-right font-mono text-xs text-muted">
        {durationMin}
      </div>

      {/* Play button */}
      <span className="flex h-8 w-8 items-center justify-center bg-accent-soft text-accent">
        <svg width="12" height="12" viewBox="0 0 20 20" fill="currentColor"><polygon points="6,3 17,10 6,17" /></svg>
      </span>
    </Link>
  );
}
