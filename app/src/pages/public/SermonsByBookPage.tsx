import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { publicSermonsApi } from "@/lib/api/publicSermons";
import type { SermonListItem } from "@/lib/api/sermons";
import { SeoTags } from "@/components/shared/SeoTags";
import { getBookBySlug } from "@/lib/bible/books";
import { NotFoundPage } from "@/pages/NotFoundPage";
import type { PagedResult } from "@/types/api";

export function SermonsByBookPage() {
  const { bookSlug } = useParams<{ bookSlug: string }>();
  const [data, setData] = useState<PagedResult<SermonListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);

  const info = bookSlug ? getBookBySlug(bookSlug) : undefined;

  useEffect(() => {
    if (!info) return;
    let cancelled = false;
    setLoading(true);
    publicSermonsApi.byBook(info.slug, page)
      .then((d) => { if (!cancelled) setData(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [info, page]);

  if (!info) return <NotFoundPage />;

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <SeoTags title={`Sermons in ${info.name}`} description={`Sermons referencing ${info.name}.`} />
      <p className="text-sm">
        <Link to="/sermons/by-book" className="text-primary hover:underline">← All books</Link>
      </p>
      <h1 className="mt-2 text-3xl font-bold sm:text-4xl">{info.name}</h1>
      <p className="mt-1 text-sm text-muted">
        {info.testament === "OldTestament" ? "Old Testament" : "New Testament"} · {info.chapterCount} chapters
      </p>

      {loading && <p className="mt-6 text-muted">Loading…</p>}
      {!loading && data && data.items.length === 0 && (
        <p className="mt-6 text-muted">No sermons reference {info.name} yet.</p>
      )}

      <ul className="mt-6 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {data?.items.map((s) => (
          <li key={s.id} className="border bg-card">
            <Link to={`/sermons/${s.slug}`} className="block">
              {s.thumbnailBlobUrl ? (
                <picture>
                  {s.thumbnailWebpBlobUrl && <source srcSet={s.thumbnailWebpBlobUrl} type="image/webp" />}
                  <img src={s.thumbnailBlobUrl} alt="" className="aspect-video w-full object-cover" />
                </picture>
              ) : (
                <div className="aspect-video w-full bg-panel-alt" />
              )}
              <div className="p-4">
                <h2 className="font-semibold hover:underline">{s.title}</h2>
                <p className="mt-1 text-xs text-muted">
                  {s.speakerName ?? "—"} · {new Date(s.publishedAt).toLocaleDateString()}
                </p>
              </div>
            </Link>
          </li>
        ))}
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
