import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Clock, Lock, Pin } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { useAuth } from "@/hooks/useAuth";
import { publicBlogApi, type BlogPostListItem } from "@/lib/api/blog";
import type { PagedResult } from "@/types/api";

export function BlogListPage() {
  const { isAuthenticated } = useAuth();
  const [data, setData] = useState<PagedResult<BlogPostListItem> | null>(null);
  const [category, setCategory] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    publicBlogApi
      .list(category ?? undefined, 1, 24)
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load posts."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [category, isAuthenticated]);

  // Build category set from the loaded posts as a simple fallback (real
  // category list lives in SiteSettings — Q16 will surface it).
  const categories = Array.from(new Set((data?.items ?? []).map((p) => p.category))).sort();
  const featured = data?.items.find((p) => p.isPinned) ?? data?.items[0];
  const rest = (data?.items ?? []).filter((p) => p.id !== featured?.id);

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-10">
          <header className="border-b pb-6">
            <h1 className="text-3xl font-bold">Blog</h1>
            <p className="mt-1 text-sm text-muted">
              Devotionals, sermon notes, missions updates, and pastor's reflections.
            </p>
          </header>

          {categories.length > 1 && (
            <nav className="mt-6 flex flex-wrap gap-2" aria-label="Filter by category">
              <CategoryChip active={!category} label="All" onClick={() => setCategory(null)} />
              {categories.map((c) => (
                <CategoryChip key={c} active={category === c} label={c} onClick={() => setCategory(c)} />
              ))}
            </nav>
          )}

          {loading && <p className="mt-6 text-muted">Loading…</p>}
          {error && <p className="mt-6 text-danger">{error}</p>}

          {featured && (
            <FeaturedCard post={featured} />
          )}

          {rest.length > 0 && (
            <section className="mt-10">
              <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">More posts</h2>
              <ul className="mt-4 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
                {rest.map((p) => <PostCard key={p.id} post={p} />)}
              </ul>
            </section>
          )}

          {!loading && !error && (data?.items.length ?? 0) === 0 && (
            <p className="mt-6 text-muted">No posts yet.</p>
          )}
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function FeaturedCard({ post }: { post: BlogPostListItem }) {
  return (
    <article className="mt-8 overflow-hidden rounded-lg border bg-card md:grid md:grid-cols-[1.4fr_1fr]">
      {post.heroImageBlobUrl ? (
        <picture>
          {post.heroImageWebpBlobUrl && <source srcSet={post.heroImageWebpBlobUrl} type="image/webp" />}
          <img
            src={post.heroImageBlobUrl}
            alt={post.heroImageAltText ?? ""}
            className="h-72 w-full object-cover md:h-full"
          />
        </picture>
      ) : (
        <div aria-hidden className="h-56 w-full bg-panel-alt md:h-full" />
      )}
      <div className="flex flex-col p-6">
        <p className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.16em] text-accent">
          {post.isPinned && <Pin className="h-3 w-3" />}
          {post.category}
          {post.isMembersOnly && <Lock className="h-3 w-3 text-muted" aria-label="Members only" />}
        </p>
        <h2 className="mt-2 font-heading text-2xl font-semibold leading-tight">
          <Link to={`/blog/${post.slug}`} className="hover:underline">{post.title}</Link>
        </h2>
        {post.excerpt && <p className="mt-3 text-sm text-fg-soft">{post.excerpt}</p>}
        <p className="mt-auto pt-4 text-xs text-muted">
          {post.authorDisplayName}
          {post.publishedAt && ` · ${new Date(post.publishedAt).toLocaleDateString()}`}
          {" · "}
          <Clock className="inline h-3 w-3" /> {post.readingTimeMinutes} min read
        </p>
      </div>
    </article>
  );
}

function PostCard({ post }: { post: BlogPostListItem }) {
  return (
    <li>
      <Link
        to={`/blog/${post.slug}`}
        className="flex h-full flex-col overflow-hidden rounded-lg border bg-card transition-colors hover:bg-panel-alt"
      >
        {post.heroImageBlobUrl ? (
          <picture>
            {post.heroImageWebpBlobUrl && <source srcSet={post.heroImageWebpBlobUrl} type="image/webp" />}
            <img
              src={post.heroImageBlobUrl}
              alt={post.heroImageAltText ?? ""}
              className="h-44 w-full object-cover"
            />
          </picture>
        ) : (
          <div aria-hidden className="h-44 w-full bg-panel-alt" />
        )}
        <div className="flex flex-1 flex-col p-4">
          <p className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-[0.16em] text-accent">
            {post.category}
            {post.isMembersOnly && <Lock className="h-3 w-3 text-muted" aria-label="Members only" />}
          </p>
          <h3 className="mt-2 font-heading text-base font-semibold leading-snug">{post.title}</h3>
          {post.excerpt && (
            <p className="mt-2 line-clamp-3 text-xs text-fg-soft">{post.excerpt}</p>
          )}
          <p className="mt-auto pt-3 text-xs text-muted">
            {post.authorDisplayName}
            {post.publishedAt && ` · ${new Date(post.publishedAt).toLocaleDateString()}`}
          </p>
        </div>
      </Link>
    </li>
  );
}

function CategoryChip({
  active, label, onClick,
}: { active: boolean; label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={
        "h-8 px-3 text-xs font-medium transition-colors " +
        (active
          ? "bg-accent text-accent-foreground"
          : "border border-border bg-card text-fg-soft hover:bg-panel-alt")
      }
    >
      {label}
    </button>
  );
}
