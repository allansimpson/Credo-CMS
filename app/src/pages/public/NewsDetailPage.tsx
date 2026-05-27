import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { newsApi } from "@/lib/api/news";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { PublicNewsDetail } from "@/types/api";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { ApiError } from "@/lib/apiClient";

export function NewsDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const { settings } = useSiteSettings();
  const [item, setItem] = useState<PublicNewsDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setLoading(true);
    setNotFound(false);
    newsApi.getPublic(slug)
      .then((n) => { if (!cancelled) { setItem(n); setLoading(false); } })
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
    <article className="mx-auto max-w-3xl px-4 py-8">
      <SeoTags
        title={item.title}
        description={description}
        ogType="article"
        imageUrl={item.heroImageUrl}
        jsonLd={jsonLd}
      />

      {item.heroImageUrl && (
        <picture>
          {item.heroImageWebpUrl && <source srcSet={item.heroImageWebpUrl} type="image/webp" />}
          <img src={item.heroImageUrl} alt={item.heroImageAlt ?? ""}
            className="mb-6 w-full rounded-lg object-cover" style={{ maxHeight: 480 }} />
        </picture>
      )}

      <h1 className="text-3xl font-bold sm:text-4xl">{item.title}</h1>
      <p className="mt-2 text-sm text-muted">
        Published {new Date(item.publishedAt).toLocaleDateString()}
        {item.calendarDate && ` · Date ${new Date(item.calendarDate).toLocaleDateString()}`}
      </p>
      {item.excerpt && <p className="mt-3 text-lg text-muted">{item.excerpt}</p>}

      <div className="mt-8">
        <TipTapReadOnly json={item.bodyJson} />
      </div>
    </article>
  );
}
