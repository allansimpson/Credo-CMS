import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { searchApi, type SearchResults } from "@/lib/api/search";
import { SeoTags } from "@/components/shared/SeoTags";

export function SearchPage() {
  const [params, setParams] = useSearchParams();
  const initialQ = params.get("q") ?? "";
  const initialPage = parseInt(params.get("page") ?? "1", 10) || 1;
  const [q, setQ] = useState(initialQ);
  const [page, setPage] = useState(initialPage);
  const [results, setResults] = useState<SearchResults | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!q.trim()) { setResults(null); return; }
    let cancelled = false;
    setLoading(true);
    searchApi.search(q, page)
      .then((r) => { if (!cancelled) setResults(r); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [q, page]);

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    setParams({ q, page: "1" });
  };

  return (
    <div className="mx-auto max-w-3xl px-4 py-8">
      <SeoTags title={q ? `Search: ${q}` : "Search"} description="Search the site." />
      <h1 className="text-3xl font-bold sm:text-4xl">Search</h1>

      <form onSubmit={submit} className="mt-4 flex gap-2">
        <input
          type="search"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="Search pages, news, leaders, documents…"
          className="h-11 flex-1 rounded-md border bg-background px-3 text-sm"
        />
        <button type="submit"
          className="inline-flex h-11 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
          Search
        </button>
      </form>

      {loading && <p className="mt-6 text-muted">Searching…</p>}
      {!loading && results && results.items.length === 0 && (
        <p className="mt-6 text-muted">No results.</p>
      )}

      {results && results.items.length > 0 && (
        <ul className="mt-6 space-y-4">
          {results.items.map((r) => (
            <li key={`${r.entityType}-${r.entityId}`} className="rounded-lg border bg-card p-4">
              <Link to={r.url} className="block">
                <h2 className="font-semibold hover:underline">{r.title}</h2>
                <p className="mt-1 text-xs uppercase text-muted">
                  {r.entityType}{r.isMembersOnly && " · Members only"}
                </p>
                {r.snippet && <p className="mt-2 text-sm text-muted">{r.snippet}</p>}
              </Link>
            </li>
          ))}
        </ul>
      )}

      {results && results.totalPages > 1 && (
        <div className="mt-6 flex items-center justify-between text-sm">
          <button type="button" onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded-md border bg-card px-3 py-1.5 hover:bg-panel-alt disabled:opacity-50">
            Previous
          </button>
          <span className="text-muted">
            Page {results.page} of {results.totalPages} · {results.totalCount} result{results.totalCount === 1 ? "" : "s"}
          </span>
          <button type="button" onClick={() => setPage((p) => Math.min(results.totalPages, p + 1))}
            disabled={page >= results.totalPages}
            className="rounded-md border bg-card px-3 py-1.5 hover:bg-panel-alt disabled:opacity-50">
            Next
          </button>
        </div>
      )}
    </div>
  );
}
