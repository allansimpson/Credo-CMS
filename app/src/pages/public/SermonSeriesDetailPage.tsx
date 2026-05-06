import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { sermonSeriesApi, type PublicSermonSeries } from "@/lib/api/sermonSeries";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { SeoTags } from "@/components/shared/SeoTags";
import { formatScriptureReference } from "@/lib/bible/scripture";
import { ApiError } from "@/lib/apiClient";
import { NotFoundPage } from "@/pages/NotFoundPage";

export function SermonSeriesDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const [series, setSeries] = useState<PublicSermonSeries | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    sermonSeriesApi.getPublic(slug)
      .then((s) => { if (!cancelled) { setSeries(s); setLoading(false); } })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404) setNotFound(true);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [slug]);

  if (loading) return <p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>;
  if (notFound || !series) return <NotFoundPage />;

  return (
    <article className="mx-auto max-w-3xl px-4 py-8">
      <SeoTags
        title={series.title}
        description={`Sermon series: ${series.title}`}
        ogType="article"
        imageUrl={series.bannerImageUrl}
      />

      {series.bannerImageUrl && (
        <picture>
          {series.bannerImageWebpUrl && <source srcSet={series.bannerImageWebpUrl} type="image/webp" />}
          <img src={series.bannerImageUrl} alt={series.bannerImageAlt ?? ""}
            className="mb-6 w-full object-cover" style={{ maxHeight: 480 }} />
        </picture>
      )}

      <h1 className="text-3xl font-bold sm:text-4xl">{series.title}</h1>
      <p className="mt-2 text-sm text-muted">
        {series.startDate}{series.endDate ? ` – ${series.endDate}` : " – ongoing"}
      </p>

      {series.scriptureReferences.length > 0 && (
        <p className="mt-3 text-sm text-muted">
          {series.scriptureReferences.map((r) => formatScriptureReference(r)).join(" · ")}
        </p>
      )}

      {series.descriptionJson && (
        <div className="mt-8">
          <TipTapReadOnly json={series.descriptionJson} />
        </div>
      )}

      <p className="mt-8 text-sm text-muted">
        Sermons in this series will appear here once Stage Q4 lands.
      </p>
    </article>
  );
}
