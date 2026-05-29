import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { ArrowRight, Play, RotateCw, RotateCcw, Trash2, X } from "lucide-react";
import { ConfirmDialog } from "@/components/shared/admin/ConfirmDialog";
import { useToast } from "@/components/shared/admin/Toast";
import { sermonsApi, type SermonListItem } from "@/lib/api/sermons";
import { sermonSeriesApi, type SermonSeriesListItem } from "@/lib/api/sermonSeries";
import { getServiceTypeInfo } from "@/components/sermons/serviceTypeLabels";
import type { PagedResult } from "@/types/api";
import {
  Avatar,
  Btn,
  Chip,
  FilterPills,
  MetaLabel,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";
import { StickyTablePager } from "@/components/shared/admin/StickyTablePager";
import { useTableQuery } from "@/hooks/useTableQuery";

// Three-layer sandwich grid template — header + every row share this class
// so columns align even when the body has its own scrollbar. Kept as a
// single string constant so the two render sites stay in lock-step.
const COLUMNS_CLASS = "grid-cols-[50px_110px_1.6fr_140px_1.4fr_80px_1fr]";

/**
 * Format a duration in seconds as h:mm:ss or m:ss. Returns "—" for null.
 *   3725 → "1:02:05"
 *    312 → "5:12"
 *      9 → "0:09"
 */
function formatDuration(seconds: number | null): string {
  if (seconds == null || seconds < 0) return "—";
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = seconds % 60;
  const ss = String(s).padStart(2, "0");
  if (h > 0) return `${h}:${String(m).padStart(2, "0")}:${ss}`;
  return `${m}:${ss}`;
}

type Tab = "active" | "deleted";

export function SermonsListPage() {
  const navigate = useNavigate();
  const { page, pageSize, q, setPage, setPageSize, setQuery } = useTableQuery({
    defaultPageSize: 50,
  });

  // Active vs Deleted tab — persisted in the URL so refresh / back-button
  // restore the view. Switching tabs resets pagination to page 1.
  const [searchParams, setSearchParams] = useSearchParams();
  const tab: Tab = searchParams.get("tab") === "deleted" ? "deleted" : "active";
  const setTab = useCallback((next: Tab) => {
    const params = new URLSearchParams(searchParams);
    if (next === "active") params.delete("tab");
    else params.set("tab", next);
    params.delete("page");
    setSearchParams(params);
  }, [searchParams, setSearchParams]);

  // Local search input drives the URL after a 250ms debounce so each keystroke
  // doesn't create a history entry or refetch.
  const [searchInput, setSearchInput] = useState(q);
  useEffect(() => { setSearchInput(q); }, [q]);
  useEffect(() => {
    if (searchInput === q) return;
    const t = window.setTimeout(() => setQuery(searchInput), 250);
    return () => window.clearTimeout(t);
  }, [searchInput, q, setQuery]);

  const [data, setData] = useState<PagedResult<SermonListItem> | null>(null);
  const [seriesData, setSeriesData] = useState<PagedResult<SermonSeriesListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [importing, setImporting] = useState(false);
  const [importInput, setImportInput] = useState("");
  const [importError, setImportError] = useState<string | null>(null);

  // Inline video player — clicking "Watch" on a row stages the sermon here
  // and the YouTube embed mounts in an overlay. null = closed.
  const [watching, setWatching] = useState<SermonListItem | null>(null);

  // Hard-delete confirmation — only reachable from the Deleted tab. Holds
  // the sermon currently up for permanent removal so the dialog can name it.
  const [hardDeleting, setHardDeleting] = useState<SermonListItem | null>(null);

  // Cheap headcount for the Deleted bucket — drives whether the Active/Deleted
  // tab strip is visible at all. Null while loading; 0 hides the pills.
  const [deletedCount, setDeletedCount] = useState<number | null>(null);

  // Fetch sermons whenever paging or query changes. Series only need to load
  // once — they're chrome, not the paginated list.
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    sermonsApi
      .list({ search: q || undefined, includeDeleted: tab === "deleted", page, pageSize })
      .then((s) => { if (!cancelled) setData(s); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [page, pageSize, q, tab]);

  useEffect(() => {
    let cancelled = false;
    sermonSeriesApi.list({ pageSize: 8 })
      .then((s) => { if (!cancelled) setSeriesData(s); })
      .catch(() => { /* leave null */ });
    return () => { cancelled = true; };
  }, []);

  // Refresh the deleted-bucket headcount on mount and on tab change. Tab
  // change covers "admin soft-deleted in the editor then came back" — a
  // single-row HEAD-like query is cheap.
  useEffect(() => {
    let cancelled = false;
    sermonsApi.list({ includeDeleted: true, page: 1, pageSize: 1 })
      .then((res) => { if (!cancelled) setDeletedCount(res.totalCount); })
      .catch(() => { /* leave null */ });
    return () => { cancelled = true; };
  }, [tab]);

  // Clamp page if out of range — e.g. URL hacked to ?page=999 or pageSize
  // change shrunk totalPages below the current page.
  useEffect(() => {
    if (data && data.totalPages > 0 && page > data.totalPages) {
      setPage(data.totalPages);
    }
  }, [data, page, setPage]);

  const handleImport = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!importInput.trim()) return;
    setImporting(true); setImportError(null);
    try {
      const created = await sermonsApi.import(importInput.trim());
      navigate(`/admin/sermons/${created.id}`);
    } catch (err) {
      const m = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Import failed."];
      setImportError(m.join("; "));
    } finally {
      setImporting(false);
    }
  };

  const { toast } = useToast();
  const [syncConfirmOpen, setSyncConfirmOpen] = useState(false);
  const handleSync = useCallback(async () => {
    setSyncConfirmOpen(false);
    try {
      await sermonsApi.triggerSync();
      toast("success", "YouTube sync queued. Refresh the list in a minute or two.");
    } catch {
      toast("error", "Failed to trigger sync.");
    }
  }, [toast]);

  const handleRestore = useCallback(async (sermonId: string, title: string) => {
    try {
      await sermonsApi.restore(sermonId);
      toast("success", `"${title}" restored.`);
      // Trigger a refetch by bumping a query param the effect doesn't watch
      // — simplest is to swap to the active tab so the admin sees it.
      setDeletedCount((c) => c === null ? null : Math.max(0, c - 1));
      const params = new URLSearchParams(searchParams);
      params.delete("tab");
      params.delete("page");
      setSearchParams(params);
    } catch {
      toast("error", "Failed to restore.");
    }
  }, [toast, searchParams, setSearchParams]);

  const performHardDelete = useCallback(async () => {
    if (!hardDeleting) return;
    const target = hardDeleting;
    setHardDeleting(null);
    try {
      await sermonsApi.hardDelete(target.id);
      toast("success", `"${target.title}" permanently deleted.`);
      // Optimistic-splice: yank the row out of local state so it disappears
      // immediately. A refetch via URL change isn't reliable when we're
      // already on the only set of URL params (no-op setSearchParams = no
      // effect re-run). The totals shift down by one and a soft re-render
      // is enough — full refetch happens naturally on the next nav.
      setData((prev) => prev ? {
        ...prev,
        items: prev.items.filter((item) => item.id !== target.id),
        totalCount: Math.max(0, prev.totalCount - 1),
        totalPages: Math.max(0, Math.ceil(Math.max(0, prev.totalCount - 1) / prev.pageSize)),
      } : prev);
      setDeletedCount((c) => c === null ? null : Math.max(0, c - 1));
    } catch {
      toast("error", "Failed to delete.");
    }
  }, [hardDeleting, toast]);

  // Active series = the most recently started one without an end date.
  const activeSeriesId = seriesData?.items.find((s) => !s.endDate)?.id
    ?? seriesData?.items[0]?.id;

  // Scroll-to-top wiring for the footer's "↑ Top" link. Points at the
  // overflow-scrolling body div, not the window.
  const tableBodyRef = useRef<HTMLDivElement>(null);
  const scrollBodyToTop = useCallback(() => {
    tableBodyRef.current?.scrollTo({ top: 0, behavior: "smooth" });
  }, []);

  const total = data?.totalCount ?? 0;
  const totalPages = data?.totalPages ?? 0;
  const rowNumberStart = (page - 1) * pageSize;
  const hasRows = useMemo(() => (data?.items?.length ?? 0) > 0, [data]);

  // Esc-to-close + body-scroll lock while the Watch modal is open.
  useEffect(() => {
    if (!watching) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setWatching(null);
    };
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = prevOverflow;
    };
  }, [watching]);

  return (
    <>
    <ConfirmDialog
      open={syncConfirmOpen}
      title="YouTube Sync"
      message="Trigger an immediate YouTube sync? New videos will be imported as draft sermons."
      confirmLabel="Run sync"
      onConfirm={handleSync}
      onCancel={() => setSyncConfirmOpen(false)}
    />

    {watching && (
      <WatchModal sermon={watching} onClose={() => setWatching(null)} />
    )}

    <ConfirmDialog
      open={hardDeleting !== null}
      tone="danger"
      title="Permanently delete this sermon?"
      message={
        hardDeleting
          ? `"${hardDeleting.title}" will be removed for good. This cannot be undone — the row, its transcript, and any attachments are wiped from the database. The YouTube video itself stays where it is.`
          : ""
      }
      confirmLabel="Permanently delete"
      onConfirm={performHardDelete}
      onCancel={() => setHardDeleting(null)}
    />

    {/* Page-level frame. Negative margins escape the admin <main>'s padding
        so the sticky footer can reach the viewport edges. h-calc fills the
        space between the admin top bar (h-14 = 3.5rem) and viewport bottom. */}
    <div className="-m-4 flex h-[calc(100vh-3.5rem)] flex-col lg:-m-8">
      {/* Top region — page header + import + series + section head. Scrolls
          internally if it gets too tall on small viewports, but normal usage
          keeps everything visible. */}
      <div className="shrink-0 space-y-8 overflow-y-auto px-4 pt-4 lg:px-8 lg:pt-8">
        <PageHeader
          eyebrow={data ? `${data.totalCount} videos` : "Loading…"}
          title="Sermons"
          kicker="audio + video archive"
          actions={
            <Btn iconLeft={<RotateCw className="h-3.5 w-3.5" />} onClick={() => setSyncConfirmOpen(true)}>
              Run YouTube sync
            </Btn>
          }
        />

        <form onSubmit={handleImport} className="border border-border bg-panel p-5">
          <MetaLabel>Import from YouTube</MetaLabel>
          <div className="mt-3 flex flex-wrap gap-2">
            <input
              value={importInput}
              onChange={(e) => setImportInput(e.target.value)}
              placeholder="https://youtube.com/watch?v=… or bare video ID"
              className="h-10 flex-1 border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
            />
            <Btn type="submit" variant="accent" size="lg" disabled={importing}>
              {importing ? "Importing…" : "Import"}
            </Btn>
          </div>
          {importError && <p role="alert" className="mt-2 text-xs text-danger">{importError}</p>}
        </form>

        {seriesData && seriesData.items.length > 0 && (
          <section className="space-y-4">
            <SectionHead number="01" title="Series" />
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
              {seriesData.items.slice(0, 4).map((s, i) => {
                const active = s.id === activeSeriesId;
                return (
                  <Link
                    key={s.id}
                    to={`/admin/sermon-series/${s.id}`}
                    className={
                      "relative flex flex-col gap-3 border p-5 transition-colors " +
                      (active
                        ? "border-border bg-sidebar text-background"
                        : "border-border bg-panel text-foreground hover:bg-panel-alt")
                    }
                  >
                    {active && (
                      <span aria-hidden className="absolute inset-y-0 left-0 w-[3px] bg-accent" />
                    )}
                    <span
                      className={
                        "text-[11px] font-semibold uppercase tracking-[0.16em] " +
                        (active ? "text-accent" : "text-muted")
                      }
                    >
                      Series {String(i + 1).padStart(2, "0")}
                    </span>
                    <h3 className="font-heading text-lg font-semibold leading-tight">
                      {s.title}
                    </h3>
                    <footer
                      className={
                        "mt-auto border-t pt-3 text-xs " +
                        (active ? "border-background/20 text-background/70" : "border-border-soft text-muted")
                      }
                    >
                      <span className="font-mono tabular-nums">
                        {new Date(s.startDate).toLocaleDateString(undefined, { timeZone: "UTC" })}
                        {s.endDate && ` – ${new Date(s.endDate).toLocaleDateString(undefined, { timeZone: "UTC" })}`}
                      </span>
                    </footer>
                  </Link>
                );
              })}
            </div>
          </section>
        )}

        <SectionHead
          number="02"
          title={
            tab === "deleted"
              ? "Deleted Videos"
              : (deletedCount ?? 0) > 0 ? "Active Videos" : "Videos"
          }
          right={
            <div className="flex flex-wrap items-center gap-3">
              {(deletedCount ?? 0) > 0 && (
                <FilterPills
                  activeValue={tab}
                  onChange={(v) => setTab(v as Tab)}
                  items={[
                    { value: "active", label: "Active" },
                    { value: "deleted", label: "Deleted" },
                  ]}
                />
              )}
              <input
                type="search"
                placeholder="Search date, sermon, type, or preacher…"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="h-8 w-80 border border-border bg-background px-3 text-xs focus-visible:border-accent focus-visible:outline-none"
              />
            </div>
          }
        />
      </div>

      {/* Table sandwich — flex column that fills the remaining viewport. */}
      <article className="mx-4 mt-4 flex flex-1 min-h-0 flex-col border border-border bg-panel lg:mx-8">
        {/* Frozen column header — same grid as rows so columns line up. */}
        <header
          className={`shrink-0 grid ${COLUMNS_CLASS} items-center gap-4 border-b border-border bg-panel-alt px-5 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted [scrollbar-gutter:stable]`}
        >
          <span className="font-mono">#</span>
          <span>Date</span>
          <span>Sermon</span>
          <span>Type</span>
          <span>Preacher</span>
          <span>Length</span>
          <span className="text-right">Actions</span>
        </header>

        {/* Scrolling body */}
        <div
          ref={tableBodyRef}
          className="flex-1 overflow-y-auto [scrollbar-gutter:stable]"
        >
          {loading && !hasRows && (
            <p className="px-5 py-6 text-muted">Loading…</p>
          )}
          {!loading && !hasRows && (
            <div className="px-5 py-8 text-center">
              {q ? (
                <>
                  <p className="text-fg-soft">No sermons match &ldquo;{q}&rdquo;.</p>
                  <button
                    type="button"
                    onClick={() => { setSearchInput(""); setQuery(""); }}
                    className="mt-2 text-sm text-accent hover:underline"
                  >
                    Clear search
                  </button>
                </>
              ) : tab === "deleted" ? (
                <p className="text-muted">No deleted sermons.</p>
              ) : (
                <p className="text-muted">No sermons. Try importing one above.</p>
              )}
            </div>
          )}
          {hasRows && (
            <ul className={loading ? "divide-y divide-border-soft opacity-60 transition-opacity" : "divide-y divide-border-soft transition-opacity"}>
              {data!.items.map((s, i) => (
                <li
                  key={s.id}
                  className={`grid ${COLUMNS_CLASS} items-center gap-4 px-5 py-3 transition-colors odd:bg-panel-alt/40 hover:bg-panel-alt ${tab === "deleted" ? "opacity-70" : ""}`}
                >
                  <span className="font-mono text-xs tabular-nums text-muted">
                    {String(rowNumberStart + i + 1).padStart(3, "0")}
                  </span>
                  <span className="font-mono text-xs tabular-nums">
                    {/* UTC-anchored so the displayed date matches the editor. */}
                    {new Date(s.publishedAt).toLocaleDateString(undefined, { timeZone: "UTC" })}
                  </span>
                  <div className="min-w-0">
                    <button
                      type="button"
                      onClick={() => navigate(`/admin/sermons/${s.id}`)}
                      className="text-left font-heading text-base font-semibold hover:underline"
                    >
                      {s.title}
                    </button>
                    <p className="mt-1 flex items-center gap-2 text-xs">
                      {!s.isPublished && <Chip tone="warn" dot>Draft</Chip>}
                      {s.isMembersOnly && <Chip tone="accent">Members</Chip>}
                      {s.sermonSeriesTitle && (
                        <span className="truncate text-muted">{s.sermonSeriesTitle}</span>
                      )}
                    </p>
                  </div>
                  <span className="text-xs text-fg-soft">
                    {getServiceTypeInfo(s.serviceType).shortLabel}
                  </span>
                  <div className="flex items-center gap-2">
                    <Avatar name={s.speakerName ?? "?"} size="sm" />
                    <span className="text-sm">{s.speakerName ?? "—"}</span>
                  </div>
                  <span className="font-mono text-xs tabular-nums text-fg-soft">
                    {formatDuration(s.durationSeconds)}
                  </span>
                  <div className="flex items-center justify-end gap-1.5">
                    {tab === "deleted" ? (
                      <>
                        <Btn
                          size="sm"
                          iconLeft={<RotateCcw aria-hidden="true" strokeWidth={1.75} className="h-3.5 w-3.5" />}
                          onClick={() => void handleRestore(s.id, s.title)}
                        >
                          Restore
                        </Btn>
                        <Btn
                          size="sm"
                          variant="ghost"
                          iconLeft={<Trash2 aria-hidden="true" strokeWidth={1.75} className="h-3.5 w-3.5" />}
                          onClick={() => setHardDeleting(s)}
                          className="text-danger hover:text-danger"
                        >
                          Delete
                        </Btn>
                      </>
                    ) : (
                      <>
                        <Btn
                          size="sm"
                          variant="ghost"
                          iconLeft={<Play className="h-3.5 w-3.5" />}
                          onClick={() => setWatching(s)}
                          disabled={!s.youTubeVideoId}
                        >
                          Watch
                        </Btn>
                        <Btn
                          size="sm"
                          iconRight={<ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-3.5 w-3.5 translate-y-px" />}
                          onClick={() => navigate(`/admin/sermons/${s.id}`)}
                        >
                          Edit
                        </Btn>
                      </>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        {/* Sticky footer pager — pinned to the bottom of the table container. */}
        <StickyTablePager
          page={page}
          pageSize={pageSize}
          total={total}
          totalPages={totalPages}
          onPageChange={(p) => { setPage(p); scrollBodyToTop(); }}
          onPageSizeChange={(s) => { setPageSize(s); scrollBodyToTop(); }}
          onScrollToTop={scrollBodyToTop}
          query={q || undefined}
          disabled={loading}
        />
      </article>
    </div>
    </>
  );
}

/**
 * Inline YouTube player overlay. Mounts when the admin clicks "Watch" on a
 * row — the embed loads in-place so they don't lose their spot in the table.
 *
 * Layout: centered card with the title + close on a header row, then a 16:9
 * iframe that fills the card. Click the backdrop or press Esc (handled by
 * the parent's effect) to dismiss.
 */
function WatchModal({
  sermon,
  onClose,
}: {
  sermon: SermonListItem;
  onClose: () => void;
}) {
  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label={`Watch ${sermon.title}`}
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
    >
      {/* Backdrop — click to close */}
      <button
        type="button"
        aria-label="Close player"
        onClick={onClose}
        className="absolute inset-0 bg-foreground/60"
      />

      {/* Card */}
      <div className="relative z-10 w-full max-w-4xl border bg-popover text-foreground shadow-2xl">
        <header className="flex items-start gap-3 border-b border-border-soft px-5 py-3">
          <div className="min-w-0 flex-1">
            <p className="font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
              Watching · {sermon.speakerName ?? "—"}
            </p>
            <h2 className="mt-1 truncate font-heading text-base font-semibold">
              {sermon.title}
            </h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="inline-flex h-8 w-8 items-center justify-center border border-border-soft hover:bg-panel-alt"
          >
            <X aria-hidden="true" strokeWidth={1.75} className="h-4 w-4" />
          </button>
        </header>
        <div className="aspect-video w-full bg-black">
          <iframe
            title={sermon.title}
            src={`https://www.youtube.com/embed/${sermon.youTubeVideoId}?autoplay=1`}
            className="h-full w-full"
            allow="encrypted-media; picture-in-picture"
            allowFullScreen
            referrerPolicy="strict-origin-when-cross-origin"
          />
        </div>
      </div>
    </div>
  );
}
