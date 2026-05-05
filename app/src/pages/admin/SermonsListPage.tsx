import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { sermonsApi, type SermonListItem } from "@/lib/api/sermons";
import type { PagedResult } from "@/types/api";

export function SermonsListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState("");
  const [data, setData] = useState<PagedResult<SermonListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [importing, setImporting] = useState(false);
  const [importInput, setImportInput] = useState("");
  const [importError, setImportError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    sermonsApi.list({ search: search || undefined, includeDeleted: false, pageSize: 50 })
      .then((d) => { if (!cancelled) setData(d); })
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

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Sermons</h1>
        <button type="button" onClick={handleSync}
          className="inline-flex h-10 items-center justify-center border bg-card px-4 text-sm hover:bg-muted">
          Run YouTube sync
        </button>
      </div>

      <form onSubmit={handleImport} className="mt-4 space-y-2 border bg-card p-4">
        <h2 className="text-sm font-semibold">Import from YouTube</h2>
        <div className="flex flex-wrap gap-2">
          <input
            value={importInput}
            onChange={(e) => setImportInput(e.target.value)}
            placeholder="https://youtube.com/watch?v=… or bare video ID"
            className="h-10 flex-1 border bg-background px-3 text-sm"
          />
          <button type="submit" disabled={importing}
            className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
            {importing ? "Importing…" : "Import"}
          </button>
        </div>
        {importError && <p role="alert" className="text-xs text-destructive">{importError}</p>}
      </form>

      <div className="mt-4 flex flex-wrap items-center gap-3 border-b">
        <input
          type="search"
          placeholder="Search title or slug…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-10 w-full max-w-xs border bg-background px-3 text-sm"
        />
      </div>

      <div className="mt-4">
        {loading && <p className="text-muted-foreground">Loading…</p>}
        {!loading && data && data.items.length === 0 && (
          <p className="text-muted-foreground">No sermons. Try importing one above.</p>
        )}
        {!loading && data && data.items.length > 0 && (
          <ul className="divide-y border bg-card">
            {data.items.map((s) => (
              <li key={s.id} className="flex flex-col gap-2 p-4 sm:flex-row sm:items-center sm:gap-4">
                {s.thumbnailBlobUrl ? (
                  <picture>
                    {s.thumbnailWebpBlobUrl && <source srcSet={s.thumbnailWebpBlobUrl} type="image/webp" />}
                    <img src={s.thumbnailBlobUrl} alt=""
                      className="h-16 w-28 object-cover" />
                  </picture>
                ) : (
                  <div className="h-16 w-28 bg-muted" />
                )}
                <div className="flex-1">
                  <button type="button"
                    onClick={() => navigate(`/admin/sermons/${s.id}`)}
                    className="text-left font-semibold hover:underline">
                    {s.title}
                  </button>
                  <p className="text-xs text-muted-foreground">
                    {s.speakerName ?? "no speaker"}
                    {s.sermonSeriesTitle && ` · ${s.sermonSeriesTitle}`}
                    {s.isMembersOnly && " · Members only"}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {new Date(s.publishedAt).toLocaleDateString()}
                    {!s.isPublished && " · Draft"}
                  </p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      <p className="mt-6 text-xs text-muted-foreground">
        New sermons created via <Link to="/admin/sermons/new" className="text-primary hover:underline">manual create</Link> too.
      </p>
    </div>
  );
}
