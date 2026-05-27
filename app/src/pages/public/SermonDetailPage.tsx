import { lazy, Suspense, useEffect, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { publicSermonsApi } from "@/lib/api/publicSermons";
import type { PublicSermon } from "@/lib/api/sermons";
import { publicDocumentFileUrl } from "@/lib/api/documents";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { formatScriptureReference } from "@/lib/bible/scripture";
import { getBookInfo } from "@/lib/bible/books";
import { ApiError } from "@/lib/apiClient";
import { NotFoundPage } from "@/pages/NotFoundPage";

const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly }))
);

export function SermonDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const [params] = useSearchParams();
  const highlight = params.get("highlight");
  const { settings } = useSiteSettings();
  const [sermon, setSermon] = useState<PublicSermon | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [showTranscript, setShowTranscript] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    publicSermonsApi.get(slug)
      .then((s) => { if (!cancelled) { setSermon(s); setLoading(false); } })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404) setNotFound(true);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [slug]);

  if (loading) return <p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>;
  if (notFound || !sermon) return <NotFoundPage />;

  const orgName = settings?.churchName ?? null;
  const description = sermon.scriptureReferences.length > 0
    ? sermon.scriptureReferences.map(formatScriptureReference).join(" · ")
    : `Sermon by ${sermon.speakerName ?? "the church"}`;

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "VideoObject",
    name: sermon.title,
    description,
    thumbnailUrl: sermon.thumbnailBlobUrl ? [sermon.thumbnailBlobUrl] : undefined,
    uploadDate: sermon.publishedAt,
    duration: sermon.durationSeconds ? `PT${sermon.durationSeconds}S` : undefined,
    embedUrl: `https://www.youtube.com/embed/${sermon.youTubeVideoId}`,
    publisher: orgName ? { "@type": "Organization", name: orgName } : undefined,
  };

  return (
    <article className="mx-auto max-w-4xl px-4 py-8">
      <SeoTags
        title={sermon.title}
        description={description}
        ogType="article"
        imageUrl={sermon.thumbnailBlobUrl}
        jsonLd={jsonLd}
      />

      <div className="aspect-video w-full bg-black">
        <iframe
          title={sermon.title}
          src={`https://www.youtube.com/embed/${sermon.youTubeVideoId}`}
          className="h-full w-full"
          allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
          allowFullScreen
        />
      </div>

      <h1 className="mt-6 text-3xl font-bold sm:text-4xl">{sermon.title}</h1>
      <p className="mt-2 text-sm text-muted">
        {sermon.speakerLeaderId ? (
          <Link to={`/leaders/${sermon.speakerLeaderId}`} className="text-primary hover:underline">
            {sermon.speakerName}
          </Link>
        ) : sermon.speakerName ? (
          <span>{sermon.speakerName}</span>
        ) : null}
        {sermon.speakerName && " · "}
        {new Date(sermon.publishedAt).toLocaleDateString()}
      </p>

      {sermon.sermonSeriesSlug && sermon.sermonSeriesTitle && (
        <p className="mt-2 text-sm">
          Part of <Link to={`/sermons/series/${sermon.sermonSeriesSlug}`}
            className="text-primary hover:underline">{sermon.sermonSeriesTitle}</Link>
        </p>
      )}

      {sermon.scriptureReferences.length > 0 && (
        <p className="mt-2 text-sm text-muted">
          {sermon.scriptureReferences.map((r, i) => {
            const info = getBookInfo(r.book);
            const linkPath = info ? `/sermons/by-book/${info.slug}` : "#";
            return (
              <span key={i}>
                {i > 0 && " · "}
                <Link to={linkPath} className="text-primary hover:underline">
                  {formatScriptureReference(r)}
                </Link>
              </span>
            );
          })}
        </p>
      )}

      {sermon.tags.length > 0 && (
        <ul className="mt-3 flex flex-wrap gap-2">
          {sermon.tags.map((t) => (
            <li key={t.id}>
              <Link to={`/sermons?tag=${t.slug}`}
                className="border bg-panel-alt px-2 py-0.5 text-xs text-muted hover:text-foreground">
                {t.name}
              </Link>
            </li>
          ))}
        </ul>
      )}

      {sermon.descriptionJson && (
        <div className="mt-8">
          <Suspense fallback={null}>
            <TipTapReadOnly json={sermon.descriptionJson} />
          </Suspense>
        </div>
      )}

      {sermon.attachments.length > 0 && (
        <section className="mt-8">
          <h2 className="text-xl font-semibold">Resources</h2>
          <ul className="mt-2 space-y-1">
            {sermon.attachments.map((a) => (
              <li key={a.documentId}>
                <a href={publicDocumentFileUrl(a.documentId)} target="_blank" rel="noreferrer"
                  className="text-sm text-primary hover:underline">📄 {a.title}</a>
              </li>
            ))}
          </ul>
        </section>
      )}

      {sermon.transcript && (
        <section className="mt-8">
          <button type="button" onClick={() => setShowTranscript((v) => !v)}
            className="text-sm font-semibold text-primary hover:underline">
            {showTranscript ? "▼ Hide transcript" : "▶ Show transcript"}
          </button>
          {showTranscript && (
            <div className="mt-3 max-h-96 overflow-y-auto border bg-panel-alt p-4 text-sm leading-relaxed">
              {highlight ? (
                <HighlightedText text={sermon.transcript} term={highlight} />
              ) : (
                <p>{sermon.transcript}</p>
              )}
            </div>
          )}
        </section>
      )}
    </article>
  );
}

function HighlightedText({ text, term }: { text: string; term: string }) {
  const parts = text.split(new RegExp(`(${escapeRegex(term)})`, "ig"));
  return (
    <p>
      {parts.map((p, i) =>
        p.toLowerCase() === term.toLowerCase()
          ? <mark key={i}>{p}</mark>
          : <span key={i}>{p}</span>
      )}
    </p>
  );
}

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
