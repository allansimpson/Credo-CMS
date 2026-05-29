import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowRight, ArrowDown } from "lucide-react";
import { newsApi } from "@/lib/api/news";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { Chip, Eyebrow, Headline, ImageSlot } from "@/components/public";
import type { PagedResult, PublicNewsItem } from "@/types/api";

export function PublicNewsListPage() {
  const { settings } = useSiteSettings();
  const [data, setData] = useState<PagedResult<PublicNewsItem> | null>(null);
  const [page, setPage] = useState(1);
  const [accumulated, setAccumulated] = useState<PublicNewsItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [allCategories, setAllCategories] = useState<string[]>([]);

  // One-time pass to harvest the categories actually in use on published
  // items. Mirrors the Events page so the filter row only shows chips that
  // will yield results.
  useEffect(() => {
    let cancelled = false;
    newsApi.listPublic(1, 50).then((d) => {
      if (cancelled) return;
      const cats = Array.from(new Set(
        d.items.map((n) => n.category).filter((c): c is string => !!c)
      )).sort();
      setAllCategories(cats);
    }).catch(() => { /* leave empty */ });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    newsApi.listPublic(page, 12, selectedCategory ?? undefined)
      .then((d) => {
        if (cancelled) return;
        setData(d);
        setAccumulated((prev) => page === 1 ? d.items : [...prev, ...d.items]);
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [page, selectedCategory]);

  useEffect(() => { setPage(1); setAccumulated([]); }, [selectedCategory]);

  const featured = accumulated[0] ?? null;
  const rest = accumulated.slice(1);

  return (
    <div>
      <SeoTags
        title={`News · ${settings?.churchName ?? ""}`}
        description="Pastoral letters, stories from our community, occasional thoughts on what we're up to and why."
      />

      {/* ── Header ────────────────────────────────────────────── */}
      <header className="mx-auto max-w-7xl px-6 py-10 md:py-14">
        <Eyebrow accent>News &amp; Writing</Eyebrow>
        <Headline as="h1" size="display" className="mt-3">
          From the Church.
        </Headline>
        <p className="mt-4 max-w-xl text-fg-soft leading-relaxed">
          Preachers notes, stories from our community, occasional thoughts on what
          we&rsquo;re up to and why.
        </p>
      </header>

      {/* ── Featured article ──────────────────────────────────── */}
      {featured && (
        <section className="mx-auto max-w-7xl px-6 pb-12">
          <div className={featured.heroImageUrl ? "grid gap-8 md:grid-cols-[1fr_1fr]" : ""}>
            {featured.heroImageUrl && (
              <Link to={`/news/${featured.slug}`} className="block">
                <picture>
                  {featured.heroImageWebpUrl && (
                    <source srcSet={featured.heroImageWebpUrl} type="image/webp" />
                  )}
                  <img
                    src={featured.heroImageUrl}
                    alt={featured.heroImageAlt ?? ""}
                    className="aspect-[4/3] w-full object-cover"
                  />
                </picture>
              </Link>
            )}
            <div className="flex flex-col justify-center">
              <span className="self-start"><Chip tone="accent" className="border-0 font-semibold">Featured</Chip></span>
              <Link to={`/news/${featured.slug}`}>
                <Headline as="h2" size="h2" className="mt-3">
                  {featured.title}
                </Headline>
              </Link>
              <p className="mt-2 font-mono text-xs uppercase tracking-wide text-muted">
                {formatDate(featured.calendarDate ?? featured.publishedAt)}
              </p>
              {featured.excerpt && (
                <p className="mt-4 text-fg-soft leading-relaxed">
                  {featured.excerpt}
                </p>
              )}
              <div className="mt-6">
                <Link
                  to={`/news/${featured.slug}`}
                  className="inline-flex items-center gap-2 bg-primary px-5 py-2.5 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
                >
                  Read on <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
                </Link>
              </div>
            </div>
          </div>
        </section>
      )}

      {/* ── Filter chips ──────────────────────────────────────── */}
      <div className="border-y border-border-soft bg-panel-alt">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-2 px-6 py-3">
          <span className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">Show</span>
          <button
            type="button"
            onClick={() => setSelectedCategory(null)}
            aria-pressed={selectedCategory === null}
            className={`inline-flex items-center border px-3 py-1 text-xs font-medium transition ${
              selectedCategory === null
                ? "border-primary bg-primary text-primary-foreground"
                : "border-border-soft bg-background text-fg-soft hover:bg-panel-alt"
            }`}
          >
            All
          </button>
          {allCategories.map((c) => (
            <button
              key={c}
              type="button"
              onClick={() => setSelectedCategory(c)}
              aria-pressed={selectedCategory === c}
              className={`inline-flex items-center border px-3 py-1 text-xs font-medium transition ${
                selectedCategory === c
                  ? "border-primary bg-primary text-primary-foreground"
                  : "border-border-soft bg-background text-fg-soft hover:bg-panel-alt"
              }`}
            >
              {c}
            </button>
          ))}
        </div>
      </div>

      {/* ── Article grid (2-col) ──────────────────────────────── */}
      <div className="mx-auto max-w-7xl px-6 py-10 md:py-14">
        {loading && accumulated.length === 0 && (
          <p className="text-muted">Loading…</p>
        )}
        {!loading && accumulated.length === 0 && (
          <p className="text-muted">No news to show yet.</p>
        )}

        <div className="grid gap-x-8 gap-y-12 md:grid-cols-2">
          {rest.map((n) => (
            <NewsCard key={n.id} item={n} />
          ))}
        </div>

        {data && data.totalPages > page && (
          <div className="mt-10 flex justify-center">
            <button
              type="button"
              onClick={() => setPage((p) => p + 1)}
              className="inline-flex items-center gap-2 border border-border-soft px-5 py-2.5 text-sm font-medium hover:bg-panel-alt"
            >
              Load more
              <ArrowDown aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

function NewsCard({ item }: { item: PublicNewsItem }) {
  return (
    <Link to={`/news/${item.slug}`} className="group block">
      {item.heroImageUrl && (
        <picture>
          {item.heroImageWebpUrl && (
            <source srcSet={item.heroImageWebpUrl} type="image/webp" />
          )}
          <img
            src={item.heroImageUrl}
            alt={item.heroImageAlt ?? ""}
            className="aspect-[4/3] w-full object-cover"
          />
        </picture>
      )}
      <div className={item.heroImageUrl ? "mt-3" : ""}>
        <h3 className="text-lg font-semibold leading-snug group-hover:underline">
          {item.title}
        </h3>
        {item.excerpt && (
          <p className="mt-2 text-sm text-fg-soft leading-relaxed line-clamp-3">
            {item.excerpt}
          </p>
        )}
        <p className="mt-2 font-mono text-[10px] uppercase tracking-wide text-muted">
          {formatDate(item.calendarDate ?? item.publishedAt)}
        </p>
      </div>
    </Link>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("en-US", {
    month: "short", day: "numeric", year: "numeric",
  });
}
