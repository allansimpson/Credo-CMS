import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowRight, Pin } from "lucide-react";
import { adminBlogApi, type BlogPostListItem } from "@/lib/api/blog";
import {
  Btn,
  Chip,
  FilterPills,
  PageHeader,
} from "@/components/shared/admin/EditorialPrimitives";

type Tab = "all" | "published" | "drafts";

export function AdminBlogListPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>("all");
  const [search, setSearch] = useState("");
  const [items, setItems] = useState<BlogPostListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const isPublished = tab === "all" ? undefined : tab === "published" ? true : false;
      const result = await adminBlogApi.list({
        search: search || undefined,
        isPublished,
        pageSize: 50,
      });
      setItems(result.items);
      setError(null);
    } catch {
      setError("Could not load posts.");
    } finally {
      setLoading(false);
    }
  }, [tab, search]);

  useEffect(() => { void load(); }, [load]);

  const counts = useMemo(() => ({
    all: items.length,
  }), [items]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={`${counts.all} posts`}
        title="Blog"
        kicker="church news, devotionals, reflections"
        actions={
          <Btn variant="accent" size="lg" onClick={() => navigate("/admin/blog/new")}>
            New post
          </Btn>
        }
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <FilterPills
          activeValue={tab}
          onChange={(v) => setTab(v as Tab)}
          items={[
            { value: "all", label: "All" },
            { value: "published", label: "Published" },
            { value: "drafts", label: "Drafts" },
          ]}
        />
        <input
          type="search"
          placeholder="Search title or slug…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-9 w-full max-w-xs border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
        />
      </div>

      {loading && <p className="text-muted">Loading…</p>}
      {error && <p className="text-danger">{error}</p>}
      {!loading && !error && items.length === 0 && <p className="text-muted">No posts match.</p>}

      {!loading && !error && items.length > 0 && (
        <article className="border border-border bg-panel">
          <header
            className="grid items-center gap-4 border-b border-border bg-panel-alt px-5 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted"
            style={{ gridTemplateColumns: "3fr 1.2fr 1fr 1.2fr 1fr" }}
          >
            <span>Title</span>
            <span>Category</span>
            <span>Status</span>
            <span>Published</span>
            <span className="text-right">Actions</span>
          </header>
          <ul className="divide-y divide-border-soft">
            {items.map((p) => (
              <li
                key={p.id}
                className="grid items-center gap-4 px-5 py-3"
                style={{ gridTemplateColumns: "3fr 1.2fr 1fr 1.2fr 1fr" }}
              >
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    {p.isPinned && <Pin className="h-3.5 w-3.5 text-accent" aria-label="Pinned" />}
                    <button
                      type="button"
                      onClick={() => navigate(`/admin/blog/${p.id}`)}
                      className="text-left font-heading text-base font-semibold hover:underline"
                    >
                      {p.title}
                    </button>
                  </div>
                  <p className="mt-1 truncate font-mono text-xs text-muted">/blog/{p.slug}</p>
                </div>
                <span className="text-sm">{p.category}</span>
                <div className="flex flex-col gap-1">
                  {p.isPublished ? <Chip tone="success" dot>Published</Chip> : <Chip tone="warn" dot>Draft</Chip>}
                  {p.isMembersOnly && <Chip tone="accent">Members</Chip>}
                </div>
                <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono text-xs">
                  {p.publishedAt ? new Date(p.publishedAt).toLocaleDateString() : "—"}
                </span>
                <div className="flex justify-end">
                  <Btn
                    size="sm"
                    iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                    onClick={() => navigate(`/admin/blog/${p.id}`)}
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
