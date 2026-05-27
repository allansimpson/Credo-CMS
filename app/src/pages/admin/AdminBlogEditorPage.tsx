import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  adminBlogApi,
  type BlogPostDetail,
  type CreateBlogPostRequest,
  type UpdateBlogPostRequest,
} from "@/lib/api/blog";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapFullEditor } from "@/components/shared/TipTapFullEditor";
import { TagAutocomplete } from "@/components/shared/TagAutocomplete";
import { slugify } from "@/lib/slug";
import {
  Btn,
  PageHeader,
  SectionHead,
  SwitchFlat,
} from "@/components/shared/admin/EditorialPrimitives";

interface FormState {
  slug: string;
  title: string;
  bodyJson: string | null;
  excerpt: string;
  heroImageBlobUrl: string | null;
  heroImageWebpBlobUrl: string | null;
  heroImageAltText: string | null;
  category: string;
  isPublished: boolean;
  isMembersOnly: boolean;
  isPinned: boolean;
  publishedAt: string;
  scheduledPublishAt: string;
  metaDescription: string;
  tags: string[];
}

const emptyForm: FormState = {
  slug: "",
  title: "",
  bodyJson: null,
  excerpt: "",
  heroImageBlobUrl: null,
  heroImageWebpBlobUrl: null,
  heroImageAltText: null,
  category: "",
  isPublished: false,
  isMembersOnly: false,
  isPinned: false,
  publishedAt: "",
  scheduledPublishAt: "",
  metaDescription: "",
  tags: [],
};

