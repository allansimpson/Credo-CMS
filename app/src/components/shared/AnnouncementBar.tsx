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
      className={`flex flex-wrap items-center justify-center gap-3 px-4 py-2 text-sm ${palette}`}>
      <span className="font-medium">{banner.message}</span>
      {banner.linkUrl && (
        <a href={banner.linkUrl} className="underline" rel="noreferrer">
          {banner.linkLabel ?? "Learn more"}
        </a>
      )}
      <button
        type="button"
        aria-label="Dismiss"
        onClick={() => {
          window.sessionStorage.setItem(DISMISSED_KEY, "true");
          setDismissed(true);
        }}
        className="ml-auto rounded px-2 py-0.5 text-xs hover:bg-black/10"
      >
        Dismiss
      </button>
    </div>
  );
}

function severityClasses(s: AnnouncementSeverity): string {
  switch (s) {
    case 2: return "bg-red-600 text-white";
    case 1: return "bg-amber-500 text-amber-50";
    case 0:
    default: return "bg-sky-600 text-white";
  }
}
