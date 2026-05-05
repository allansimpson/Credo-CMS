import { lazy, Suspense, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { eventsApi, type PublicEvent } from "@/lib/api/events";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { ApiError } from "@/lib/apiClient";
import { NotFoundPage } from "@/pages/NotFoundPage";

const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly }))
);

export function EventDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const { settings } = useSiteSettings();
  const [event, setEvent] = useState<PublicEvent | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    eventsApi.getPublic(slug)
      .then((e) => { if (!cancelled) { setEvent(e); setLoading(false); } })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404) setNotFound(true);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [slug]);

  if (loading) return <p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>;
  if (notFound || !event) return <NotFoundPage />;

  const orgName = settings?.churchName ?? null;
  const description = event.location ? `${event.location} · ${orgName}` : orgName;

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "Event",
    name: event.title,
    startDate: event.startsAt,
    endDate: event.endsAt ?? undefined,
    location: event.location ? { "@type": "Place", name: event.location } : undefined,
    image: event.heroImageUrl ? [event.heroImageUrl] : undefined,
    organizer: orgName ? { "@type": "Organization", name: orgName } : undefined,
  };

  const canRegister = event.registrationMode > 0
    && (!event.registrationOpensAt || new Date(event.registrationOpensAt) <= new Date())
    && (!event.registrationClosesAt || new Date(event.registrationClosesAt) > new Date());

  return (
    <article className="mx-auto max-w-3xl px-4 py-8">
      <SeoTags
        title={event.title}
        description={description}
        ogType="article"
        imageUrl={event.heroImageUrl}
        jsonLd={jsonLd}
      />

      {event.heroImageUrl && (
        <picture>
          {event.heroImageWebpUrl && <source srcSet={event.heroImageWebpUrl} type="image/webp" />}
          <img src={event.heroImageUrl} alt={event.heroImageAlt ?? ""}
            className="mb-6 w-full object-cover" style={{ maxHeight: 480 }} />
        </picture>
      )}

      <h1 className="text-3xl font-bold sm:text-4xl">{event.title}</h1>
      <p className="mt-2 text-sm text-muted-foreground">
        {new Date(event.startsAt).toLocaleString()}
        {event.endsAt && ` – ${new Date(event.endsAt).toLocaleString()}`}
        {event.location && ` · ${event.location}`}
        {event.visibility === 1 && " · Members only"}
      </p>

      {event.recurrenceRule && event.nextOccurrences.length > 1 && (
        <section className="mt-6 border bg-card p-4">
          <h2 className="text-sm font-semibold">Upcoming occurrences</h2>
          <ul className="mt-2 space-y-1 text-sm text-muted-foreground">
            {event.nextOccurrences.slice(0, 8).map((d, i) => (
              <li key={i}>{new Date(d).toLocaleString()}</li>
            ))}
          </ul>
        </section>
      )}

      {canRegister && (
        <div className="mt-6">
          {event.externalRegistrationUrl ? (
            <a href={event.externalRegistrationUrl} target="_blank" rel="noreferrer"
              className="inline-flex h-11 items-center justify-center bg-primary px-6 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
              Register externally ↗
            </a>
          ) : (
            <Link to={`/events/${event.slug}/register`}
              className="inline-flex h-11 items-center justify-center bg-primary px-6 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
              Register
            </Link>
          )}
        </div>
      )}

      {event.descriptionJson && (
        <div className="mt-8">
          <Suspense fallback={null}>
            <TipTapReadOnly json={event.descriptionJson} />
          </Suspense>
        </div>
      )}

      <section className="mt-8">
        <h2 className="text-sm font-semibold">Add to calendar</h2>
        <div className="mt-2 flex flex-wrap gap-2">
          <a href={`/api/public/events/${event.slug}/ics`} className="text-sm text-primary hover:underline">
            Download .ics
          </a>
          <a href={googleCalendarUrl(event)} target="_blank" rel="noreferrer"
            className="text-sm text-primary hover:underline">Google Calendar</a>
          <a href={outlookUrl(event)} target="_blank" rel="noreferrer"
            className="text-sm text-primary hover:underline">Outlook</a>
        </div>
      </section>
    </article>
  );
}

function googleCalendarUrl(e: PublicEvent): string {
  const start = (e.startsAt || "").replace(/[-:]/g, "").replace(/\.\d+/, "");
  const end = ((e.endsAt ?? e.startsAt) || "").replace(/[-:]/g, "").replace(/\.\d+/, "");
  const params = new URLSearchParams({
    action: "TEMPLATE",
    text: e.title,
    dates: `${start}/${end}`,
  });
  if (e.location) params.set("location", e.location);
  return `https://www.google.com/calendar/render?${params}`;
}

function outlookUrl(e: PublicEvent): string {
  const params = new URLSearchParams({
    path: "/calendar/action/compose",
    rru: "addevent",
    subject: e.title,
    startdt: e.startsAt,
    enddt: e.endsAt ?? e.startsAt,
  });
  if (e.location) params.set("location", e.location);
  return `https://outlook.live.com/calendar/0/deeplink/compose?${params}`;
}
