import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { publicSermonsApi } from "@/lib/api/publicSermons";
import type { SermonListItem } from "@/lib/api/sermons";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { PagedResult } from "@/types/api";

export function SermonsArchivePage() {
  const { settings } = useSiteSettings();
  const [params, setParams] = useSearchParams();
  const initialQ = params.get("q") ?? "";
  const tagSlug = params.get("tag") ?? undefined;
  const [search, setSearch] = useState(initialQ);
  const [data, setData] = useState<PagedResult<SermonListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    publicSermonsApi.list({ search: initialQ || undefined, tagSlug, page, pageSize: 12 })
      .then((d) => { if (!cancelled) setData(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [initialQ, tagSlug, page]);

  const submitSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const next = new URLSearchParams(params);
    if (search) next.set("q", search); else next.delete("q");
    setParams(next);
    setPage(1);
  };

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <SeoTags
        title={`Sermons${tagSlug ? ` · #${tagSlug}` : ""} · ${settings?.churchName ?? ""}`}
        description="Browse our sermon archive." />

      <header className="mb-6">
        <h1 className="text-3xl font-bold sm:text-4xl">Sermons</h1>
        <nav className="mt-3 flex flex-wrap gap-3 text-sm">
          <Link to="/sermons" className="text-primary hover:underline">Latest</Link>
          <Link to="/sermons/series" className="text-primary hover:underline">By Series</Link>
          <Link to="/sermons/by-book" className="text-primary hover:underline">By Book</Link>
        </nav>
        <form onSubmit={submitSearch} className="mt-4 flex gap-2">
          <input type="search" value={search} onChange={(e) => setSearch(e.target.value)}
            placeholder="Search sermons…"
            className="h-10 flex-1 border bg-background px-3 text-sm" />
          <button type="submit"
            className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
            Search
          </button>
        </form>
        {tagSlug && (
          <p className="mt-2 text-xs text-muted-foreground">
            Filtered by tag: <strong>{tagSlug}</strong>{" "}
            <Link to="/sermons" className="text-primary hover:underline">clear</Link>
          </p>
        )}
      </header>

      {loading && <p className="text-muted-foreground">Loading…</p>}
      {!loading && data && data.items.length === 0 && (
        <p className="text-muted-foreground">No sermons found.</p>
      )}

      <ul className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {data?.items.map((s) => (
          <li key={s.id} className="border bg-card">
            <Link to={`/sermons/${s.slug}`} className="block">
              {s.thumbnailBlobUrl ? (
                <picture>
                  {s.thumbnailWebpBlobUrl && <source srcSet={s.thumbnailWebpBlobUrl} type="image/webp" />}
                  <img src={s.thumbnailBlobUrl} alt="" className="aspect-video w-full object-cover" />
                </picture>
              ) : (
                <div className="aspect-video w-full bg-muted" />
              )}
              <div className="p-4">
                <h2 className="font-semibold hover:underline">{s.title}</h2>
                <p className="mt-1 text-xs text-muted-foreground">
                  {s.speakerName ?? "—"}
                  {s.sermonSeriesTitle && ` · ${s.sermonSeriesTitle}`}
                </p>
                <p className="text-xs text-muted-foreground">
                  {new Date(s.publishedAt).toLocaleDateString()}
                  {s.isMembersOnly && " · Members only"}
                </p>
              </div>
            </Link>
          </li>
        ))}
      </ul>

      {data && data.totalPages > 1 && (
        <div className="mt-6 flex items-center justify-center">
          <button type="button" onClick={() => setPage((p) => p + 1)}
            disabled={page >= data.totalPages}
            className="inline-flex h-10 items-center justify-center border bg-card px-4 text-sm hover:bg-muted disabled:opacity-50">
            Load more
          </button>
        </div>
      )}
    </div>
  );
}
