import { lazy, Suspense, useEffect, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { Download } from "lucide-react";
import { publicSermonsApi } from "@/lib/api/publicSermons";
import type { PublicSermon, SermonListItem } from "@/lib/api/sermons";
import { publicDocumentFileUrl } from "@/lib/api/documents";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { formatScriptureReference } from "@/lib/bible/scripture";
import { getBookInfo } from "@/lib/bible/books";
import { ApiError } from "@/lib/apiClient";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { BigNum, BtnLink, Chip, Eyebrow, Headline } from "@/components/public";

const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly }))
);

export function SermonDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const [params] = useSearchParams();
  const highlight = params.get("highlight");
  const { settings } = useSiteSettings();
  const [sermon, setSermon] = useState<PublicSermon | null>(null);
  const [siblings, setSiblings] = useState<SermonListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [showTranscript, setShowTranscript] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    publicSermonsApi.get(slug)
      .then((s) => {
        if (cancelled) return;
        setSermon(s);
        setLoading(false);
        if (s.sermonSeriesId) {
          publicSermonsApi.list({ sermonSeriesId: s.sermonSeriesId })
            .then((res) => { if (!cancelled) setSiblings(res.items); })
            .catch(() => {});
        }
      })
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
  const passageText = sermon.scriptureReferences.length > 0
    ? sermon.scriptureReferences.map(formatScriptureReference).join(" · ")
    : null;
  const description = passageText
    ?? `Sermon by ${sermon.speakerName ?? "the church"}`;
  const publishDate = new Date(sermon.publishedAt);
  const formattedDate = publishDate.toLocaleDateString("en-US", {
    month: "short", day: "numeric", year: "numeric",
  });
  const durationMin = sermon.durationSeconds
    ? `${Math.round(sermon.durationSeconds / 60)} min`
    : null;

  const seriesIndex = siblings.length > 0
    ? [...siblings].sort((a, b) => new Date(a.publishedAt).getTime() - new Date(b.publishedAt).getTime())
        .findIndex((s) => s.slug === sermon.slug) + 1
    : null;
  const sortedSiblings = [...siblings].sort(
    (a, b) => new Date(a.publishedAt).getTime() - new Date(b.publishedAt).getTime()
  );

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
    <article>
      <SeoTags
        title={sermon.title}
        description={description}
        ogType="article"
        imageUrl={sermon.thumbnailBlobUrl}
        jsonLd={jsonLd}
      />

      {/* ── Breadcrumb ────────────────────────────────────────────── */}
      <nav className="bg-inset px-6 py-3 text-[11px] font-medium uppercase tracking-[0.14em] text-inset-foreground/70">
        <div className="mx-auto flex max-w-7xl gap-2">
          <Link to="/sermons" className="hover:text-inset-foreground">Sermons</Link>
          {sermon.sermonSeriesSlug && sermon.sermonSeriesTitle && (
            <>
              <span aria-hidden>/</span>
              <Link to={`/sermons/series/${sermon.sermonSeriesSlug}`} className="hover:text-inset-foreground">
                {sermon.sermonSeriesTitle}
              </Link>
            </>
          )}
          {seriesIndex && (
            <>
              <span aria-hidden>/</span>
              <span className="text-accent">Part {seriesIndex}</span>
            </>
          )}
        </div>
      </nav>

      {/* ── Header ────────────────────────────────────────────────── */}
      <header className="mx-auto max-w-7xl px-6 py-10 md:py-14">
        {sermon.sermonSeriesTitle && (
          <Eyebrow accent>
            {sermon.sermonSeriesTitle}
            {seriesIndex ? ` · Part ${seriesIndex}` : ""}
          </Eyebrow>
        )}
        <Headline as="h1" size="display" className="mt-3">
          {sermon.title}
        </Headline>

        {/* Metadata row */}
        <div className="mt-6 flex flex-wrap gap-x-10 gap-y-3 text-sm">
          {passageText && (
            <div>
              <p className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">Passage</p>
              <p className="mt-0.5 font-semibold">{passageText}</p>
            </div>
          )}
          {sermon.speakerName && (
            <div>
              <p className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">Speaker</p>
              <p className="mt-0.5 font-semibold">{sermon.speakerName}</p>
            </div>
          )}
          <div>
            <p className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">Date</p>
            <p className="mt-0.5 font-semibold">{formattedDate}</p>
          </div>
          {durationMin && (
            <div>
              <p className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">Length</p>
              <p className="mt-0.5 font-semibold">{durationMin}</p>
            </div>
          )}
        </div>
      </header>

      {/* ── Video embed ───────────────────────────────────────────── */}
      <section className="mx-auto max-w-7xl px-6">
        <div className="aspect-video w-full overflow-hidden bg-black">
          <iframe
            title={sermon.title}
            src={`https://www.youtube.com/embed/${sermon.youTubeVideoId}`}
            className="h-full w-full"
            allow="encrypted-media; picture-in-picture"
            allowFullScreen
            referrerPolicy="strict-origin-when-cross-origin"
          />
        </div>
      </section>

      {/* ── Description + resources ───────────────────────────────── */}
      <div className="mx-auto max-w-7xl px-6 py-10 md:py-14">
        <div className="grid gap-10 md:grid-cols-[1fr_1fr]">
          {/* Left: scripture panel / description */}
          <div>
            {sermon.descriptionJson && (
              <div className="border-l-2 border-accent bg-panel-alt p-5">
                {passageText && (
                  <p className="mb-3 text-[11px] font-medium uppercase tracking-[0.14em] text-accent">
                    {passageText}
                  </p>
                )}
                <div className="text-sm leading-relaxed text-fg-soft">
                  <Suspense fallback={null}>
                    <TipTapReadOnly json={sermon.descriptionJson} />
                  </Suspense>
                </div>
              </div>
            )}
          </div>

          {/* Right: tags + attachments */}
          <div className="space-y-6">
            {sermon.tags.length > 0 && (
              <ul className="flex flex-wrap gap-2">
                {sermon.tags.map((t) => (
                  <li key={t.id}>
                    <Link to={`/sermons?tag=${t.slug}`}>
                      <Chip tone="muted">{t.name}</Chip>
                    </Link>
                  </li>
                ))}
              </ul>
            )}

            {sermon.attachments.length > 0 && (
              <div className="flex flex-wrap gap-3">
                {sermon.attachments.map((a) => (
                  <a
                    key={a.documentId}
                    href={publicDocumentFileUrl(a.documentId)}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center gap-1.5 border border-border-soft px-4 py-2 text-sm font-medium hover:bg-panel-alt"
                  >
                    <Download aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
                    {a.title}
                  </a>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* ── Transcript ────────────────────────────────────────────── */}
      {sermon.transcript && (
        <section className="mx-auto max-w-7xl border-t border-border-soft px-6 py-8">
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

      {/* ── Series navigation ─────────────────────────────────────── */}
      {sortedSiblings.length > 1 && sermon.sermonSeriesTitle && (
        <section className="border-t border-border-soft">
          <div className="mx-auto max-w-7xl px-6 py-10 md:py-14">
            <Eyebrow accent>The series · {sermon.sermonSeriesTitle}</Eyebrow>

            <div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
              {sortedSiblings.map((s, i) => {
                const isCurrent = s.slug === sermon.slug;
                return (
                  <Link
                    key={s.id}
                    to={`/sermons/${s.slug}`}
                    className={[
                      "block border p-4 text-sm transition-colors",
                      isCurrent
                        ? "border-accent bg-accent text-white"
                        : "border-border-soft hover:bg-panel-alt",
                    ].join(" ")}
                  >
                    <p className={[
                      "text-[10px] font-medium uppercase tracking-[0.14em]",
                      isCurrent ? "text-white/70" : "text-muted",
                    ].join(" ")}>
                      {isCurrent ? <Chip tone="accent">Part {String(i + 1).padStart(2, "0")}</Chip> : `Part ${String(i + 1).padStart(2, "0")}`}
                    </p>
                    <p className={["mt-1 font-semibold leading-snug", isCurrent ? "text-white" : ""].join(" ")}>
                      {s.title}
                    </p>
                  </Link>
                );
              })}
            </div>
          </div>
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
