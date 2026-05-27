import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import { newsApi } from "@/lib/api/news";
import type { NewsListItem, PagedResult } from "@/types/api";
import {
  Btn,
  Chip,
  FilterPills,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";

type Tab = "active" | "drafts" | "members" | "deleted";

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
    newsApi.list({
      search: search || undefined,
      includeDeleted: tab === "deleted",
      pageSize: 50,
    })
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load news."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [tab, search]);

  const filtered = useMemo(() => {
    if (!data) return [];
    if (tab === "drafts") return data.items.filter((n) => !n.isPublished);
    if (tab === "members") return data.items.filter((n) => n.isMembersOnly);
    return data.items;
  }, [data, tab]);

  // First published item is treated as the featured item per §5.4.
  const featured = filtered.find((n) => n.isPublished);
  const rest = featured ? filtered.filter((n) => n.id !== featured.id) : filtered;

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow={data ? `${data.items.length} posts` : "Loading…"}
        title="News"
        kicker="church news, announcements, and updates"
        actions={
          <Btn variant="accent" size="lg" onClick={() => navigate("/admin/news/new")}>
            Compose post
          </Btn>
        }
      />

      {loading && <p className="text-muted">Loading…</p>}
      {!loading && error && <p className="text-danger">{error}</p>}

      {!loading && !error && featured && (
        <article className="relative grid gap-6 border border-border bg-panel md:grid-cols-[1fr_1.2fr]">
          <span aria-hidden className="absolute inset-y-0 left-0 w-[3px] bg-accent" />
          <div className="relative h-56 bg-panel-alt md:h-auto">
            <span className="absolute left-4 top-4 bg-accent px-2 py-1 text-[10px] font-bold uppercase tracking-[0.18em] text-accent-foreground">
              Featured
            </span>
            <div
              aria-hidden
              className="h-full w-full"
              style={{
                backgroundImage:
                  "repeating-linear-gradient(45deg, transparent 0 12px, hsl(var(--border-soft)) 12px 13px)",
              }}
            />
          </div>
          <div className="flex flex-col justify-center p-6">
            <p className="font-mono text-[11px] uppercase tracking-wider text-muted">
              {featured.publishedAt
                ? new Date(featured.publishedAt).toLocaleDateString()
                : "Draft"}
            </p>
            <h2 className="mt-2 font-heading text-3xl font-semibold leading-tight tracking-tight">
              {featured.title}
            </h2>
            {featured.excerpt && (
              <p className="mt-3 text-sm text-fg-soft">{featured.excerpt}</p>
            )}
            <div className="mt-6 flex items-center gap-3">
              <span className="font-mono text-[11px] uppercase tracking-wider text-muted">
                /news/{featured.slug}
              </span>
              <Btn
                size="sm"
                iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                onClick={() => navigate(`/admin/news/${featured.id}`)}
                className="ml-auto"
              >
                Edit
              </Btn>
            </div>
          </div>
        </article>
      )}

      <section className="space-y-4">
        <SectionHead
          number="01"
          title="The index"
          right={
            <input
              type="search"
              placeholder="Search title or slug…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-8 w-56 border border-border bg-background px-3 text-xs focus-visible:border-accent focus-visible:outline-none"
            />
          }
        />
        <FilterPills
          activeValue={tab}
          onChange={(v) => setTab(v as Tab)}
          items={[
            { value: "active", label: "Active" },
            { value: "drafts", label: "Drafts" },
            { value: "members", label: "Members only" },
            { value: "deleted", label: "Deleted" },
          ]}
        />

        {rest.length === 0 ? (
          <p className="text-muted">No news items found.</p>
        ) : (
          <ul className="divide-y divide-border-soft border border-border bg-panel">
            {rest.map((n, i) => (
              <li
                key={n.id}
                className="grid items-center gap-4 px-5 py-4"
                style={{ gridTemplateColumns: "60px 1fr 160px 100px" }}
              >
                <span
                  style={{ fontVariantNumeric: "tabular-nums" }}
                  className="font-mono text-2xl font-bold text-muted"
                >
                  {String(i + 2).padStart(2, "0")}
                </span>
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    {!n.isPublished && <Chip tone="warn" dot>Draft</Chip>}
                    {n.isMembersOnly && <Chip tone="accent">Members</Chip>}
                  </div>
                  <button
                    type="button"
                    onClick={() => navigate(`/admin/news/${n.id}`)}
                    className="mt-1 text-left font-heading text-base font-semibold hover:underline"
                  >
                    {n.title}
                  </button>
                  {n.excerpt && (
                    <p className="mt-1 line-clamp-1 text-xs text-fg-soft">
                      {n.excerpt}
                    </p>
                  )}
                </div>
                <div className="text-xs text-muted">
                  <p className="truncate font-mono">/news/{n.slug}</p>
                  <p className="font-mono">
                    {new Date(n.publishedAt ?? n.modifiedAt).toLocaleDateString()}
                  </p>
                </div>
                <div className="flex justify-end">
                  <Btn
                    size="sm"
                    iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                    onClick={() => navigate(`/admin/news/${n.id}`)}
                  >
                    Edit
                  </Btn>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
