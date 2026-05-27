import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import { pagesApi } from "@/lib/api/pages";
import type { PageListItem, PagedResult } from "@/types/api";
import {
  Btn,
  Chip,
  FilterPills,
  PageHeader,
} from "@/components/shared/admin/EditorialPrimitives";

type Tab = "all" | "published" | "draft" | "members" | "deleted";

export function PagesListPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>("all");
  const [search, setSearch] = useState("");
  const [data, setData] = useState<PagedResult<PageListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    pagesApi.list({
      search: search || undefined,
      includeDeleted: tab === "deleted",
      pageSize: 50,
    })
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load pages."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [tab, search]);

  const filtered = useMemo(() => {
    if (!data) return [];
    if (tab === "published") return data.items.filter((p) => p.isPublished);
    if (tab === "draft") return data.items.filter((p) => !p.isPublished);
    if (tab === "members") return data.items.filter((p) => p.isMembersOnly);
    return data.items;
  }, [data, tab]);

  const counts = useMemo(() => {
    const items = data?.items ?? [];
    return {
      all: items.length,
      published: items.filter((p) => p.isPublished).length,
      draft: items.filter((p) => !p.isPublished).length,
      members: items.filter((p) => p.isMembersOnly).length,
      deleted: tab === "deleted" ? items.length : 0,
    };
  }, [data, tab]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={
          data
            ? `${counts.all} active · ${counts.draft} drafts · ${counts.deleted} deleted`
            : "Loading…"
        }
        title="Pages"
        kicker="every static page on the public site"
        actions={
          <Btn
            variant="accent"
            size="lg"
            onClick={() => navigate("/admin/pages/new")}
          >
            New page
          </Btn>
        }
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <FilterPills
          activeValue={tab}
          onChange={(v) => setTab(v as Tab)}
          items={[
            { value: "all", label: "All", count: counts.all },
            { value: "published", label: "Published", count: counts.published },
            { value: "draft", label: "Drafts", count: counts.draft },
            { value: "members", label: "Members only", count: counts.members },
            { value: "deleted", label: "Deleted" },
          ]}
        />
        <input
          type="search"
          placeholder="Search title or slug…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-9 w-full max-w-xs border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-accent"
        />
      </div>

      {loading && <p className="text-muted">Loading…</p>}
      {!loading && error && <p className="text-danger">{error}</p>}
      {!loading && !error && filtered.length === 0 && (
        <p className="text-muted">No pages found.</p>
      )}
      {!loading && !error && filtered.length > 0 && (
        <article className="border border-border bg-panel">
          <header
            className="grid items-center gap-4 border-b border-border bg-panel-alt px-5 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted"
            style={{ gridTemplateColumns: "3fr 1.1fr 1.4fr 1fr" }}
          >
            <span>Page</span>
            <span>Status</span>
            <span>Last edited</span>
            <span className="text-right">Actions</span>
          </header>
          <ul className="divide-y divide-border-soft">
            {filtered.map((p) => (
              <li
                key={p.id}
                className="grid items-center gap-4 px-5 py-3"
                style={{ gridTemplateColumns: "3fr 1.1fr 1.4fr 1fr" }}
              >
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <button
                      type="button"
                      onClick={() => navigate(`/admin/pages/${p.id}`)}
                      className="truncate text-left font-heading text-base font-semibold hover:underline"
                    >
                      {p.title}
                    </button>
                    {p.isSystemPage && <Chip tone="muted">System</Chip>}
                    {p.isMembersOnly && <Chip tone="accent">Members</Chip>}
                  </div>
                  <p className="mt-1 truncate font-mono text-xs text-muted">
                    /{p.slug}
                  </p>
                </div>
                <div>
                  {p.isPublished
                    ? <Chip tone="success" dot>Published</Chip>
                    : <Chip tone="warn" dot>Draft</Chip>}
                </div>
                <div className="text-xs">
                  <p>{relTime(p.modifiedAt)}</p>
                  <p className="font-mono text-muted">
                    {new Date(p.modifiedAt).toLocaleDateString()}
                  </p>
                </div>
                <div className="flex items-center justify-end gap-2">
                  <Link
                    to={`/${p.slug}`}
                    target="_blank"
                    rel="noreferrer"
                    className="text-xs font-medium text-fg-soft hover:text-foreground hover:underline"
                  >
                    View
                  </Link>
                  <Btn
                    size="sm"
                    iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                    onClick={() => navigate(`/admin/pages/${p.id}`)}
                  >
                    Edit
                  </Btn>
                </div>
              </li>
            ))}
          </ul>
        </article>
      )}
    </div>
  );
}

function relTime(iso: string): string {
  const ms = Date.now() - new Date(iso).getTime();
  const min = Math.floor(ms / 60000);
  if (min < 1) return "just now";
  if (min < 60) return `${min} min ago`;
  const hr = Math.floor(min / 60);
  if (hr < 24) return `${hr} hr ago`;
  const day = Math.floor(hr / 24);
  if (day < 7) return `${day} d ago`;
  return new Date(iso).toLocaleDateString();
}
