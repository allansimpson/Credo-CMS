import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowRight, Users } from "lucide-react";
import { eventsApi, type EventListItem } from "@/lib/api/events";
import type { PagedResult } from "@/types/api";
import {
  Btn,
  Chip,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";

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

  const items = data?.items ?? [];
  const calendar = useMemo(() => buildCalendar(items), [items]);

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow={data ? `${items.length} events` : "Loading…"}
        title="Events"
        kicker="services, classes, gatherings"
        actions={
          <>
            <Btn onClick={() => navigate("/admin/events/calendar")}>Calendar view</Btn>
            <Btn variant="accent" size="lg" onClick={() => navigate("/admin/events/new")}>
              New event
            </Btn>
          </>
        }
      />

      <div className="grid gap-8 lg:grid-cols-[1fr_320px]">
        <section className="space-y-4">
          <SectionHead
            number="01"
            title="Up next"
            right={
              <>
                <input
                  type="search"
                  placeholder="Search title or slug…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="h-8 w-56 border border-border bg-background px-3 text-xs focus-visible:border-accent focus-visible:outline-none"
                />
                <label className="flex items-center gap-2 text-xs text-fg-soft">
                  <input
                    type="checkbox"
                    checked={showRecurringOnly}
                    onChange={(e) => setShowRecurringOnly(e.target.checked)}
                    className="accent-accent"
                  />
                  Recurring only
                </label>
              </>
            }
          />

          {loading && <p className="text-muted">Loading…</p>}
          {!loading && items.length === 0 && (
            <p className="text-muted">No events found.</p>
          )}

          {!loading && items.length > 0 && (
            <ul className="divide-y divide-border-soft border border-border bg-panel">
              {items.map((e) => {
                const featured = !e.hasRecurrence; // visual stand-in for now
                return (
                  <li
                    key={e.id}
                    className="relative grid items-center gap-4 px-5 py-4"
                    style={{ gridTemplateColumns: "88px 1fr 160px 120px" }}
                  >
                    {featured && (
                      <span aria-hidden className="absolute inset-y-0 left-0 w-[3px] bg-accent" />
                    )}
                    {/* Date block */}
                    <div className="border-r border-border-soft pr-4 text-center">
                      <p className="text-[11px] font-semibold uppercase tracking-wider text-muted">
                        {new Date(e.startsAt).toLocaleDateString(undefined, { weekday: "short" })}
                      </p>
                      <p
                        style={{ fontVariantNumeric: "tabular-nums" }}
                        className="font-heading text-2xl font-bold leading-none"
                      >
                        {new Date(e.startsAt).getDate()}
                      </p>
                      <p className="text-[11px] uppercase tracking-wider text-muted">
                        {new Date(e.startsAt).toLocaleDateString(undefined, { month: "short" })}
                      </p>
                    </div>
                    {/* Title block */}
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        {e.visibility === 1 && <Chip tone="accent">Members</Chip>}
                        {e.visibility === 0 && <Chip tone="success" dot>Public</Chip>}
                        {e.visibility === null && <Chip tone="warn" dot>No visibility</Chip>}
                        {!e.isPublished && <Chip tone="warn" dot>Draft</Chip>}
                        {e.hasRecurrence && <Chip tone="muted">Recurring</Chip>}
                      </div>
                      <button
                        type="button"
                        onClick={() => navigate(`/admin/events/${e.id}`)}
                        className="mt-1 truncate text-left font-heading text-base font-semibold hover:underline"
                      >
                        {e.title}
                      </button>
                      <p className="mt-1 truncate text-xs text-fg-soft">
                        {new Date(e.startsAt).toLocaleString()}
                        {e.location && ` · ${e.location}`}
                      </p>
                    </div>
                    {/* Capacity (placeholder until backend exposes counts) */}
                    <div className="text-xs">
                      <p className="font-mono text-muted">/events/{e.slug}</p>
                      {e.registrationMode > 0 && (
                        <Chip tone="accent" className="mt-1">Registration</Chip>
                      )}
                    </div>
                    {/* Actions */}
                    <div className="flex items-center justify-end gap-1.5">
                      {e.registrationMode > 0 && (
                        <Btn
                          size="sm"
                          variant="ghost"
                          iconLeft={<Users className="h-3.5 w-3.5" />}
                          onClick={() => navigate(`/admin/events/${e.id}/registrations`)}
                        >
                          Roster
                        </Btn>
                      )}
                      <Btn
                        size="sm"
                        iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                        onClick={() => navigate(`/admin/events/${e.id}`)}
                      >
                        Edit
                      </Btn>
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </section>

        <aside className="space-y-6">
          <section className="border border-border bg-panel p-5">
            <header className="flex items-baseline justify-between">
              <h2 className="font-heading text-base font-semibold">{calendar.monthLabel}</h2>
              <Link
                to="/admin/events/calendar"
                className="text-[11px] uppercase tracking-wider text-accent hover:underline"
              >
                Open
              </Link>
            </header>
            <div className="mt-4 grid grid-cols-7 gap-px text-center text-[10px] uppercase tracking-wider text-muted">
              {["S","M","T","W","T","F","S"].map((d, i) => (
                <span key={i}>{d}</span>
              ))}
            </div>
            <div className="mt-1 grid grid-cols-7 gap-px text-center font-mono text-xs">
              {calendar.cells.map((c, i) => (
                <span
                  key={i}
                  className={
                    "h-7 leading-7 " +
                    (c.isToday
                      ? "bg-accent font-bold text-accent-foreground"
                      : c.hasEvent
                      ? "bg-accent/15 text-foreground"
                      : c.inMonth
                      ? "text-foreground"
                      : "text-muted/40")
                  }
                >
                  {c.day || ""}
                </span>
              ))}
            </div>
          </section>

          <section className="border border-border bg-panel p-5">
            <h2 className="font-heading text-base font-semibold">Categories</h2>
            <ul className="mt-3 space-y-1.5 text-sm">
              {[
                { label: "Worship", count: items.filter((e) => /service|worship|sunday/i.test(e.title)).length },
                { label: "Classes", count: items.filter((e) => /class|study/i.test(e.title)).length },
                { label: "Outreach", count: items.filter((e) => /mission|outreach/i.test(e.title)).length },
                { label: "Other", count: items.length },
              ].map((c) => (
                <li key={c.label} className="flex items-center justify-between">
                  <span>{c.label}</span>
                  <span
                    style={{ fontVariantNumeric: "tabular-nums" }}
                    className="font-mono text-xs text-muted"
                  >
                    {String(c.count).padStart(2, "0")}
                  </span>
                </li>
              ))}
            </ul>
          </section>

          {/* Heads-up dark inset */}
          <section className="relative bg-sidebar p-5 text-background">
            <span aria-hidden className="absolute inset-y-0 left-0 w-[3px] bg-accent" />
            <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-accent">
              Heads up
            </p>
            <p className="mt-2 text-sm leading-snug text-background/90">
              Volunteer slots are still open for this Sunday's setup crew.
            </p>
            <Btn variant="accent" size="sm" className="mt-3">
              See volunteers
            </Btn>
          </section>
        </aside>
      </div>
    </div>
  );
}

interface CalendarCell {
  day: number | null;
  inMonth: boolean;
  isToday: boolean;
  hasEvent: boolean;
}

function buildCalendar(items: EventListItem[]): {
  monthLabel: string;
  cells: CalendarCell[];
} {
  const today = new Date();
  const year = today.getFullYear();
  const month = today.getMonth();
  const firstOfMonth = new Date(year, month, 1);
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const startWeekday = firstOfMonth.getDay();

  const eventDays = new Set<number>();
  for (const e of items) {
    const d = new Date(e.startsAt);
    if (d.getFullYear() === year && d.getMonth() === month) {
      eventDays.add(d.getDate());
    }
  }

  const cells: CalendarCell[] = [];
  for (let i = 0; i < startWeekday; i++) {
    cells.push({ day: null, inMonth: false, isToday: false, hasEvent: false });
  }
  for (let day = 1; day <= daysInMonth; day++) {
    cells.push({
      day,
      inMonth: true,
      isToday: day === today.getDate(),
      hasEvent: eventDays.has(day),
    });
  }
  while (cells.length % 7 !== 0) {
    cells.push({ day: null, inMonth: false, isToday: false, hasEvent: false });
  }

  return {
    monthLabel: today.toLocaleDateString(undefined, { month: "long", year: "numeric" }),
    cells,
  };
}
