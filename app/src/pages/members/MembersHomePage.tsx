import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { HandHeart, Users, UsersRound, ChevronRight, Heart, Calendar, Lock, Play } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { memberPrayerApi, type PrayerRequestListItem } from "@/lib/api/prayerRequests";
import { profileRegistrationsApi, type MyRegistration } from "@/lib/api/eventRegistration";
import { newsApi } from "@/lib/api/news";
import { publicSermonsApi } from "@/lib/api/publicSermons";
import type { PublicNewsItem } from "@/types/api";
import type { SermonListItem } from "@/lib/api/sermons";
import {
  Content,
  PageHead,
  Panel,
  QuickActionTile,
  SkeletonCard,
} from "@/components/members/portal-primitives";

/**
 * Portal home: greeting + quick-action row + dashboard digest grid.
 * Each digest panel reads only the data it can — the API surface is
 * lean today, so the grid focuses on prayer requests + a placeholder
 * for events RSVPs (real EventRegistration data lives on its own
 * controller and can be wired in v1.1 polish).
 */
export function MembersHomePage() {
  const { user } = useAuth();
  const { settings } = useSiteSettings();
  const navigate = useNavigate();
  const firstName = user?.firstName ?? "there";
  const greeting = greetingFor(new Date());

  const [myPrayers, setMyPrayers] = useState<PrayerRequestListItem[] | null>(null);
  const [prayersError, setPrayersError] = useState(false);
  const [myEvents, setMyEvents] = useState<MyRegistration[] | null>(null);
  const [eventsError, setEventsError] = useState(false);
  const [news, setNews] = useState<PublicNewsItem[] | null>(null);
  const [sermons, setSermons] = useState<SermonListItem[] | null>(null);
  const [latestError, setLatestError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    memberPrayerApi.list()
      .then((items) => {
        if (cancelled) return;
        const mine = items.filter((p) => p.viewerCanEdit);
        setMyPrayers(mine.slice(0, 3));
      })
      .catch(() => { if (!cancelled) setPrayersError(true); });

    profileRegistrationsApi.list()
      .then((items) => {
        if (cancelled) return;
        // Upcoming + still registered (status 0 = Registered). Take top 3.
        const now = Date.now();
        const upcoming = items
          .filter((r) => r.status === 0 && new Date(r.eventStartsAt).getTime() >= now)
          .sort((a, b) => new Date(a.eventStartsAt).getTime() - new Date(b.eventStartsAt).getTime())
          .slice(0, 3);
        setMyEvents(upcoming);
      })
      .catch(() => { if (!cancelled) setEventsError(true); });

    Promise.all([
      newsApi.listPublic(1, 3).catch(() => null),
      publicSermonsApi.list({ page: 1, pageSize: 3 }).catch(() => null),
    ]).then(([newsResp, sermonResp]) => {
      if (cancelled) return;
      if (!newsResp && !sermonResp) { setLatestError(true); return; }
      setNews(newsResp?.items ?? []);
      setSermons(sermonResp?.items ?? []);
    });

    return () => { cancelled = true; };
  }, []);

  return (
    <Content>
      <PageHead
        title={`${greeting}, ${firstName}.`}
        sub={settings?.churchName ? `Welcome back to ${settings.churchName}.` : undefined}
      />

      {/* Quick actions */}
      <section className="mb-7 grid gap-3 sm:grid-cols-3">
        <QuickActionTile
          icon={<HandHeart strokeWidth={1.6} className="h-4 w-4" />}
          label="Submit prayer request"
          onClick={() => navigate("/members/prayer/new")}
        />
        <QuickActionTile
          icon={<Users strokeWidth={1.6} className="h-4 w-4" />}
          label="Browse directory"
          onClick={() => navigate("/members/directory")}
        />
        <QuickActionTile
          icon={<UsersRound strokeWidth={1.6} className="h-4 w-4" />}
          label="Find a group"
          onClick={() => navigate("/members/groups")}
        />
      </section>

      {/* Digest grid */}
      <section className="grid gap-4 md:grid-cols-2">
        {/* My prayer requests */}
        <Panel noPad>
          <header className="flex items-center justify-between border-b border-border-soft px-4 py-3">
            <h2 className="font-heading text-sm font-semibold">My prayer requests</h2>
            <button
              type="button"
              onClick={() => navigate("/members/prayer")}
              className="inline-flex items-center gap-1 font-mono text-[10px] uppercase tracking-[0.12em] text-muted hover:text-foreground"
            >
              View all <ChevronRight strokeWidth={1.5} className="h-3 w-3" />
            </button>
          </header>
          <div className="divide-y divide-border-soft">
            {prayersError && (
              <p className="px-4 py-6 text-sm text-danger">Couldn't load.</p>
            )}
            {!prayersError && myPrayers === null && (
              <div className="space-y-3 p-4">
                <SkeletonCard />
                <SkeletonCard />
              </div>
            )}
            {myPrayers && myPrayers.length === 0 && (
              <p className="px-4 py-6 text-sm text-muted">
                You haven't submitted any prayer requests yet.{" "}
                <button
                  type="button"
                  onClick={() => navigate("/members/prayer/new")}
                  className="text-accent underline"
                >
                  Submit one
                </button>
                .
              </p>
            )}
            {myPrayers?.map((p) => (
              <button
                key={p.id}
                type="button"
                onClick={() => navigate(`/members/prayer/${p.id}`)}
                className="flex w-full items-start gap-3 px-4 py-3 text-left hover:bg-panel-alt"
              >
                <div className="min-w-0 flex-1">
                  <p className="truncate font-heading text-sm font-semibold">{p.title}</p>
                  <p className="mt-0.5 font-mono text-[10px] uppercase tracking-[0.12em] text-muted">
                    {p.prayedForCount} prayed · {p.updateCount} update{p.updateCount === 1 ? "" : "s"}
                  </p>
                </div>
                <Heart
                  strokeWidth={1.75}
                  className={`mt-0.5 h-4 w-4 ${p.viewerHasPrayed ? "fill-accent text-accent" : "text-muted"}`}
                />
              </button>
            ))}
          </div>
        </Panel>

        {/* Events I'm registered for */}
        <Panel noPad>
          <header className="flex items-center justify-between border-b border-border-soft px-4 py-3">
            <h2 className="font-heading text-sm font-semibold">Events I'm registered for</h2>
            <button
              type="button"
              onClick={() => navigate("/profile/registrations")}
              className="inline-flex items-center gap-1 font-mono text-[10px] uppercase tracking-[0.12em] text-muted hover:text-foreground"
            >
              View all <ChevronRight strokeWidth={1.5} className="h-3 w-3" />
            </button>
          </header>
          <div className="divide-y divide-border-soft">
            {eventsError && (
              <p className="px-4 py-6 text-sm text-danger">Couldn't load.</p>
            )}
            {!eventsError && myEvents === null && (
              <div className="space-y-3 p-4">
                <SkeletonCard />
                <SkeletonCard />
              </div>
            )}
            {myEvents && myEvents.length === 0 && (
              <p className="px-4 py-6 text-sm text-muted">
                You haven't registered for any upcoming events.
              </p>
            )}
            {myEvents?.map((r) => (
              <Link
                key={r.id}
                to={`/events/${encodeURIComponent(r.eventSlug)}`}
                className="flex w-full items-start gap-3 px-4 py-3 text-left hover:bg-panel-alt"
              >
                <Calendar strokeWidth={1.6} className="mt-0.5 h-4 w-4 shrink-0 text-muted" />
                <div className="min-w-0 flex-1">
                  <p className="truncate font-heading text-sm font-semibold">{r.eventTitle}</p>
                  <p className="mt-0.5 font-mono text-[10px] uppercase tracking-[0.12em] text-muted">
                    {formatEventDate(r.eventStartsAt, r.occurrenceDate)}
                  </p>
                </div>
              </Link>
            ))}
          </div>
        </Panel>

        {/* Latest for members */}
        <Panel noPad className="md:col-span-2">
          <header className="flex items-center justify-between border-b border-border-soft px-4 py-3">
            <h2 className="font-heading text-sm font-semibold">Latest for members</h2>
          </header>
          <div className="grid divide-y divide-border-soft sm:grid-cols-2 sm:divide-x sm:divide-y-0">
            <LatestNewsColumn news={news} error={latestError} />
            <LatestSermonsColumn sermons={sermons} error={latestError} />
          </div>
        </Panel>
      </section>
    </Content>
  );
}

