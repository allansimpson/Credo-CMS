import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { sermonSeriesApi, type SermonSeriesListItem } from "@/lib/api/sermonSeries";
import type { PagedResult } from "@/types/api";

type Tab = "active" | "deleted";

export function SermonSeriesListPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>("active");
  const [data, setData] = useState<PagedResult<SermonSeriesListItem> | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    sermonSeriesApi.list({ includeDeleted: tab === "deleted", pageSize: 50 })
      .then((d) => { if (!cancelled) setData(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [tab]);

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Sermon Series</h1>
        <Link
          to="/admin/sermon-series/new"
          className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
        >
          New series
        </Link>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-3 border-b">
        {(["active", "deleted"] as Tab[]).map((t) => (
          <button key={t} type="button" onClick={() => setTab(t)}
            className={
              "h-10 px-4 text-sm transition-colors " +
              (tab === t
                ? "border-b-2 border-accent text-foreground font-semibold"
                : "text-muted-foreground hover:text-foreground")
            }>
            {t === "active" ? "Active" : "Deleted"}
          </button>
        ))}
      </div>

      <div className="mt-4">
        {loading && <p className="text-muted-foreground">Loading…</p>}
        {!loading && data && data.items.length === 0 && (
          <p className="text-muted-foreground">No sermon series.</p>
        )}
        {!loading && data && data.items.length > 0 && (
          <ul className="divide-y border bg-card">
            {data.items.map((s) => (
              <li key={s.id} className="flex flex-col gap-2 p-4 sm:flex-row sm:items-center sm:gap-4">
                {s.bannerImageUrl ? (
                  <picture>
                    {s.bannerImageWebpUrl && <source srcSet={s.bannerImageWebpUrl} type="image/webp" />}
                    <img src={s.bannerImageUrl} alt={s.bannerImageAlt ?? ""}
                      className="h-16 w-28 object-cover" />
                  </picture>
                ) : (
                  <div className="h-16 w-28 bg-muted" />
                )}
                <div className="flex-1">
                  <button type="button"
                    onClick={() => navigate(`/admin/sermon-series/${s.id}`)}
                    className="text-left font-semibold hover:underline">
                    {s.title}
                  </button>
                  <p className="text-xs text-muted-foreground">/sermons/series/{s.slug}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {s.startDate}{s.endDate ? ` – ${s.endDate}` : " – ongoing"}
                  </p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
