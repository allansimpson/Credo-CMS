import { useEffect, useState } from "react";
import { serviceTimesApi } from "@/lib/api/serviceTimes";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { DayOfWeek, PublicServiceTime } from "@/types/api";

const DAYS: { value: DayOfWeek; label: string }[] = [
  { value: "Sunday", label: "Sunday" }, { value: "Monday", label: "Monday" },
  { value: "Tuesday", label: "Tuesday" }, { value: "Wednesday", label: "Wednesday" },
  { value: "Thursday", label: "Thursday" }, { value: "Friday", label: "Friday" },
  { value: "Saturday", label: "Saturday" },
];

export function PublicServiceTimesPage() {
  const { settings } = useSiteSettings();
  const [items, setItems] = useState<PublicServiceTime[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    serviceTimesApi.listPublic()
      .then((d) => { if (!cancelled) setItems(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  const grouped = DAYS.map((d) => ({
    day: d,
    items: items.filter((s) => s.dayOfWeek === d.value),
  })).filter((g) => g.items.length > 0);

  // JSON-LD schema for service times — Schema.org Schedule.
  const jsonLd = items.length > 0 ? {
    "@context": "https://schema.org",
    "@type": "Organization",
    name: settings?.churchName,
    event: items.map((s) => ({
      "@type": "Event",
      name: s.name,
      eventSchedule: {
        "@type": "Schedule",
        byDay: scheduleDay(s.dayOfWeek),
        startTime: s.startTime.slice(0, 5),
        endTime: s.endTime?.slice(0, 5),
      },
      location: s.location ? { "@type": "Place", name: s.location } : undefined,
    })),
  } : null;

  return (
    <div className="mx-auto max-w-3xl px-4 py-8">
      <SeoTags
        title={`Service Times · ${settings?.churchName ?? ""}`}
        description="Weekly worship and gathering schedule."
        jsonLd={jsonLd}
      />
      <h1 className="text-3xl font-bold sm:text-4xl">Service Times</h1>

      {loading && <p className="mt-6 text-muted">Loading…</p>}
      {!loading && items.length === 0 && (
        <p className="mt-6 text-muted">Service times haven't been published yet.</p>
      )}

      <div className="mt-6 space-y-6">
        {grouped.map((g) => (
          <section key={g.day.value}>
            <h2 className="text-xl font-semibold">{g.day.label}</h2>
            <ul className="mt-2 divide-y rounded-lg border bg-card">
              {g.items.map((s) => (
                <li key={`${s.dayOfWeek}-${s.displayOrder}-${s.name}`} className="p-4">
                  <p className="font-semibold">{s.name}</p>
                  <p className="text-sm text-muted">
                    {s.startTime.slice(0, 5)}{s.endTime && ` – ${s.endTime.slice(0, 5)}`}
                    {s.location && ` · ${s.location}`}
                  </p>
                  {s.notes && <p className="mt-1 text-sm">{s.notes}</p>}
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>
    </div>
  );
}

function scheduleDay(d: DayOfWeek): string {
  return `https://schema.org/${d}`;
}
