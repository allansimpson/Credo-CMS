import { lazy, Suspense, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { eventsApi, type PublicEvent } from "@/lib/api/events";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { Chip, Eyebrow, Headline, ImageSlot } from "@/components/public";
import { Calendar, Clock, MapPin, Mail, ArrowRight } from "lucide-react";
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

  if (loading) return <p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>;
  if (notFound || !event) return <NotFoundPage />;

  const orgName = settings?.churchName ?? null;
  const contactEmail = settings?.contactEmail ?? "office@hopecommunity.church";
  const description = event.location ? `${event.location} · ${orgName}` : orgName;
  const startDate = new Date(event.startsAt);
  const formattedDate = startDate.toLocaleDateString("en-US", {
    weekday: "short", month: "long", day: "numeric", year: "numeric",
  });
  const startTime = startDate.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" });
  const endTime = event.endsAt
    ? new Date(event.endsAt).toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" })
    : null;
  const timeRange = endTime ? `${startTime} – ${endTime}` : startTime;
  const monthLabel = startDate.toLocaleDateString("en-US", { month: "long", year: "numeric" });

  const canRegister = event.registrationMode > 0
    && (!event.registrationOpensAt || new Date(event.registrationOpensAt) <= new Date())
    && (!event.registrationClosesAt || new Date(event.registrationClosesAt) > new Date());

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

  return (
    <article>
      <SeoTags
        title={event.title}
        description={description}
        ogType="article"
        imageUrl={event.heroImageUrl}
        jsonLd={jsonLd}
      />

      {/* ── Breadcrumb ────────────────────────────────────────── */}
      <nav className="bg-inset px-6 py-3 text-[11px] font-medium uppercase tracking-[0.14em] text-inset-foreground/70">
        <div className="mx-auto flex max-w-7xl gap-2">
          <Link to="/events" className="hover:text-inset-foreground">Events</Link>
          <span aria-hidden>/</span>
          <span>{monthLabel}</span>
          <span aria-hidden>/</span>
          <span className="text-accent">{event.title}</span>
        </div>
      </nav>

      {/* ── Content + Sidebar ─────────────────────────────────── */}
      <div className="mx-auto max-w-7xl px-6 py-10 md:py-14">
        <div className="grid gap-10 md:grid-cols-[1fr_20rem]">
          {/* Main content */}
          <div>
            {/* Header */}
            <div className="flex flex-wrap items-center gap-3">
              <Chip tone="accent">Welcome</Chip>
              <span className="font-mono text-xs text-muted">
                {startDate.toLocaleDateString("en-US", { month: "short", day: "numeric" })}
                {" · "}
                {startDate.toLocaleDateString("en-US", { weekday: "short" })}
                {" · "}
                {startTime}
              </span>
            </div>
            <Headline as="h1" size="display" className="mt-3">
              {event.title}
            </Headline>

            {/* Hero image */}
            <div className="mt-8">
              {event.heroImageUrl ? (
                <picture>
                  {event.heroImageWebpUrl && <source srcSet={event.heroImageWebpUrl} type="image/webp" />}
                  <img src={event.heroImageUrl} alt={event.heroImageAlt ?? ""} className="aspect-[4/3] w-full object-cover" />
                </picture>
              ) : (
                <ImageSlot ratio="4:3" label={event.location ?? event.title} alt="" />
              )}
            </div>

            {/* Description body */}
            {event.descriptionJson && (
              <div className="mt-8">
                <Suspense fallback={null}>
                  <TipTapReadOnly json={event.descriptionJson} />
                </Suspense>
              </div>
            )}
          </div>

          {/* Sidebar */}
          <aside className="space-y-6 md:sticky md:top-4 md:self-start">
            {/* Details card — inset (same bg as footer) */}
            <div className="bg-inset p-6 text-inset-foreground">
              <p className="text-[11px] font-semibold uppercase tracking-[0.14em] text-accent">Details</p>
              <ul className="mt-5 space-y-4 text-sm">
                <li className="flex items-start gap-3">
                  <Calendar size={18} strokeWidth={1.5} className="mt-0.5 shrink-0 text-accent" />
                  <span>{formattedDate}</span>
                </li>
                <li className="flex items-start gap-3">
                  <Clock size={18} strokeWidth={1.5} className="mt-0.5 shrink-0 text-accent" />
                  <span>{timeRange}</span>
                </li>
                {event.location && (
                  <li className="flex items-start gap-3">
                    <MapPin size={18} strokeWidth={1.5} className="mt-0.5 shrink-0 text-accent" />
                    <span>{event.location}</span>
                  </li>
                )}
                <li className="flex items-start gap-3">
                  <Mail size={18} strokeWidth={1.5} className="mt-0.5 shrink-0 text-accent" />
                  <a href={`mailto:${contactEmail}`} className="hover:underline">{contactEmail}</a>
                </li>
              </ul>

              <hr className="my-5 border-inset-foreground/15" />

              <div className="space-y-2">
                {canRegister ? (
                  event.externalRegistrationUrl ? (
                    <a href={event.externalRegistrationUrl} target="_blank" rel="noreferrer"
                      className="flex w-full items-center justify-center gap-2 bg-accent py-2.5 text-sm font-semibold text-inset-foreground hover:bg-accent/90">
                      RSVP <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
                    </a>
                  ) : (
                    <Link to={`/events/${event.slug}/register`}
                      className="flex w-full items-center justify-center gap-2 bg-accent py-2.5 text-sm font-semibold text-inset-foreground hover:bg-accent/90">
                      RSVP <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
                    </Link>
                  )
                ) : (
                  <span className="flex w-full items-center justify-center gap-2 bg-accent py-2.5 text-sm font-semibold text-inset-foreground">
                    RSVP <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
                  </span>
                )}
                <a href={`/api/public/events/${event.slug}/ics`}
                  className="flex w-full items-center justify-center gap-2 border border-inset-foreground/25 py-2.5 text-sm font-medium text-inset-foreground hover:bg-inset-foreground/10">
                  <Calendar size={14} strokeWidth={1.5} />
                  Add to calendar
                </a>
              </div>
            </div>

            {/* Hosted by card */}
            <div className="border border-border-soft p-5">
              <p className="font-semibold">Hosted by</p>
              <p className="mt-1 text-sm text-fg-soft">Anna Kowalski · Care</p>
              <a href={`mailto:${contactEmail}`} className="mt-2 inline-flex items-center gap-1.5 text-sm text-accent hover:underline">
                <Mail size={14} strokeWidth={1.5} /> Email Anna
              </a>
            </div>
          </aside>
        </div>
      </div>
    </article>
  );
}
