import { useEffect, useState } from "react";
import { announcementApi } from "@/lib/api/announcement";
import type { AnnouncementSeverity, PublicAnnouncementBanner } from "@/types/api";

const DISMISSED_KEY = "credocms:banner-dismissed";

/**
 * Public-site dismissible announcement bar. Rendered above the public nav.
 * Dismissal is per-session via sessionStorage; a fresh tab will see the
 * banner again. Severity drives colour.
 */
export function AnnouncementBar() {
  const [banner, setBanner] = useState<PublicAnnouncementBanner | null>(null);
  const [dismissed, setDismissed] = useState(false);

  useEffect(() => {
    let cancelled = false;
    announcementApi.getPublic()
      .then((b) => { if (!cancelled) setBanner(b); })
      .catch(() => {});
    if (window.sessionStorage.getItem(DISMISSED_KEY) === "true") {
      setDismissed(true);
    }
    return () => { cancelled = true; };
  }, []);

  if (!banner || dismissed) return null;

  const palette = severityClasses(banner.severity);

  return (
    <div role="region" aria-label="Site announcement"
      className={`flex items-center justify-center gap-2 px-4 py-2 text-xs ${palette}`}>
      <span className="font-semibold uppercase tracking-[0.12em] text-accent">This Sunday</span>
      <span aria-hidden className="opacity-40">·</span>
      <span>{banner.message}</span>
      {banner.linkUrl && (
        <a href={banner.linkUrl} className="ml-1 underline underline-offset-2" rel="noreferrer">
          {banner.linkLabel ?? "Learn more"}
        </a>
      )}
    </div>
  );
}

function severityClasses(s: AnnouncementSeverity): string {
  switch (s) {
    case 2: return "bg-red-600 text-white";
    case 1: return "bg-amber-500 text-amber-50";
    case 0:
    default: return "bg-inset text-inset-foreground";
  }
}