function greetingFor(d: Date): string {
  const h = d.getHours();
  if (h < 12) return "Good morning";
  if (h < 18) return "Good afternoon";
  return "Good evening";
}

function formatEventDate(eventStartsAt: string, occurrenceDate: string | null): string {
  const iso = occurrenceDate ?? eventStartsAt;
  return new Date(iso).toLocaleDateString("en-US", {
    weekday: "short", month: "short", day: "numeric",
  });
}

function LatestNewsColumn({ news, error }: { news: PublicNewsItem[] | null; error: boolean }) {
  return (
    <div>
      <p className="border-b border-border-soft px-4 py-2 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
        News
      </p>
      <div className="divide-y divide-border-soft">
        {error && <p className="px-4 py-4 text-sm text-danger">Couldn't load.</p>}
        {!error && news === null && (
          <div className="space-y-2 p-4"><SkeletonCard /></div>
        )}
        {news && news.length === 0 && (
          <p className="px-4 py-4 text-sm text-muted">Nothing new yet.</p>
        )}
        {news?.map((n) => (
          <Link
            key={n.id}
            to={`/news/${encodeURIComponent(n.slug)}`}
            className="block px-4 py-3 hover:bg-panel-alt"
          >
            <p className="line-clamp-2 font-heading text-sm font-medium leading-snug">
              {n.title}
            </p>
            <p className="mt-1 flex items-center gap-2 font-mono text-[10px] uppercase tracking-[0.12em] text-muted">
              {new Date(n.calendarDate ?? n.publishedAt).toLocaleDateString("en-US", {
                month: "short", day: "numeric",
              })}
              {n.isMembersOnly && (
                <Lock strokeWidth={1.75} className="h-3 w-3 text-accent" aria-label="Members only" />
              )}
            </p>
          </Link>
        ))}
      </div>
    </div>
  );
}

function LatestSermonsColumn({ sermons, error }: { sermons: SermonListItem[] | null; error: boolean }) {
  return (
    <div>
      <p className="border-b border-border-soft px-4 py-2 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
        Sermons
      </p>
      <div className="divide-y divide-border-soft">
        {error && <p className="px-4 py-4 text-sm text-danger">Couldn't load.</p>}
        {!error && sermons === null && (
          <div className="space-y-2 p-4"><SkeletonCard /></div>
        )}
        {sermons && sermons.length === 0 && (
          <p className="px-4 py-4 text-sm text-muted">Nothing new yet.</p>
        )}
        {sermons?.map((s) => (
          <Link
            key={s.id}
            to={`/sermons/${encodeURIComponent(s.slug)}`}
            className="block px-4 py-3 hover:bg-panel-alt"
          >
            <p className="line-clamp-2 font-heading text-sm font-medium leading-snug">
              {s.title}
            </p>
            <p className="mt-1 flex items-center gap-2 font-mono text-[10px] uppercase tracking-[0.12em] text-muted">
              <Play strokeWidth={1.75} className="h-3 w-3" />
              {new Date(s.publishedAt).toLocaleDateString("en-US", {
                month: "short", day: "numeric",
              })}
              {s.isMembersOnly && (
                <Lock strokeWidth={1.75} className="h-3 w-3 text-accent" aria-label="Members only" />
              )}
            </p>
          </Link>
        ))}
      </div>
    </div>
  );
}
