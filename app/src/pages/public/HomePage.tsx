import { lazy, Suspense, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { homepageApi, type HomepageDto } from "@/lib/api/homepage";
import type { PublicTemplate } from "@/types/api";
import {
  BigNum,
  BtnLink,
  Chip,
  Eyebrow,
  Headline,
  ImageSlot,
} from "@/components/public";

// Note: HomePage is rendered inside PublicLayout, which already provides
// the chrome (header + footer) via the legacy shims that delegate to the
// new template-aware <PublicHeader>/<PublicFooter>. The page itself only
// emits section content; wrapping in <PublicPage> would double-render
// the chrome.

// Members-only welcome text is the only place the home page reaches for
// the TipTap renderer. Lazy so anonymous visitors don't pay the cost.
const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly }))
);

/**
 * Public Site PR #2 — Home (Editorial Warm + Quiet Sanctuary).
 *
 * Both templates render from this single source; only treatment differs
 * per the design handoff §7.1. Content shape is identical — same seven
 * blocks in the same order. Editorial uses dark `--inset` bands for the
 * "I'm New" + "Give" strips (matches the prototype); Quiet uses panel +
 * rule dividers throughout and never reaches for dark insets in the
 * body of the page.
 */
export function HomePage() {
  const { settings } = useSiteSettings();
  const [data, setData] = useState<HomepageDto | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    let cancelled = false;
    homepageApi.get()
      .then((d) => { if (!cancelled) { setData(d); setLoaded(true); } })
      .catch(() => { if (!cancelled) setLoaded(true); });
    return () => { cancelled = true; };
  }, []);

  const template: PublicTemplate = settings?.template ?? 0;
  const isEditorial = template === 0;
  const churchName = data?.site.churchName ?? settings?.churchName ?? "Welcome";
  const tagline = data?.site.tagline ?? settings?.tagline ?? null;
  const ctaLabel = data?.site.homepageHeroCtaLabel ?? settings?.homepageHeroCtaLabel ?? "Plan your visit";
  const ctaLink = data?.site.homepageHeroCtaLink ?? settings?.homepageHeroCtaLink ?? "#service-times";

  return (
    <>
      <Hero
        churchName={churchName}
        tagline={tagline}
        ctaLabel={ctaLabel}
        ctaLink={ctaLink}
        serviceTimes={data?.serviceTimes ?? []}
        isEditorial={isEditorial}
      />

      <ThisSundayBlock data={data} loaded={loaded} isEditorial={isEditorial} />

      <ImNewStrip isEditorial={isEditorial} />

      <UpcomingEventsBlock data={data} loaded={loaded} />

      <LatestNewsBlock data={data} loaded={loaded} />

      <BeliefsTeaser />

      <GiveStrip isEditorial={isEditorial} />

      {data?.membersWelcomeText && (
        <section className="bg-panel-alt py-16">
          <div className="mx-auto max-w-3xl px-6">
            <Eyebrow accent>Members</Eyebrow>
            <Headline as="h2" size="h3" className="mt-3">Welcome back</Headline>
            <div className="mt-4 text-fg-soft">
              <Suspense fallback={null}>
                <TipTapReadOnly json={data.membersWelcomeText} />
              </Suspense>
            </div>
          </div>
        </section>
      )}
    </>
  );
}

// ---- Blocks --------------------------------------------------------------

interface HeroProps {
  churchName: string;
  tagline: string | null;
  ctaLabel: string;
  ctaLink: string;
  serviceTimes: HomepageDto["serviceTimes"];
  isEditorial: boolean;
}

