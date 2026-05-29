import { Suspense, useEffect, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { Eye, Lock, Pencil } from "lucide-react";
import { pagesApi } from "@/lib/api/pages";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { useAuth } from "@/hooks/useAuth";
import { TEMPLATE_COMPONENTS } from "@/components/templates";
import type { PublicPage } from "@/types/api";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { ApiError } from "@/lib/apiClient";

export function DynamicPagePage() {
  const { slug } = useParams<{ slug: string }>();
  const [searchParams] = useSearchParams();
  const { hasAnyRole } = useAuth();
  const { settings } = useSiteSettings();
  const [page, setPage] = useState<PublicPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  // Preview mode: admin opened "/<slug>?preview=1" from the editor. Hits the
  // admin-only endpoint that bypasses the published / members-only filter.
  const previewRequested = searchParams.get("preview") === "1";
  const isAdmin = hasAnyRole(["Administrator", "Editor"]);
  const usePreview = previewRequested && isAdmin;

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setLoading(true);
    setNotFound(false);
    const fetcher = usePreview ? pagesApi.getPreview(slug) : pagesApi.getPublic(slug);
    fetcher
      .then((p) => { if (!cancelled) { setPage(p); setLoading(false); } })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404) {
          setNotFound(true);
        }
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [slug, usePreview]);

  if (loading) return <p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>;
  if (notFound || !page) return <NotFoundPage />;

  const previewBanner = usePreview ? <PreviewBanner page={page} /> : null;

  // Dispatch to a custom template if one is registered.
  const TemplateComponent = page.template && page.template !== "Standard"
    ? TEMPLATE_COMPONENTS[page.template]
    : undefined;

  if (TemplateComponent) {
    return (
      <>
        {previewBanner}
        <Suspense fallback={<p className="mx-auto max-w-3xl p-8 text-muted">Loading…</p>}>
          <TemplateComponent page={page} />
        </Suspense>
      </>
    );
  }

  // Standard template — default rich-text rendering.
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
    <>
      {previewBanner}
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
      {page.excerpt && <p className="mt-3 text-lg text-muted">{page.excerpt}</p>}

      <div className="mt-8">
        <TipTapReadOnly json={page.bodyJson} />
      </div>
      </article>
    </>
  );
}

/* ── Preview banner ──────────────────────────────────────────────────────
 * Sticky strip at the very top of the page when an admin is previewing
 * draft content. Heavy contrast + diagonal-stripe edge + jump-back-to-
 * editor link, so there's no mistaking this for the live experience. */

function PreviewBanner({ page }: { page: PublicPage }) {
  // Stick the banner directly beneath the public-site header. PublicHeader
  // publishes its rendered height to `--public-header-offset` on
  // documentElement, so we consume it via Tailwind's arbitrary-value syntax
  // and stay correct across announcement-bar wrap, viewport resize, etc.
  return (
    <div
      role="status"
      aria-label="Admin preview"
      className="sticky top-[var(--public-header-offset,0px)] z-30 border-b-2 border-primary-foreground/40 bg-primary text-primary-foreground shadow-md"
    >
      <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-x-4 gap-y-2 px-4 py-2.5 sm:px-6">
        <span className="inline-flex items-center gap-2">
          <Eye className="h-4 w-4" />
          <span className="font-mono text-[11px] font-bold uppercase tracking-[0.22em]">
            Preview
          </span>
        </span>
        <span className="hidden text-[11px] font-medium text-primary-foreground/80 sm:inline">
          Admins only · visitors will not see this view
        </span>
        {page.isMembersOnly && (
          <span className="inline-flex items-center gap-1.5 border border-primary-foreground/40 bg-primary-foreground/10 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.14em]">
            <Lock className="h-3 w-3" />
            Members only
          </span>
        )}
        <Link
          to={`/admin/pages/${page.id}`}
          data-allow-nav="true"
          className="ml-auto inline-flex items-center gap-1.5 border border-foreground bg-foreground px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-background hover:bg-foreground/85"
        >
          <Pencil className="h-3 w-3" />
          Back to editor
        </Link>
      </div>
    </div>
  );
}
