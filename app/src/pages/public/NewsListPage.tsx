import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { newsApi } from "@/lib/api/news";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { PagedResult, PublicNewsItem } from "@/types/api";

export function PublicNewsListPage() {
  const { settings } = useSiteSettings();
  const [data, setData] = useState<PagedResult<PublicNewsItem> | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    newsApi.listPublic(page, 10)
      .then((d) => { if (!cancelled) setData(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [page]);

  const title = "News" + (settings ? ` · ${settings.churchName}` : "");

  return (
    <div className="mx-auto max-w-3xl px-4 py-8">
      <SeoTags title={title} description={`Latest news from ${settings?.churchName ?? "the church"}.`} />
      <h1 className="text-3xl font-bold sm:text-4xl">News</h1>

      {loading && <p className="mt-6 text-muted">Loading…</p>}
      {!loading && data && data.items.length === 0 && (
        <p className="mt-6 text-muted">No news to show yet.</p>
      )}

      <ul className="mt-6 space-y-4">
        {data?.items.map((n) => (
          <li key={n.id} className="rounded-lg border bg-card p-4">
            <Link to={`/news/${n.slug}`} className="block">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
                {n.heroImageUrl && (
                  <picture>
                    {n.heroImageWebpUrl && <source srcSet={n.heroImageWebpUrl} type="image/webp" />}
                    <img src={n.heroImageUrl} alt={n.heroImageAlt ?? ""}
                      className="h-32 w-full rounded object-cover sm:w-48" />
                  </picture>
                )}
                <div className="flex-1">
                  <h2 className="text-xl font-semibold hover:underline">{n.title}</h2>
                  <p className="mt-1 text-xs text-muted">
                    {new Date(n.publishedAt).toLocaleDateString()}
                    {n.calendarDate && ` · ${new Date(n.calendarDate).toLocaleDateString()}`}
                    {n.isMembersOnly && " · Members only"}
                  </p>
                  {n.excerpt && <p className="mt-2 text-sm text-muted">{n.excerpt}</p>}
                </div>
              </div>
            </Link>
          </li>
        ))}
      </ul>

      {data && data.totalPages > 1 && (
        <div className="mt-6 flex items-center justify-between text-sm">
          <button
            type="button"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded-md border bg-card px-3 py-1.5 hover:bg-panel-alt disabled:opacity-50"
          >
            Previous
          </button>
          <span className="text-muted">Page {page} of {data.totalPages}</span>
          <button
            type="button"
            onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
            disabled={page >= data.totalPages}
            className="rounded-md border bg-card px-3 py-1.5 hover:bg-panel-alt disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