function Hero({ churchName, tagline, ctaLabel, ctaLink, serviceTimes, isEditorial }: HeroProps) {
  // Editorial: photo-led overlay, type bottom-left.
  // Quiet: split with type left, photo right.
  // Hero image source is a follow-up SiteSettings field; for PR #2 both
  // treatments render the labelled <ImageSlot> placeholder.
  if (isEditorial) {
    return (
      <section className="relative" data-testid="home-hero" data-template="editorial">
        <ImageSlot
          ratio="21:9"
          alt={`${churchName} sanctuary`}
          label="HERO PHOTO"
          loading="eager"
        />
        <div className="absolute inset-0 bg-inset/40" aria-hidden="true" />
        <div className="absolute inset-0 flex items-end">
          <div className="mx-auto w-full max-w-7xl px-6 pb-10 sm:pb-16">
            <Eyebrow accent tone="inverse">A church for everyone</Eyebrow>
            <Headline size="display" tone="inverse" className="mt-4 max-w-3xl">
              {churchName}
            </Headline>
            {tagline && (
              <p className="mt-4 max-w-xl text-lg text-inset-foreground/90">{tagline}</p>
            )}
            <HeroBottomRow
              ctaLabel={ctaLabel}
              ctaLink={ctaLink}
              serviceTimes={serviceTimes}
              tone="inverse"
            />
          </div>
        </div>
      </section>
    );
  }

  // Quiet: split layout.
  return (
    <section data-testid="home-hero" data-template="quiet">
      <div className="mx-auto grid max-w-7xl gap-10 px-6 py-16 md:grid-cols-2 md:gap-16 md:py-24">
        <div className="flex flex-col justify-center">
          <Eyebrow>A church for everyone</Eyebrow>
          <Headline size="display" className="mt-4">{churchName}</Headline>
          {tagline && <p className="mt-6 max-w-xl text-xl text-fg-soft">{tagline}</p>}
          <HeroBottomRow
            ctaLabel={ctaLabel}
            ctaLink={ctaLink}
            serviceTimes={serviceTimes}
            tone="default"
          />
        </div>
        <ImageSlot
          ratio="4:5"
          alt={`${churchName} sanctuary`}
          label="HERO PHOTO"
          loading="eager"
        />
      </div>
    </section>
  );
}

interface HeroBottomRowProps {
  ctaLabel: string;
  ctaLink: string;
  serviceTimes: HomepageDto["serviceTimes"];
  tone: "default" | "inverse";
}

function HeroBottomRow({ ctaLabel, ctaLink, serviceTimes, tone }: HeroBottomRowProps) {
  const isInverse = tone === "inverse";
  // First configured service is "the next one" for the hero summary.
  const next = serviceTimes[0];
  const ctaVariant = isInverse ? "inverseFilled" : "primary";

  return (
    <div className="mt-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:gap-6">
      <BtnLink to={ctaLink} variant={ctaVariant} size="lg">{ctaLabel}</BtnLink>
      {next && (
        <div className={isInverse ? "text-inset-foreground/80" : "text-fg-soft"}>
          <span className="font-mono text-xs uppercase tracking-[0.18em]">
            {next.dayOfWeek}
          </span>
          {" · "}
          <span className="font-medium">{formatTime(next.startTime)}</span>
          {next.location && <> · {next.location}</>}
        </div>
      )}
    </div>
  );
}

interface ThisSundayBlockProps {
  data: HomepageDto | null;
  loaded: boolean;
  isEditorial: boolean;
}

