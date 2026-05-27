import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { eventsApi, type PublicEventListItem } from "@/lib/api/events";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { PagedResult } from "@/types/api";

export function PublicEventsListPage() {
  const { settings } = useSiteSettings();
  const [data, setData] = useState<PagedResult<PublicEventListItem> | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    eventsApi.listPublic(page, 12)
      .then((d) => { if (!cancelled) setData(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [page]);

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <SeoTags
        title={`Events · ${settings?.churchName ?? ""}`}
        description="Upcoming events and gatherings." />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-3xl font-bold sm:text-4xl">Events</h1>
        <Link to="/calendar" className="text-sm text-primary hover:underline">View calendar →</Link>
      </div>

      {loading && <p className="mt-6 text-muted">Loading…</p>}
      {!loading && data && data.items.length === 0 && (
        <p className="mt-6 text-muted">No upcoming events.</p>
      )}

      <ul className="mt-6 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {data?.items.map((e) => {
          const next = e.nextOccurrenceAt;
          return (
            <li key={e.id} className="border bg-card">
              <Link to={`/events/${e.slug}`} className="block">
                {e.heroImageUrl ? (
                  <picture>
                    {e.heroImageWebpUrl && <source srcSet={e.heroImageWebpUrl} type="image/webp" />}
                    <img src={e.heroImageUrl} alt={e.heroImageAlt ?? ""} className="aspect-video w-full object-cover" />
                  </picture>
                ) : (
                  <div className="aspect-video w-full bg-panel-alt" />
                )}
                <div className="p-4">
                  <h2 className="font-semibold hover:underline">{e.title}</h2>
                  <p className="mt-1 text-xs text-muted">
                    {new Date(next).toLocaleString()}
                    {e.recurrenceRule && " · Recurring"}
                  </p>
                  {e.location && <p className="text-xs text-muted">{e.location}</p>}
                </div>
              </Link>
            </li>
          );
        })}
      </ul>

      {data && data.totalPages > 1 && (
        <div className="mt-6 flex items-center justify-center">
          <button type="button" onClick={() => setPage((p) => p + 1)} disabled={page >= data.totalPages}
            className="inline-flex h-10 items-center justify-center border bg-card px-4 text-sm hover:bg-panel-alt disabled:opacity-50">
            Load more
          </button>
        </div>
      )}
    </div>
  );
}
