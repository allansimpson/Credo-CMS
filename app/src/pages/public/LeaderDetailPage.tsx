import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { leadersApi } from "@/lib/api/leaders";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { ApiError } from "@/lib/apiClient";
import type { PublicLeader } from "@/types/api";
import { NotFoundPage } from "@/pages/NotFoundPage";

export function LeaderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { settings } = useSiteSettings();
  const [item, setItem] = useState<PublicLeader | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    leadersApi.getPublic(id)
      .then((l) => { if (!cancelled) { setItem(l); setLoading(false); } })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404) setNotFound(true);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [id]);

  if (loading) return <p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>;
  if (notFound || !item) return <NotFoundPage />;

  const orgName = settings?.churchName ?? null;
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "Person",
    name: item.fullName,
    jobTitle: item.title,
    image: item.photoUrl,
    affiliation: orgName ? { "@type": "Organization", name: orgName } : undefined,
  };

  return (
    <article className="mx-auto max-w-3xl px-4 py-8">
      <SeoTags
        title={`${item.fullName} · ${orgName ?? ""}`}
        description={item.title ?? `${item.fullName}, ${item.category}`}
        ogType="article"
        imageUrl={item.photoUrl}
        jsonLd={jsonLd}
      />

      <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-start">
        {item.photoUrl ? (
          <picture>
            {item.photoWebpUrl && <source srcSet={item.photoWebpUrl} type="image/webp" />}
            <img src={item.photoUrl} alt={item.photoAlt ?? item.fullName}
              className="h-40 w-40 rounded-full object-cover" />
          </picture>
        ) : (
          <div className="h-40 w-40 rounded-full bg-panel-alt" />
        )}
        <div>
          <h1 className="text-3xl font-bold sm:text-4xl">{item.fullName}</h1>
          {item.title && <p className="mt-1 text-lg text-muted">{item.title}</p>}
          <p className="mt-1 text-sm text-muted">{item.category}</p>
        </div>
      </div>

      {item.bioJson && (
        <div className="mt-8">
          <TipTapReadOnly json={item.bioJson} />
        </div>
      )}
    </article>
  );
}
