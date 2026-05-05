import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { eventsApi, type EventListItem } from "@/lib/api/events";
import type { PagedResult } from "@/types/api";

export function EventsListPage() {
  const navigate = useNavigate();
  const [data, setData] = useState<PagedResult<EventListItem> | null>(null);
  const [search, setSearch] = useState("");
  const [showRecurringOnly, setShowRecurringOnly] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    eventsApi.list({
      search: search || undefined,
      hasRecurrence: showRecurringOnly ? true : undefined,
      pageSize: 50,
    }).then((d) => { if (!cancelled) setData(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [search, showRecurringOnly]);

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Events</h1>
        <div className="flex flex-wrap items-center gap-2">
          <Link to="/admin/events/calendar"
            className="inline-flex h-10 items-center justify-center border bg-card px-4 text-sm hover:bg-muted">
            Calendar view
          </Link>
          <Link to="/admin/events/new"
            className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
            New event
          </Link>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-3 border-b">
        <input
          type="search"
          placeholder="Search title or slug…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-10 w-full max-w-xs border bg-background px-3 text-sm"
        />
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={showRecurringOnly}
            onChange={(e) => setShowRecurringOnly(e.target.checked)} />
          Recurring only
        </label>
      </div>

      <div className="mt-4">
        {loading && <p className="text-muted-foreground">Loading…</p>}
        {!loading && data && data.items.length === 0 && (
          <p className="text-muted-foreground">No events found.</p>
        )}
        {!loading && data && data.items.length > 0 && (
          <ul className="divide-y border bg-card">
            {data.items.map((e) => (
              <li key={e.id} className="flex flex-col gap-2 p-4 sm:flex-row sm:items-center sm:gap-4">
                <div className="flex-1">
                  <button type="button" onClick={() => navigate(`/admin/events/${e.id}`)}
                    className="text-left font-semibold hover:underline">
                    {e.title}
                  </button>
                  <p className="text-xs text-muted-foreground">/events/{e.slug}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {new Date(e.startsAt).toLocaleString()}
                    {e.location && ` · ${e.location}`}
                    {e.hasRecurrence && " · Recurring"}
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-2 text-xs">
                  {e.visibility === null && <Badge color="amber">No visibility</Badge>}
                  {e.visibility === 0 && <Badge color="emerald">Public</Badge>}
                  {e.visibility === 1 && <Badge color="indigo">Members only</Badge>}
                  {e.registrationMode > 0 && <Badge color="muted">Registration</Badge>}
                  {!e.isPublished && <Badge color="amber">Draft</Badge>}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function Badge({ color, children }: { color: "emerald" | "amber" | "indigo" | "muted"; children: React.ReactNode }) {
  const palette: Record<string, string> = {
    emerald: "bg-emerald-100 text-emerald-800 border-emerald-200",
    amber: "bg-amber-100 text-amber-800 border-amber-200",
    indigo: "bg-indigo-100 text-indigo-800 border-indigo-200",
    muted: "bg-muted text-muted-foreground border-border",
  };
  return <span className={`inline-flex items-center border px-2 py-0.5 ${palette[color]}`}>{children}</span>;
}
