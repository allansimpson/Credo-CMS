import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { sermonSeriesApi, type PublicSermonSeries } from "@/lib/api/sermonSeries";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";

export function SermonSeriesListPublicPage() {
  const { settings } = useSiteSettings();
  const [items, setItems] = useState<PublicSermonSeries[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    sermonSeriesApi.listPublic()
      .then((d) => { if (!cancelled) setItems(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <SeoTags
        title={`Sermon Series · ${settings?.churchName ?? ""}`}
        description="Browse our sermon series." />
      <h1 className="text-3xl font-bold sm:text-4xl">Sermon Series</h1>

      {loading && <p className="mt-6 text-muted-foreground">Loading…</p>}
      {!loading && items.length === 0 && (
        <p className="mt-6 text-muted-foreground">No sermon series have been published yet.</p>
      )}

      <ul className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {items.map((s) => (
          <li key={s.id} className="border bg-card">
            <Link to={`/sermons/series/${s.slug}`} className="block">
              {s.bannerImageUrl ? (
                <picture>
                  {s.bannerImageWebpUrl && <source srcSet={s.bannerImageWebpUrl} type="image/webp" />}
                  <img src={s.bannerImageUrl} alt={s.bannerImageAlt ?? ""}
                    className="aspect-video w-full object-cover" />
                </picture>
              ) : (
                <div className="aspect-video w-full bg-muted" />
              )}
              <div className="p-4">
                <h2 className="font-semibold hover:underline">{s.title}</h2>
                <p className="mt-1 text-xs text-muted-foreground">
                  {s.startDate}{s.endDate ? ` – ${s.endDate}` : " – ongoing"}
                </p>
              </div>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
