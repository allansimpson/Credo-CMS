import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { newsApi } from "@/lib/api/news";
import type { NewsListItem, PagedResult } from "@/types/api";

type Tab = "active" | "deleted";

export function NewsListPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>("active");
  const [search, setSearch] = useState("");
  const [data, setData] = useState<PagedResult<NewsListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    newsApi.list({ search: search || undefined, includeDeleted: tab === "deleted", pageSize: 50 })
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load news."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [tab, search]);

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">News</h1>
        <Link
          to="/admin/news/new"
          className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
        >
          New news item
        </Link>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-3 border-b">
        <Tab current={tab} onSelect={setTab} value="active" label="Active" />
        <Tab current={tab} onSelect={setTab} value="deleted" label="Deleted" />
        <input
          type="search"
          placeholder="Search title or slug…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="ml-auto h-10 w-full max-w-xs rounded-md border bg-background px-3 text-sm"
        />
      </div>

      <div className="mt-4">
        {loading && <p className="text-muted">Loading…</p>}
        {!loading && error && <p className="text-danger">{error}</p>}
        {!loading && !error && data && data.items.length === 0 && (
          <p className="text-muted">No news items found.</p>
        )}
        {!loading && !error && data && data.items.length > 0 && (
          <ul className="divide-y rounded-lg border bg-card">
            {data.items.map((n) => (
              <li
                key={n.id}
                className="flex flex-col gap-2 p-4 sm:flex-row sm:items-center sm:gap-4"
              >
                <div className="flex-1">
                  <button
                    type="button"
                    onClick={() => navigate(`/admin/news/${n.id}`)}
                    className="text-left font-semibold hover:underline"
                  >
                    {n.title}
                  </button>
                  <p className="text-xs text-muted">/news/{n.slug}</p>
                </div>
                <div className="flex flex-wrap items-center gap-2 text-xs">
                  {n.isPublished
                    ? <Badge color="emerald">Published</Badge>
                    : <Badge color="amber">Draft</Badge>}
                  {n.isMembersOnly && <Badge color="indigo">Members only</Badge>}
                  {n.expiresAt && new Date(n.expiresAt) <= new Date() && (
                    <Badge color="muted">Expired</Badge>
                  )}
                </div>
                <p className="text-xs text-muted sm:w-40 sm:text-right">
                  {new Date(n.publishedAt ?? n.modifiedAt).toLocaleString()}
                </p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function Tab({ current, value, label, onSelect }: { current: string; value: "active" | "deleted"; label: string; onSelect: (v: "active" | "deleted") => void }) {
  const active = current === value;
  return (
    <button
      type="button"
      onClick={() => onSelect(value)}
      className={
        "h-10 px-4 text-sm transition-colors " +
        (active
          ? "border-b-2 border-accent text-foreground font-semibold"
          : "text-muted hover:text-foreground")
      }
    >
      {label}
    </button>
  );
}

function Badge({ color, children }: { color: "emerald" | "amber" | "indigo" | "muted"; children: React.ReactNode }) {
  const palette: Record<string, string> = {
    emerald: "bg-emerald-100 text-emerald-800 border-emerald-200",
    amber: "bg-amber-100 text-amber-800 border-amber-200",
    indigo: "bg-indigo-100 text-indigo-800 border-indigo-200",
    muted: "bg-panel-alt text-muted border-border",
  };
  return (
    <span className={`inline-flex items-center rounded-full border px-2 py-0.5 text-xs ${palette[color]}`}>
      {children}
    </span>
  );
}