function ThisSundayBlock({ data, loaded, isEditorial }: ThisSundayBlockProps) {
  return (
    <section className="border-t border-border-soft">
      <div className="mx-auto max-w-7xl px-6 py-16 md:py-20">
        <Eyebrow accent>This Sunday</Eyebrow>
        <Headline as="h2" size="h2" className="mt-3 max-w-2xl">
          {isEditorial ? "Coming up this Sunday" : "Join us this Sunday"}
        </Headline>
        <div className="mt-8 grid gap-8 md:grid-cols-[2fr_3fr]">
          <ImageSlot
            ratio="4:3"
            alt={data?.latestSermon?.title ?? "Most recent sermon"}
            label="SERMON THUMBNAIL"
            src={data?.latestSermon?.thumbnailBlobUrl ?? null}
            webpSrc={data?.latestSermon?.thumbnailWebpBlobUrl ?? null}
          />
          <div className="flex flex-col justify-center">
            {!loaded ? (
              <p className="text-sm text-muted">Loading…</p>
            ) : data?.latestSermon ? (
              <>
                <Eyebrow>
                  {data.latestSermon.sermonSeriesTitle ?? "Latest sermon"}
                </Eyebrow>
                <Headline as="h3" size="h3" className="mt-3">
                  {data.latestSermon.title}
                </Headline>
                <p className="mt-3 text-fg-soft">
                  {data.latestSermon.speakerName && (
                    <>
                      <span className="font-mono text-xs uppercase tracking-[0.18em]">
                        {data.latestSermon.speakerName}
                      </span>
                      {" · "}
                    </>
                  )}
                  <span className="font-mono text-xs uppercase tracking-[0.18em]">
                    {new Date(data.latestSermon.publishedAt).toLocaleDateString()}
                  </span>
                </p>
                <div className="mt-5 flex items-center gap-3">
                  <BtnLink to={`/sermons/${data.latestSermon.slug}`} variant="primary">
                    Watch
                  </BtnLink>
                  {data.latestSermon.sermonSeriesId && data.latestSermon.sermonSeriesTitle && (
                    <BtnLink
                      to={`/sermons/series/${data.latestSermon.sermonSeriesId}`}
                      variant="ghost"
                    >
                      View series
                    </BtnLink>
                  )}
                </div>
              </>
            ) : (
              <>
                <Eyebrow>This week</Eyebrow>
                <Headline as="h3" size="h3" className="mt-3">No sermons published yet</Headline>
                <p className="mt-3 text-fg-soft">
                  The latest sermon will appear here once it's published.
                </p>
              </>
            )}
          </div>
        </div>
      </div>
    </section>
  );
}

function ImNewStrip({ isEditorial }: { isEditorial: boolean }) {
  if (isEditorial) {
    return (
      <section
        data-testid="im-new-strip"
        data-template="editorial"
        className="bg-inset text-inset-foreground"
      >
        <div className="mx-auto max-w-7xl px-6 py-16 md:py-20">
          <div className="grid items-center gap-8 md:grid-cols-[3fr_2fr]">
            <div>
              <Eyebrow accent tone="inverse">First time?</Eyebrow>
              <Headline as="h2" size="h2" tone="inverse" className="mt-3">
                We saved you a seat.
              </Headline>
              <p className="mt-4 max-w-xl text-lg text-inset-foreground/85">
                Whatever you wore on Saturday. There's an offering, but if
                you're visiting we genuinely don't expect anything from you.
                Just be here.
              </p>
            </div>
            <div className="md:justify-self-end">
              <BtnLink to="/im-new" variant="inverseFilled" size="lg">
                What to expect
              </BtnLink>
            </div>
          </div>
        </div>
      </section>
    );
  }

  // Quiet: panel + rule dividers, no dark inset.
  return (
    <section
      data-testid="im-new-strip"
      data-template="quiet"
      className="border-t border-border-soft"
    >
      <div className="mx-auto max-w-4xl px-6 py-20 text-center md:py-28">
        <Eyebrow>First time?</Eyebrow>
        <Headline as="h2" size="h2" className="mt-3">
          We saved you a seat.
        </Headline>
        <p className="mx-auto mt-6 max-w-2xl text-lg text-fg-soft">
          Whatever you wore on Saturday. There's an offering, but if you're
          visiting we genuinely don't expect anything from you. Just be here.
        </p>
        <div className="mt-8 flex justify-center">
          <BtnLink to="/im-new" variant="primary" size="lg">What to expect</BtnLink>
        </div>
      </div>
    </section>
  );
}

