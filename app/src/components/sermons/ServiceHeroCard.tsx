import { Link } from "react-router-dom";
import type { SermonListItem } from "@/lib/api/sermons";
import { getServiceTypeInfo } from "./serviceTypeLabels";
import { Chip, ImageSlot } from "@/components/public";

export function ServiceHeroCard({ sermon }: { sermon: SermonListItem }) {
  const info = getServiceTypeInfo(sermon.serviceType);
  const d = new Date(sermon.publishedAt);
  const time = d.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" });

  return (
    <Link to={`/sermons/${sermon.slug}`} className="group grid gap-7 md:grid-cols-[320px_1fr]">
      {/* Thumbnail */}
      <div className="relative">
        {sermon.thumbnailBlobUrl ? (
          <img
            src={sermon.thumbnailBlobUrl}
            alt=""
            className="w-full object-cover"
            style={{ aspectRatio: "16/10" }}
          />
        ) : (
          <ImageSlot ratio="16:9" label={info.label} alt="" tone="inverse" />
        )}
        <Chip tone={info.chipTone} className="absolute left-3.5 top-3.5 border-0 text-[10px]">
          {info.label}
        </Chip>
        <span className="absolute inset-0 flex items-center justify-center">
          <span className="flex h-14 w-14 items-center justify-center bg-accent text-accent-foreground transition-transform group-hover:scale-110">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor"><polygon points="6,3 17,10 6,17" /></svg>
          </span>
        </span>
      </div>

      {/* Content */}
      <div className="flex flex-col justify-center">
        <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-accent">
          {sermon.sermonSeriesTitle ?? info.label}
        </p>
        <h3
          className="mt-2 font-heading font-semibold leading-snug group-hover:underline"
          style={{ fontSize: 32, letterSpacing: "-0.02em", lineHeight: 1.15 }}
        >
          {sermon.title}
        </h3>
        <p className="mt-3 font-mono text-xs text-muted">
          {sermon.speakerName && <span>{sermon.speakerName}</span>}
          {sermon.speakerName && " · "}
          {time}
        </p>
      </div>
    </Link>
  );
}
