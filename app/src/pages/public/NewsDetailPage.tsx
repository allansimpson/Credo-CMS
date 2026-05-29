import { lazy, Suspense, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { newsApi } from "@/lib/api/news";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { Chip, Eyebrow, Headline, ImageSlot } from "@/components/public";
import type { PublicNewsDetail, PublicNewsItem } from "@/types/api";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { ApiError } from "@/lib/apiClient";

const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly }))
);

export function NewsDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const { settings } = useSiteSettings();
  const [item, setItem] = useState<PublicNewsDetail | null>(null);
  const [related, setRelated] = useState<PublicNewsItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setLoading(true);
    setNotFound(false);
    newsApi.getPublic(slug)
      .then((n) => {
        if (cancelled) return;
        setItem(n);
        setLoading(false);
        newsApi.listPublic(1, 4)
          .then((res) => {
            if (!cancelled) {
              setRelated(res.items.filter((r) => r.slug !== slug).slice(0, 3));
            }
          })
          .catch(() => {});
      })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404) setNotFound(true);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [slug]);

  if (loading) return <p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>;
  if (notFound || !item) return <NotFoundPage />;

  const description = item.metaDescription ?? item.excerpt ?? settings?.churchName ?? null;
  const orgName = settings?.churchName ?? null;
  const displayDate = new Date(item.calendarDate ?? item.publishedAt);
  const formattedDate = displayDate.toLocaleDateString("en-US", {
    month: "short", day: "numeric", year: "numeric",
  });

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "Article",
    headline: item.title,
    datePublished: item.publishedAt,
    description,
    publisher: orgName ? { "@type": "Organization", name: orgName } : undefined,
    image: item.heroImageUrl ? [item.heroImageUrl] : undefined,
  };

  return (
    <article>
      <SeoTags
        title={item.title}
        description={description}
        ogType="article"
        imageUrl={item.heroImageUrl}
        jsonLd={jsonLd}
      />

      {/* ── Centered header ───────────────────────────────────── */}
      <header className="mx-auto max-w-3xl px-6 py-10 text-center md:py-14">
        <Chip tone="accent">Pastoral letter</Chip>
        <Headline as="h1" size="h1" className="mt-4">
          {item.title}
        </Headline>
        <p className="mt-3 font-mono text-xs uppercase tracking-wide text-muted">
          {formattedDate}
        </p>
      </header>

      {/* ── Hero image (omitted entirely when no image is set) ── */}
      {item.heroImageUrl && (
        <section className="mx-auto max-w-5xl px-6">
          <picture>
            {item.heroImageWebpUrl && <source srcSet={item.heroImageWebpUrl} type="image/webp" />}
            <img
              src={item.heroImageUrl}
              alt={item.heroImageAlt ?? ""}
              className="aspect-[16/9] w-full object-cover"
            />
          </picture>
        </section>
      )}

      {/* ── Body ──────────────────────────────────────────────── */}
      <div className="mx-auto max-w-3xl px-6 py-10 md:py-14">
        <div className="prose-editorial">
          <Suspense fallback={null}>
            <TipTapReadOnly json={item.bodyJson} />
          </Suspense>
        </div>
      </div>

      {/* ── Related articles ──────────────────────────────────── */}
      {related.length > 0 && (
        <section className="border-t border-border-soft">
          <div className="mx-auto max-w-7xl px-6 py-10 md:py-14">
            <Eyebrow accent>More from the church</Eyebrow>

            <div className="mt-6 grid gap-8 md:grid-cols-3">
              {related.map((r) => (
                <Link key={r.id} to={`/news/${r.slug}`} className="group block">
                  <h3 className="font-semibold leading-snug group-hover:underline">
                    {r.title}
                  </h3>
                  <p className="mt-1 font-mono text-[10px] uppercase tracking-wide text-muted">
                    {new Date(r.calendarDate ?? r.publishedAt).toLocaleDateString("en-US", {
                      month: "short", day: "numeric", year: "numeric",
                    })}
                  </p>
                </Link>
              ))}
            </div>
          </div>
        </section>
      )}
    </article>
  );
}