export function AdminBlogEditorPage() {
  const { id } = useParams<{ id?: string }>();
  const isNew = !id || id === "new";
  const navigate = useNavigate();

  const [form, setForm] = useState<FormState>(emptyForm);
  const [original, setOriginal] = useState<BlogPostDetail | null>(null);
  const [loading, setLoading] = useState(!isNew);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [slugAutoGen, setSlugAutoGen] = useState(isNew);

  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    adminBlogApi.get(id!)
      .then((p) => {
        if (cancelled) return;
        setOriginal(p);
        setForm({
          slug: p.slug,
          title: p.title,
          bodyJson: p.bodyJson,
          excerpt: p.excerpt ?? "",
          heroImageBlobUrl: p.heroImageBlobUrl,
          heroImageWebpBlobUrl: p.heroImageWebpBlobUrl,
          heroImageAltText: p.heroImageAltText,
          category: p.category,
          isPublished: p.isPublished,
          isMembersOnly: p.isMembersOnly,
          isPinned: p.isPinned,
          publishedAt: p.publishedAt ? p.publishedAt.slice(0, 16) : "",
          scheduledPublishAt: p.scheduledPublishAt ? p.scheduledPublishAt.slice(0, 16) : "",
          metaDescription: p.metaDescription ?? "",
          tags: p.tags,
        });
        setSlugAutoGen(false);
        setLoading(false);
      })
      .catch(() => {
        if (cancelled) return;
        setErrors(["Could not load post."]);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [id, isNew]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true); setErrors([]); setSuccess(false);

    const body: CreateBlogPostRequest | UpdateBlogPostRequest = {
      slug: form.slug,
      title: form.title,
      bodyJson: form.bodyJson ?? "",
      excerpt: form.excerpt || null,
      heroImageBlobUrl: form.heroImageBlobUrl,
      heroImageWebpBlobUrl: form.heroImageWebpBlobUrl,
      heroImageAltText: form.heroImageAltText,
      category: form.category,
      relatedSermonId: null,
      isPublished: form.isPublished,
      isMembersOnly: form.isMembersOnly,
      isPinned: form.isPinned,
      publishedAt: form.publishedAt ? new Date(form.publishedAt).toISOString() : null,
      scheduledPublishAt: form.scheduledPublishAt ? new Date(form.scheduledPublishAt).toISOString() : null,
      metaDescription: form.metaDescription || null,
      tags: form.tags,
    };

    try {
      if (isNew) {
        const created = await adminBlogApi.create(body);
        navigate(`/admin/blog/${created.id}`);
      } else {
        const updated = await adminBlogApi.update(id!, body);
        setOriginal(updated);
        setSuccess(true);
      }
    } catch (err) {
      const messages = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Save failed."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!id) return;
    if (!window.confirm("Soft-delete this post?")) return;
    await adminBlogApi.softDelete(id);
    navigate("/admin/blog");
  };

  if (loading) return <p className="text-muted">Loading…</p>;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={isNew ? "New post" : `Editing · /blog/${form.slug}`}
        title={form.title || "Untitled post"}
        actions={
          !isNew && original && (
            <Btn variant="danger" onClick={handleDelete}>Delete</Btn>
          )
        }
      />

      <form onSubmit={handleSubmit} className="space-y-6">
        {errors.length > 0 && (
          <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
            <ul className="list-disc pl-5">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
          </div>
        )}
        {success && (
          <div role="status" className="border border-success/30 bg-success/10 p-3 text-sm text-success">
            Saved.
          </div>
        )}

        <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
          <div className="space-y-6">
            <section className="space-y-4">
              <SectionHead number="01" title="Identity" />
              <Field label="Title" required>
                <input
                  required
                  value={form.title}
                  onChange={(e) => {
                    setForm({
                      ...form,
                      title: e.target.value,
                      slug: slugAutoGen ? slugify(e.target.value) : form.slug,
                    });
                  }}
                  className="input"
                />
              </Field>
              <Field
                label="Slug"
                required
                hint={slugAutoGen ? "Auto-generating from title." : "Editing manually."}
              >
                <input
                  required
                  value={form.slug}
                  onChange={(e) => { setSlugAutoGen(false); setForm({ ...form, slug: e.target.value }); }}
                  className="input"
                />
              </Field>
              <Field label="Category" required>
                <input
                  required
                  value={form.category}
                  maxLength={100}
                  onChange={(e) => setForm({ ...form, category: e.target.value })}
                  className="input"
                  placeholder="Devotional, Sermon Notes, Missions, Pastor's Reflections, Announcements"
                />
              </Field>
            </section>

            <section className="space-y-4">
              <SectionHead number="02" title="Hero image" />
              <ImageUpload
                ariaLabel="Blog hero image"
                value={{
                  url: form.heroImageBlobUrl,
                  webpUrl: form.heroImageWebpBlobUrl,
                  alt: form.heroImageAltText,
                }}
                onChange={(next) => setForm({
                  ...form,
                  heroImageBlobUrl: next.url,
                  heroImageWebpBlobUrl: next.webpUrl,
                  heroImageAltText: next.alt,
                })}
              />
            </section>

            <section className="space-y-4">
              <SectionHead number="03" title="Body" />
              <TipTapFullEditor
                ariaLabel="Blog body"
                valueJson={form.bodyJson}
                onChangeJson={(json) => setForm({ ...form, bodyJson: json })}
                placeholder="Write the post body…"
              />
            </section>

            <section className="space-y-4">
              <SectionHead number="04" title="SEO & summary" />
              <Field label="Excerpt" hint="Optional. Auto-derived from body if blank.">
                <textarea
                  value={form.excerpt}
                  maxLength={500}
                  onChange={(e) => setForm({ ...form, excerpt: e.target.value })}
                  className="min-h-20 w-full border border-border bg-background p-2 text-sm focus-visible:border-accent focus-visible:outline-none"
                />
              </Field>
              <Field label="Meta description" hint="Up to 300 chars.">
                <textarea
                  value={form.metaDescription}
                  maxLength={300}
                  onChange={(e) => setForm({ ...form, metaDescription: e.target.value })}
                  className="min-h-16 w-full border border-border bg-background p-2 text-sm focus-visible:border-accent focus-visible:outline-none"
                />
              </Field>
            </section>

            <section className="space-y-4">
              <SectionHead number="05" title="Tags" />
              <TagAutocomplete
                ariaLabel="Blog tags"
                value={form.tags.map((name) => ({ id: null, name }))}
                onChange={(next) => setForm({ ...form, tags: next.map((t) => t.name) })}
              />
            </section>
          </div>

          <aside className="space-y-6 lg:sticky lg:top-4 lg:self-start">
            <section className="border border-border bg-panel">
              <header className="border-b border-border-soft px-5 py-3">
                <h3 className="font-heading text-sm font-semibold">Publishing</h3>
              </header>
              <div className="space-y-4 p-5 text-sm">
                <div className="flex items-center justify-between">
                  <span>Published</span>
                  <SwitchFlat
                    label="Published"
                    checked={form.isPublished}
                    onChange={(v) => setForm({ ...form, isPublished: v })}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <span>Members only</span>
                  <SwitchFlat
                    label="Members only"
                    checked={form.isMembersOnly}
                    onChange={(v) => setForm({ ...form, isMembersOnly: v })}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <span>Pinned</span>
                  <SwitchFlat
                    label="Pinned"
                    checked={form.isPinned}
                    onChange={(v) => setForm({ ...form, isPinned: v })}
                  />
                </div>
                <Field label="Publish date" hint="Optional. Auto-set on first publish.">
                  <input
                    type="datetime-local"
                    value={form.publishedAt}
                    onChange={(e) => setForm({ ...form, publishedAt: e.target.value })}
                    className="input"
                  />
                </Field>
                <Field label="Scheduled publish" hint="Captured for future use.">
                  <input
                    type="datetime-local"
                    value={form.scheduledPublishAt}
                    onChange={(e) => setForm({ ...form, scheduledPublishAt: e.target.value })}
                    className="input"
                  />
                </Field>
              </div>
            </section>

            <Btn type="submit" variant="accent" size="lg" disabled={submitting} className="w-full">
              {submitting ? "Saving…" : isNew ? "Create post" : "Save changes"}
            </Btn>
          </aside>
        </div>

        <Styles />
      </form>
    </div>
  );
}

function Field({
  label, hint, required, children,
}: { label: string; hint?: string; required?: boolean; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium">
        {label}{required && <span className="text-danger"> *</span>}
      </span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted">{hint}</span>}
    </label>
  );
}

function Styles() {
  return (
    <style>{`
      .input {
        height: 2.5rem;
        width: 100%;
        border: 1px solid hsl(var(--border));
        background: hsl(var(--background));
        padding: 0 0.75rem;
        font-size: 0.875rem;
      }
      .input:focus { outline: none; border-color: hsl(var(--accent)); }
    `}</style>
  );
}