interface BlockProps {
  data: HomepageDto | null;
  loaded: boolean;
}

function UpcomingEventsBlock({ data, loaded }: BlockProps) {
  const events = data?.upcomingEvents ?? [];
  return (
    <section className="border-t border-border-soft">
      <div className="mx-auto max-w-7xl px-6 py-16 md:py-20">
        <div className="flex items-baseline justify-between gap-4">
          <div>
            <Eyebrow accent>Calendar</Eyebrow>
            <Headline as="h2" size="h2" className="mt-3">Upcoming events</Headline>
          </div>
          <Link
            to="/events"
            className="text-sm font-medium underline underline-offset-4 hover:text-accent"
          >
            View all events →
          </Link>
        </div>
        <div className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {!loaded
            ? Array.from({ length: 4 }).map((_, i) => <EventCardPlaceholder key={i} />)
            : events.length === 0
            ? <p className="text-fg-soft">Nothing scheduled yet — check back soon.</p>
            : events.map((e) => <EventCard key={e.id} event={e} />)}
        </div>
      </div>
    </section>
  );
}

function EventCard({ event }: { event: HomepageDto["upcomingEvents"][number] }) {
  const next = new Date(event.nextOccurrenceAt);
  const day = next.toLocaleDateString(undefined, { weekday: "short" });
  const dayNum = next.getDate();
  const month = next.toLocaleDateString(undefined, { month: "short" });
  return (
    <Link
      to={`/events/${event.slug}`}
      className="group flex flex-col gap-3 border-l-2 border-transparent pl-4 transition-colors hover:border-accent"
    >
      <div className="flex items-baseline gap-3">
        <Eyebrow>{day}</Eyebrow>
        <BigNum size="md">{dayNum}</BigNum>
        <Eyebrow>{month}</Eyebrow>
      </div>
      <h3 className="font-heading text-lg font-semibold leading-snug group-hover:text-accent">
        {event.title}
      </h3>
      {event.location && <p className="text-sm text-fg-soft">{event.location}</p>}
    </Link>
  );
}

function EventCardPlaceholder() {
  return (
    <div className="flex flex-col gap-3 pl-4">
      <div className="h-3 w-20 bg-panel-alt" />
      <div className="h-8 w-10 bg-panel-alt" />
      <div className="h-4 w-40 bg-panel-alt" />
    </div>
  );
}

function LatestNewsBlock({ data, loaded }: BlockProps) {
  const news = data?.latestNews ?? [];
  return (
    <section className="border-t border-border-soft bg-panel-alt/50">
      <div className="mx-auto max-w-7xl px-6 py-16 md:py-20">
        <div className="flex items-baseline justify-between gap-4">
          <div>
            <Eyebrow accent>Latest</Eyebrow>
            <Headline as="h2" size="h2" className="mt-3">News from our church</Headline>
          </div>
          <Link
            to="/news"
            className="text-sm font-medium underline underline-offset-4 hover:text-accent"
          >
            All news →
          </Link>
        </div>
        <div className="mt-8 grid gap-6 md:grid-cols-3">
          {!loaded
            ? Array.from({ length: 3 }).map((_, i) => <NewsCardPlaceholder key={i} />)
            : news.length === 0
            ? <p className="text-fg-soft">No news yet — your first post will land here.</p>
            : news.map((n) => <NewsCard key={n.id} item={n} />)}
        </div>
      </div>
    </section>
  );
}

function NewsCard({ item }: { item: HomepageDto["latestNews"][number] }) {
  return (
    <Link to={`/news/${item.slug}`} className="group block">
      <ImageSlot
        ratio="3:2"
        alt={item.heroImageAlt ?? item.title}
        label="ARTICLE IMAGE"
        src={item.heroImageUrl}
        webpSrc={item.heroImageWebpUrl}
      />
      <div className="mt-4 flex items-center gap-2">
        {item.isMembersOnly && <Chip tone="accent">Members</Chip>}
        <span className="font-mono text-xs uppercase tracking-[0.18em] text-muted">
          {new Date(item.publishedAt).toLocaleDateString()}
        </span>
      </div>
      <h3 className="mt-3 font-heading text-xl font-semibold leading-snug group-hover:text-accent">
        {item.title}
      </h3>
      {item.excerpt && <p className="mt-2 text-fg-soft">{item.excerpt}</p>}
    </Link>
  );
}

