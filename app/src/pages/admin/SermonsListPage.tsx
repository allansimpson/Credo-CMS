import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowRight, Play, RotateCw } from "lucide-react";
import { sermonsApi, type SermonListItem } from "@/lib/api/sermons";
import { sermonSeriesApi, type SermonSeriesListItem } from "@/lib/api/sermonSeries";
import type { PagedResult } from "@/types/api";
import {
  Avatar,
  Btn,
  Chip,
  MetaLabel,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";

export function SermonsListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState("");
  const [data, setData] = useState<PagedResult<SermonListItem> | null>(null);
  const [seriesData, setSeriesData] = useState<PagedResult<SermonSeriesListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [importing, setImporting] = useState(false);
  const [importInput, setImportInput] = useState("");
  const [importError, setImportError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    Promise.all([
      sermonsApi.list({ search: search || undefined, includeDeleted: false, pageSize: 50 }),
      sermonSeriesApi.list({ pageSize: 8 }),
    ])
      .then(([s, series]) => {
        if (!cancelled) {
          setData(s);
          setSeriesData(series);
        }
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [search]);

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

  const handleSync = async () => {
    if (!window.confirm("Trigger an immediate YouTube sync?")) return;
    await sermonsApi.triggerSync();
    window.alert("Sync queued. Refresh the list in a minute or two.");
  };

  // Active series = the most recently started one without an end date.
  const activeSeriesId = seriesData?.items.find((s) => !s.endDate)?.id
    ?? seriesData?.items[0]?.id;

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow={data ? `${data.items.length} messages` : "Loading…"}
        title="Sermons"
        kicker="audio + video archive"
        actions={
          <>
            <Btn iconLeft={<RotateCw className="h-3.5 w-3.5" />} onClick={handleSync}>
              Run YouTube sync
            </Btn>
            <Btn variant="accent" size="lg" onClick={() => navigate("/admin/sermons/new")}>
              New sermon
            </Btn>
          </>
        }
      />

      <form
        onSubmit={handleImport}
        className="border border-border bg-panel p-5"
      >
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
                    <span className="font-mono">
                      {new Date(s.startDate).toLocaleDateString()}
                      {s.endDate && ` – ${new Date(s.endDate).toLocaleDateString()}`}
                    </span>
                  </footer>
                </Link>
              );
            })}
          </div>
        </section>
      )}

      <section className="space-y-4">
        <SectionHead
          number="02"
          title="Recent messages"
          right={
            <input
              type="search"
              placeholder="Search title…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-8 w-56 border border-border bg-background px-3 text-xs focus-visible:border-accent focus-visible:outline-none"
            />
          }
        />

        {loading && <p className="text-muted">Loading…</p>}
        {!loading && data && data.items.length === 0 && (
          <p className="text-muted">No sermons. Try importing one above.</p>
        )}

        {!loading && data && data.items.length > 0 && (
          <article className="border border-border bg-panel">
            <header
              className="grid items-center gap-4 border-b border-border bg-panel-alt px-5 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted"
              style={{ gridTemplateColumns: "50px 3fr 1.4fr 1fr 1fr" }}
            >
              <span className="font-mono">#</span>
              <span>Sermon</span>
              <span>Preacher</span>
              <span>Date</span>
              <span className="text-right">Actions</span>
            </header>
            <ul className="divide-y divide-border-soft">
              {data.items.map((s, i) => (
                <li
                  key={s.id}
                  className="grid items-center gap-4 px-5 py-3"
                  style={{ gridTemplateColumns: "50px 3fr 1.4fr 1fr 1fr" }}
                >
                  <span
                    style={{ fontVariantNumeric: "tabular-nums" }}
                    className="font-mono text-xs text-muted"
                  >
                    {String(i + 1).padStart(3, "0")}
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
                  <div className="flex items-center gap-2">
                    <Avatar name={s.speakerName ?? "?"} size="sm" />
                    <span className="text-sm">{s.speakerName ?? "—"}</span>
                  </div>
                  <span
                    style={{ fontVariantNumeric: "tabular-nums" }}
                    className="font-mono text-xs"
                  >
                    {new Date(s.publishedAt).toLocaleDateString()}
                  </span>
                  <div className="flex items-center justify-end gap-1.5">
                    <Btn
                      size="sm"
                      variant="ghost"
                      iconLeft={<Play className="h-3.5 w-3.5" />}
                    >
                      Listen
                    </Btn>
                    <Btn
                      size="sm"
                      iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                      onClick={() => navigate(`/admin/sermons/${s.id}`)}
                    >
                      Edit
                    </Btn>
                  </div>
                </li>
              ))}
            </ul>
          </article>
        )}
      </section>
    </div>
  );
}
