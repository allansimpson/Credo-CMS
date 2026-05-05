import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { pagesApi } from "@/lib/api/pages";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { PublicPage } from "@/types/api";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { ApiError } from "@/lib/apiClient";

export function DynamicPagePage() {
  const { slug } = useParams<{ slug: string }>();
  const { settings } = useSiteSettings();
  const [page, setPage] = useState<PublicPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setLoading(true);
    setNotFound(false);
    pagesApi.getPublic(slug)
      .then((p) => { if (!cancelled) { setPage(p); setLoading(false); } })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404) {
          setNotFound(true);
        }
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [slug]);

  if (loading) return <p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>;
  if (notFound || !page) return <NotFoundPage />;

  const description =
    page.metaDescription
    ?? page.excerpt
    ?? settings?.churchName
    ?? null;
  const orgName = settings?.churchName ?? null;
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "Article",
    headline: page.title,
    datePublished: page.publishedAt,
    description,
    publisher: orgName ? { "@type": "Organization", name: orgName } : undefined,
    image: page.heroImageUrl ? [page.heroImageUrl] : undefined,
  };

  return (
    <article className="mx-auto max-w-3xl px-4 py-8">
      <SeoTags
        title={page.title}
        description={description}
        ogType="article"
        imageUrl={page.heroImageUrl}
        jsonLd={jsonLd}
      />

      {page.heroImageUrl && (
        <picture>
          {page.heroImageWebpUrl && <source srcSet={page.heroImageWebpUrl} type="image/webp" />}
          <img
            src={page.heroImageUrl}
            alt={page.heroImageAlt ?? ""}
            className="mb-6 w-full rounded-lg object-cover"
            style={{ maxHeight: 480 }}
          />
        </picture>
      )}

      <h1 className="text-3xl font-bold sm:text-4xl">{page.title}</h1>
      {page.excerpt && <p className="mt-3 text-lg text-muted-foreground">{page.excerpt}</p>}

      <div className="mt-8">
        <TipTapReadOnly json={page.bodyJson} />
      </div>
    </article>
  );
}
