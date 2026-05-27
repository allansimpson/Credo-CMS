import { useCallback, useState } from "react";
import { Link } from "react-router-dom";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import listPlugin from "@fullcalendar/list";
import { calendarApi } from "@/lib/api/calendar";

export function EventsCalendarOverviewPage() {
  const [loading, setLoading] = useState(false);

  const fetchEvents = useCallback(async (info: { start: Date; end: Date }) => {
    setLoading(true);
    try {
      const items = await calendarApi.list(info.start, info.end);
      return items.filter((i) => i.entityType === "Event").map((i) => ({
        id: `${i.entityType}-${i.entityId}-${i.start}`,
        title: i.title,
        start: i.start,
        end: i.end ?? undefined,
        allDay: i.allDay,
        url: i.url,
      }));
    } finally {
      setLoading(false);
    }
  }, []);

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Events calendar</h1>
        <div className="flex items-center gap-3">
          {loading && <span className="text-xs text-muted">Loading…</span>}
          <Link to="/admin/events" className="text-sm text-primary hover:underline">
            ← Back to events list
          </Link>
        </div>
      </div>
      <p className="mt-1 text-sm text-muted">
        Click an occurrence to open the event editor.
      </p>

      <div className="mt-6">
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
          dayMaxEvents={4}
        />
      </div>
    </div>
  );
}
