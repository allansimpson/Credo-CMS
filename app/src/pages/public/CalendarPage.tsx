import { useCallback, useState } from "react";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import listPlugin from "@fullcalendar/list";
import { calendarApi, type CalendarItem } from "@/lib/api/calendar";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";

interface FcEvent {
  id: string;
  title: string;
  start: string;
  end?: string;
  allDay: boolean;
  url: string;
  classNames?: string[];
  extendedProps: { entityType: string; location: string | null; membersOnly: boolean };
}

export function CalendarPage() {
  const { settings } = useSiteSettings();
  const [loading, setLoading] = useState(false);

  const fetchEvents = useCallback(async (info: { start: Date; end: Date }) => {
    setLoading(true);
    try {
      const items = await calendarApi.list(info.start, info.end);
      return items.map(toFcEvent);
    } finally {
      setLoading(false);
    }
  }, []);

  return (
    <article className="mx-auto max-w-6xl px-4 py-8">
      <SeoTags
        title={`Calendar · ${settings?.churchName ?? ""}`}
        description="Upcoming events and dated news on a single calendar."
      />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-3xl font-bold sm:text-4xl">Calendar</h1>
        <div className="flex items-center gap-3">
          {loading && <span className="text-xs text-muted">Loading…</span>}
          <a href="/calendar/feed.ics"
            className="text-sm text-primary hover:underline">Subscribe (iCal) ↗</a>
        </div>
      </div>

      <div className="mt-6 fc-credo">
        <FullCalendar
          plugins={[dayGridPlugin, listPlugin]}
          initialView="dayGridMonth"
          headerToolbar={{
            left: "prev,next today",
            center: "title",
            right: "dayGridMonth,listWeek",
          }}
          height="auto"
          events={fetchEvents}
          eventDisplay="block"
          dayMaxEvents={4}
          firstDay={0}
        />
      </div>

      <style>{`
        .fc-credo .fc { font-family: inherit; }
        .fc-credo .fc-event { cursor: pointer; }
        .fc-credo .fc-event.credo-news { background: hsl(var(--muted)); color: hsl(var(--muted)); border: 0; }
        .fc-credo .fc-event.credo-members { box-shadow: inset 0 0 0 2px hsl(var(--accent, 47 95% 50%)); }
      `}</style>
    </article>
  );
}

function toFcEvent(item: CalendarItem): FcEvent {
  const classNames: string[] = [];
  if (item.entityType === "News") classNames.push("credo-news");
  if (item.membersOnly) classNames.push("credo-members");
  return {
    id: `${item.entityType}-${item.entityId}-${item.start}`,
    title: item.title,
    start: item.start,
    end: item.end ?? undefined,
    allDay: item.allDay,
    url: item.url,
    classNames,
    extendedProps: {
      entityType: item.entityType,
      location: item.location,
      membersOnly: item.membersOnly,
    },
  };
}
