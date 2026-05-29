import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { eventsApi, type PublicEventListItem } from "@/lib/api/events";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { BigNum, Eyebrow, Headline, ImageSlot } from "@/components/public";
import { Calendar, Rss, ArrowRight, ArrowDown } from "lucide-react";
import type { PagedResult } from "@/types/api";

export function PublicEventsListPage() {
  const { settings } = useSiteSettings();
  const [data, setData] = useState<PagedResult<PublicEventListItem> | null>(null);
  const [page, setPage] = useState(1);
  const [accumulated, setAccumulated] = useState<PublicEventListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [allCategories, setAllCategories] = useState<string[]>([]);

  // Load full unfiltered list once to harvest the universe of categories
  // present on published events. Cheap because page-size 50 covers a year's
  // worth of events for most parishes.
  useEffect(() => {
    let cancelled = false;
    eventsApi.listPublic(1, 50).then((d) => {
      if (cancelled) return;
      const cats = Array.from(new Set(
        d.items.map((e) => e.category).filter((c): c is string => !!c)
      )).sort();
      setAllCategories(cats);
    }).catch(() => { /* leave empty */ });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    eventsApi.listPublic(page, 20, selectedCategory ?? undefined)
      .then((d) => {
        if (cancelled) return;
        setData(d);
        setAccumulated((prev) => page === 1 ? d.items : [...prev, ...d.items]);
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [page, selectedCategory]);

  // Reset pagination when category filter changes
  useEffect(() => { setPage(1); setAccumulated([]); }, [selectedCategory]);

  const featured = accumulated[0] ?? null;
  const rest = accumulated.slice(1);

  const monthGroups = useMemo(() => {
    const groups = new Map<string, PublicEventListItem[]>();
    for (const e of rest) {
      const d = new Date(e.nextOccurrenceAt);
      const key = `${d.getFullYear()}-${String(d.getMonth()).padStart(2, "0")}`;
      const label = d.toLocaleDateString("en-US", { month: "long", year: "numeric" });
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key)!.push(e);
    }
    return Array.from(groups.entries()).map(([key, items]) => {
      const d = new Date(items[0].nextOccurrenceAt);
      return { key, label: d.toLocaleDateString("en-US", { month: "long", year: "numeric" }), items };
    });
  }, [rest]);

  return (
    <div>
      <SeoTags
        title={`Events · ${settings?.churchName ?? ""}`}
        description="Upcoming events and gatherings."
      />

      {/* ── Header ────────────────────────────────────────────── */}
      <header className="mx-auto max-w-7xl px-6 py-10 md:py-14">
        <Eyebrow accent>Events</Eyebrow>
        <Headline as="h1" size="display" className="mt-3">
          What&rsquo;s coming up.
        </Headline>

        {/* ── Filter chips + actions ──────────────────────────── */}
        <div className="mt-8 flex flex-wrap items-center justify-between gap-4 border-b border-border-soft pb-4">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">Show</span>
            <button
              type="button"
              onClick={() => setSelectedCategory(null)}
              aria-pressed={selectedCategory === null ? "true" : "false"}
              className={`inline-flex items-center border px-3 py-1 text-xs font-medium transition ${
                selectedCategory === null
                  ? "border-primary bg-primary text-primary-foreground"
                  : "border-border-soft hover:bg-panel-alt"
              }`}
            >
              All
            </button>
            {allCategories.map((c) => (
              <button
                key={c}
                type="button"
                onClick={() => setSelectedCategory(c)}
                aria-pressed={selectedCategory === c ? "true" : "false"}
                className={`inline-flex items-center border px-3 py-1 text-xs font-medium transition ${
                  selectedCategory === c
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-border-soft hover:bg-panel-alt"
                }`}
              >
                {c}
              </button>
            ))}
          </div>
          <div className="flex flex-wrap gap-2">
            <Link
              to="/calendar"
              className="inline-flex items-center gap-1.5 border border-border-soft px-3 py-1.5 text-xs font-medium hover:bg-panel-alt"
            >
              <Calendar size={14} strokeWidth={1.5} /> Calendar view
            </Link>
            <a
              href="/calendar/feed.ics"
              className="inline-flex items-center gap-1.5 border border-border-soft px-3 py-1.5 text-xs font-medium hover:bg-panel-alt"
            >
              <Rss size={14} strokeWidth={1.5} /> Subscribe
            </a>
          </div>
        </div>
      </header>

      {loading && accumulated.length === 0 && (
        <p className="mx-auto max-w-7xl px-6 py-8 text-muted">Loading…</p>
      )}

      {/* ── Featured event ────────────────────────────────────── */}
      {featured && (
        <section className="mx-auto max-w-7xl px-6 py-10">
          <div className="grid gap-8 md:grid-cols-[1fr_1fr]">
            <Link to={`/events/${featured.slug}`} className="block">
              {featured.heroImageUrl ? (
                <img src={featured.heroImageUrl} alt={featured.heroImageAlt ?? ""} className="aspect-[4/3] w-full object-cover" />
              ) : (
                <ImageSlot ratio="4:3" label={`${featured.title}`} alt="" />
              )}
            </Link>
            <div className="flex flex-col justify-center">
              <span className="inline-flex w-fit items-center gap-1.5 bg-accent px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.12em] text-accent-foreground">
                Featured
              </span>
              <Link to={`/events/${featured.slug}`}>
                <Headline as="h2" size="h2" className="mt-3">{featured.title}</Headline>
              </Link>
              <div className="mt-3 flex gap-6 text-sm">
                <div>
                  <p className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">When</p>
                  <p className="mt-0.5 font-semibold">{formatEventWhen(featured)}</p>
                  <p className="text-xs text-muted">{formatEventTime(featured)}</p>
                </div>
                {featured.location && (
                  <div>
                    <p className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">Where</p>
                    <p className="mt-0.5 font-semibold">{featured.location}</p>
                  </div>
                )}
              </div>
              <div className="mt-6 flex flex-wrap gap-3">
                <Link to={`/events/${featured.slug}`}
                  className="inline-flex items-center gap-2 bg-primary px-5 py-2.5 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
                  RSVP
                  <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4" />
                </Link>
                <span className="inline-flex items-center gap-1.5 border border-border-soft px-4 py-2 text-sm font-medium text-fg-soft">
                  <Calendar size={14} strokeWidth={1.5} /> Add to calendar
                </span>
              </div>
            </div>
          </div>
        </section>
      )}

      {/* ── Month-grouped event rows ──────────────────────────── */}
      <div className="mx-auto max-w-7xl px-6 pb-12">
        {monthGroups.map((group) => (
          <section key={group.key} className="mt-10">
            <div className="flex items-baseline justify-between border-b-2 border-foreground pb-2">
              <h2 className="text-2xl font-semibold md:text-3xl">{group.label}</h2>
              <span className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">
                {group.items.length} {group.items.length === 1 ? "event" : "events"}
              </span>
            </div>
            <div className="divide-y divide-border-soft">
              {group.items.map((e) => (
                <EventRow key={e.id} event={e} />
              ))}
            </div>
          </section>
        ))}

        {data && data.totalPages > page && (
          <div className="mt-10 flex justify-center">
            <button type="button" onClick={() => setPage((p) => p + 1)}
              className="inline-flex items-center gap-2 border border-border-soft px-5 py-2.5 text-sm font-medium hover:bg-panel-alt">
              Load more events
              <ArrowDown aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

function EventRow({ event }: { event: PublicEventListItem }) {
  const d = new Date(event.nextOccurrenceAt);
  const month = d.toLocaleDateString("en-US", { month: "short" }).toUpperCase();
  const day = d.getDate();
  const dayOfWeek = d.toLocaleDateString("en-US", { weekday: "short" });

  return (
    <div className="grid items-center gap-x-4 py-5 md:grid-cols-[4rem_1fr_auto_auto]">
      {/* Date column */}
      <div className="hidden text-center md:block">
        <p className="text-[10px] font-semibold uppercase tracking-wider text-accent">{month}</p>
        <BigNum size="lg" tone="default">{day}</BigNum>
        <p className="text-[10px] text-muted">{dayOfWeek}</p>
      </div>

      {/* Title + description */}
      <div className="min-w-0">
        <h3 className="font-semibold">{event.title}</h3>
        {event.location && (
          <p className="mt-0.5 text-xs text-muted">{event.location}</p>
        )}
      </div>

      {/* Time + location */}
      <div className="text-right text-xs text-muted md:text-left">
        <p className="font-mono">{formatEventTime(event)}</p>
        {event.location && <p className="font-mono">{event.location}</p>}
      </div>

      {/* Details button */}
      <div className="hidden md:block">
        <Link to={`/events/${event.slug}`}
          className="inline-flex items-center gap-1.5 border border-border-soft px-3 py-1.5 text-xs font-medium hover:bg-panel-alt">
          Details
          <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-3.5 w-3.5 translate-y-px" />
        </Link>
      </div>
    </div>
  );
}

function formatEventWhen(e: PublicEventListItem): string {
  const d = new Date(e.nextOccurrenceAt);
  const dayOfWeek = d.toLocaleDateString("en-US", { weekday: "short" });
  return `${dayOfWeek} · ${d.toLocaleDateString("en-US", { month: "short", day: "numeric" })}`;
}

function formatEventTime(e: PublicEventListItem): string {
  const start = new Date(e.nextOccurrenceAt);
  const startStr = start.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" });
  if (!e.endsAt) return startStr;
  const end = new Date(e.endsAt);
  const endStr = end.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" });
  return `${startStr} – ${endStr}`;
}
