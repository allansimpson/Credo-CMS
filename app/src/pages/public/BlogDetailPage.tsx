import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Clock, Lock } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { useAuth } from "@/hooks/useAuth";
import { publicBlogApi, type BlogPostDetail } from "@/lib/api/blog";

export function BlogDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const { isAuthenticated } = useAuth();
  const [post, setPost] = useState<BlogPostDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setLoading(true); setNotFound(false);
    publicBlogApi.get(slug)
      .then((d) => { if (!cancelled) setPost(d); })
      .catch((err) => {
        if (cancelled) return;
        if ((err as { status?: number }).status === 404) setNotFound(true);
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [slug, isAuthenticated]);

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-10">
          <Link
            to="/blog"
            className="mb-6 inline-flex items-center gap-1 text-sm text-muted hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" /> All posts
          </Link>

          {loading && <p className="text-muted">Loading…</p>}
          {!loading && notFound && (
            <div className="rounded-lg border bg-card p-6">
              <h1 className="text-xl font-bold">Post not found</h1>
              <p className="mt-2 text-sm text-muted">It may have moved or be members-only.</p>
              {!isAuthenticated && (
                <Link
                  to={`/login?return=${encodeURIComponent(`/blog/${slug}`)}`}
                  className="mt-4 inline-flex h-10 items-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
                >
                  Sign in
                </Link>
              )}
            </div>
          )}
          {!loading && post && (
            <article className="space-y-6">
              {post.heroImageBlobUrl && (
                <picture>
                  {post.heroImageWebpBlobUrl && <source srcSet={post.heroImageWebpBlobUrl} type="image/webp" />}
                  <img src={post.heroImageBlobUrl} alt={post.heroImageAltText ?? ""} className="h-72 w-full object-cover" />
                </picture>
              )}
              <header className="border-b pb-6">
                <p className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.16em] text-accent">
                  {post.category}
                  {post.isMembersOnly && <Lock className="h-3 w-3 text-muted" aria-label="Members only" />}
                </p>
                <h1 className="mt-2 text-3xl font-bold leading-tight">{post.title}</h1>
                <p className="mt-3 text-sm text-muted">
                  {post.authorDisplayName}
                  {post.publishedAt && ` · ${new Date(post.publishedAt).toLocaleDateString()}`}
                  {" · "}
                  <Clock className="inline h-3 w-3" /> {post.readingTimeMinutes} min read
                </p>
              </header>
              <section className="prose prose-sm max-w-none">
                <TipTapReadOnly valueJson={post.bodyJson} />
              </section>
              {post.tags.length > 0 && (
                <footer className="flex flex-wrap items-center gap-2 border-t pt-4">
                  <span className="text-xs uppercase tracking-wide text-muted">Tags:</span>
                  {post.tags.map((t) => (
                    <span key={t} className="rounded bg-panel-alt px-2 py-0.5 text-[11px] uppercase tracking-wider text-fg-soft">
                      {t}
                    </span>
                  ))}
                </footer>
              )}
            </article>
          )}
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}
