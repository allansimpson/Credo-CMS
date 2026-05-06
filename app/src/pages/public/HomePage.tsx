import { lazy, Suspense, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { homepageApi, type HomepageDto } from "@/lib/api/homepage";

// TipTap read-only is heavy; only loaded if there's actually a members-only
// welcome message to render.
const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly }))
);

const DAY_LABELS = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

export function HomePage() {
  const { settings } = useSiteSettings();
  const [data, setData] = useState<HomepageDto | null>(null);

  useEffect(() => {
    let cancelled = false;
    homepageApi.get()
      .then((d) => { if (!cancelled) setData(d); })
      .catch(() => {});
    return () => { cancelled = true; };
  }, []);

  const churchName = data?.site.churchName ?? settings?.churchName ?? "Welcome";
  const tagline = data?.site.tagline ?? settings?.tagline;
  const ctaLabel = data?.site.homepageHeroCtaLabel ?? settings?.homepageHeroCtaLabel ?? "Plan your visit";
  const ctaLink = data?.site.homepageHeroCtaLink ?? settings?.homepageHeroCtaLink ?? "#service-times";

  return (
    <main>
      <section className="bg-primary text-primary-foreground">
        <div className="mx-auto max-w-6xl px-4 py-20 text-center md:py-28">
          <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">{churchName}</h1>
          {tagline && <p className="mx-auto mt-4 max-w-2xl text-lg opacity-90">{tagline}</p>}
          <div className="mt-8">
            <Link
              to={ctaLink}
              className="inline-flex h-11 items-center rounded-md bg-accent px-6 font-semibold text-accent-foreground hover:bg-accent/90"
            >
              {ctaLabel}
            </Link>
          </div>
        </div>
      </section>

      {data?.membersWelcomeText && (
        <section className="mx-auto max-w-3xl px-4 py-8">
          <div className="rounded-lg border bg-card p-6">
            <h2 className="text-xl font-semibold">Welcome back</h2>
            <Suspense fallback={null}>
              <TipTapReadOnly json={data.membersWelcomeText} />
            </Suspense>
          </div>
        </section>
      )}

      {data && data.serviceTimes.length > 0 && (
        <section id="service-times" className="mx-auto max-w-5xl px-4 py-12">
          <h2 className="text-2xl font-semibold">Service Times</h2>
          <ul className="mt-4 grid gap-3 sm:grid-cols-2">
            {data.serviceTimes.map((s, i) => (
              <li key={`${s.dayOfWeek}-${i}`} className="rounded-lg border bg-card p-4">
                <p className="text-sm font-semibold">{DAY_LABELS[s.dayOfWeek]} · {s.startTime.slice(0, 5)}</p>
                <p className="mt-1 font-semibold">{s.name}</p>
                {s.location && <p className="text-sm text-muted">{s.location}</p>}
              </li>
            ))}
          </ul>
        </section>
      )}

      <section className="mx-auto max-w-5xl px-4 py-12">
        <div className="grid gap-6 sm:grid-cols-2">
          <PlaceholderCard title="Latest sermon" body="Sermons land in Phase 3." />
          <PlaceholderCard title="Upcoming events" body="Events land in Phase 3." />
        </div>
      </section>

      {data && data.latestNews.length > 0 && (
        <section className="mx-auto max-w-5xl px-4 py-12">
          <div className="flex items-baseline justify-between">
            <h2 className="text-2xl font-semibold">Latest News</h2>
            <Link to="/news" className="text-sm text-primary hover:underline">View all →</Link>
          </div>
          <ul className="mt-4 grid gap-4 sm:grid-cols-2">
            {data.latestNews.map((n) => (
              <li key={n.id} className="rounded-lg border bg-card p-4">
                <Link to={`/news/${n.slug}`} className="block">
                  <h3 className="font-semibold hover:underline">{n.title}</h3>
                  <p className="mt-1 text-xs text-muted">
                    {new Date(n.publishedAt).toLocaleDateString()}
                    {n.isMembersOnly && " · Members only"}
                  </p>
                  {n.excerpt && <p className="mt-2 text-sm text-muted">{n.excerpt}</p>}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}
    </main>
  );
}

function PlaceholderCard({ title, body }: { title: string; body: string }) {
  return (
    <div className="rounded-lg border bg-card p-6">
      <h3 className="text-lg font-semibold">{title}</h3>
      <p className="mt-2 text-sm text-muted">{body}</p>
    </div>
  );
}
