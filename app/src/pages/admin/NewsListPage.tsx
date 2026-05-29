import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowRight, Plus, Trash2 } from "lucide-react";
import { newsApi } from "@/lib/api/news";
import type { NewsListItem, PagedResult } from "@/types/api";
import {
  Btn,
  Chip,
  FilterPills,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";
import { ConfirmDialog } from "@/components/shared/admin/ConfirmDialog";
import { useToast } from "@/components/shared/admin/Toast";

type Tab = "active" | "drafts" | "members" | "trash";

export function NewsListPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [tab, setTab] = useState<Tab>("active");
  const [search, setSearch] = useState("");
  const [data, setData] = useState<PagedResult<NewsListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [emptyTrashOpen, setEmptyTrashOpen] = useState(false);
  const [emptying, setEmptying] = useState(false);

  const reload = useCallback(() => {
    setLoading(true);
    newsApi.list({
      search: search || undefined,
      includeDeleted: tab === "trash",
      pageSize: 50,
    })
      .then((d) => { setData(d); setError(null); })
      .catch(() => setError("Could not load news."))
      .finally(() => setLoading(false));
  }, [search, tab]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    newsApi.list({
      search: search || undefined,
      includeDeleted: tab === "trash",
      pageSize: 50,
    })
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load news."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [tab, search]);

  const handleRestore = async (item: NewsListItem) => {
    try {
      await newsApi.restore(item.id);
      toast("success", `Restored "${item.title}".`);
      reload();
    } catch {
      toast("error", "Could not restore news item.");
    }
  };

  const handleHardDelete = async (item: NewsListItem) => {
    try {
      await newsApi.hardDelete(item.id);
      toast("success", `Permanently deleted "${item.title}".`);
      setData((prev) => prev
        ? { ...prev, items: prev.items.filter((x) => x.id !== item.id), totalCount: Math.max(0, prev.totalCount - 1) }
        : prev);
    } catch {
      toast("error", "Could not permanently delete news item.");
    }
  };

  // Loops in batches of 50 (the admin list cap) so emptying still works
  // when more than a page of items sit in the trash. Breaks out of the
  // loop if a batch makes no progress so a persistent failure can't spin.
  const performEmptyTrash = async () => {
    setEmptyTrashOpen(false);
    setEmptying(true);
    let deleted = 0;
    let failed = 0;
    try {
      while (true) {
        const batch = await newsApi.list({ includeDeleted: true, pageSize: 50 });
        if (batch.items.length === 0) break;
        const results = await Promise.allSettled(
          batch.items.map((i) => newsApi.hardDelete(i.id)),
        );
        const successesThisBatch = results.filter((r) => r.status === "fulfilled").length;
        deleted += successesThisBatch;
        failed += results.length - successesThisBatch;
        if (successesThisBatch === 0) break;
      }
      if (failed > 0) {
        toast(
          "warning",
          `Deleted ${deleted}. ${failed} item${failed === 1 ? "" : "s"} failed.`,
        );
      } else if (deleted > 0) {
        toast(
          "success",
          `Trash emptied — ${deleted} item${deleted === 1 ? "" : "s"} permanently deleted.`,
        );
      }
    } finally {
      setEmptying(false);
      reload();
    }
  };

  const filtered = useMemo(() => {
    if (!data) return [];
    if (tab === "drafts") return data.items.filter((n) => !n.isPublished);
    if (tab === "members") return data.items.filter((n) => n.isMembersOnly);
    return data.items;
  }, [data, tab]);

  // First published item is treated as the featured item per §5.4.
  // The Trash tab is a recovery view — never surface a featured card there.
  const featured = tab === "trash" ? undefined : filtered.find((n) => n.isPublished);
  const rest = featured ? filtered.filter((n) => n.id !== featured.id) : filtered;
  const trashCount = tab === "trash" ? (data?.totalCount ?? 0) : 0;

  return (
    <div className="space-y-8">
      <ConfirmDialog
        open={emptyTrashOpen}
        variant="danger"
        title="Empty Trash?"
        message={
          trashCount > 0
            ? `${trashCount} item${trashCount === 1 ? "" : "s"} will be permanently deleted from the database. This cannot be undone.`
            : "Trash is already empty."
        }
        confirmLabel="Empty Trash"
        onConfirm={performEmptyTrash}
        onCancel={() => setEmptyTrashOpen(false)}
      />

      <PageHeader
        eyebrow={data ? `${data.items.length} posts` : "Loading…"}
        title="News"
        kicker="church news, announcements, and updates"
        actions={
          <Btn
            variant="accent"
            size="lg"
            iconLeft={<Plus className="h-4 w-4" />}
            onClick={() => navigate("/admin/news/new")}
          >
            Add News
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
        <div className="flex items-center justify-between gap-3">
          <FilterPills
            activeValue={tab}
            onChange={(v) => setTab(v as Tab)}
            items={[
              { value: "active", label: "Active" },
              { value: "drafts", label: "Drafts" },
              { value: "members", label: "Members only" },
              { value: "trash", label: "Trash" },
            ]}
          />
          {tab === "trash" && trashCount > 0 && (
            <Btn
              size="sm"
              variant="danger"
              iconLeft={<Trash2 className="h-3.5 w-3.5" />}
              disabled={emptying}
              onClick={() => setEmptyTrashOpen(true)}
            >
              {emptying ? "Emptying…" : "Empty Trash"}
            </Btn>
          )}
        </div>

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
                <div className="flex justify-end gap-2">
                  {tab === "trash" ? (
                    <>
                      <Btn size="sm" onClick={() => handleRestore(n)}>
                        Restore
                      </Btn>
                      <Btn size="sm" variant="danger" onClick={() => handleHardDelete(n)}>
                        Delete
                      </Btn>
                    </>
                  ) : (
                    <Btn
                      size="sm"
                      iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                      onClick={() => navigate(`/admin/news/${n.id}`)}
                    >
                      Edit
                    </Btn>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