function NewsCardPlaceholder() {
  return (
    <div className="flex flex-col gap-3">
      <ImageSlot ratio="3:2" alt="" label="ARTICLE IMAGE" />
      <div className="h-3 w-24 bg-panel-alt" />
      <div className="h-5 w-full bg-panel-alt" />
      <div className="h-4 w-3/4 bg-panel-alt" />
    </div>
  );
}

function BeliefsTeaser() {
  return (
    <section className="border-t border-border-soft">
      <div className="mx-auto max-w-7xl px-6 py-16 md:py-20">
        <div className="grid gap-8 md:grid-cols-[2fr_3fr] md:items-end">
          <div>
            <Eyebrow accent>What we believe</Eyebrow>
            <Headline as="h2" size="h2" className="mt-3">
              We're glad to belong to a faith that's older than us.
            </Headline>
          </div>
          <div>
            <p className="text-lg text-fg-soft">
              Scripture, the historic creeds, weekly communion. The same
              gospel that's been preached for two thousand years, ordinary
              people gathered to hear it together.
            </p>
            <div className="mt-6">
              <BtnLink to="/what-we-believe" variant="secondary">
                Read our beliefs
              </BtnLink>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function GiveStrip({ isEditorial }: { isEditorial: boolean }) {
  // TODO: SiteSettings.GivingUrl — currently a placeholder anchor. Follow-up
  // PR adds the field + admin Branding control + opens in a new tab.
  const giveHref = "#give";
  if (isEditorial) {
    return (
      <section
        data-testid="give-strip"
        data-template="editorial"
        className="bg-inset text-inset-foreground"
      >
        <div className="mx-auto max-w-7xl px-6 py-16 md:py-20">
          <div className="grid items-center gap-8 md:grid-cols-[3fr_2fr]">
            <div>
              <Eyebrow accent tone="inverse">Give</Eyebrow>
              <Headline as="h2" size="h2" tone="inverse" className="mt-3">
                Generosity sustains the work.
              </Headline>
              <p className="mt-4 max-w-xl text-lg text-inset-foreground/85">
                If you call this place home, your giving keeps the lights on
                and the ministry going. If you're a visitor, please don't
                feel obligated.
              </p>
            </div>
            <div className="md:justify-self-end">
              <BtnLink to={giveHref} variant="inverseFilled" size="lg">
                Give online
              </BtnLink>
            </div>
          </div>
        </div>
      </section>
    );
  }
  return (
    <section
      data-testid="give-strip"
      data-template="quiet"
      className="border-t border-border-soft"
    >
      <div className="mx-auto max-w-4xl px-6 py-20 text-center md:py-28">
        <Eyebrow>Give</Eyebrow>
        <Headline as="h2" size="h2" className="mt-3">
          Generosity sustains the work.
        </Headline>
        <p className="mx-auto mt-6 max-w-2xl text-lg text-fg-soft">
          If you call this place home, your giving keeps the lights on and
          the ministry going. If you're a visitor, please don't feel
          obligated.
        </p>
        <div className="mt-8 flex justify-center">
          <BtnLink to={giveHref} variant="primary" size="lg">Give online</BtnLink>
        </div>
      </div>
    </section>
  );
}

// ---- Helpers -------------------------------------------------------------

/** Trims a "HH:mm:ss" service-time string to "HH:mm". */
function formatTime(raw: string): string {
  return raw.slice(0, 5);
}
