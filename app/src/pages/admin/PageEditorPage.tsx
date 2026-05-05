import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { pagesApi } from "@/lib/api/pages";
import { slugify } from "@/lib/slug";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapFullEditor } from "@/components/shared/TipTapFullEditor";
import type { CreatePageRequest, PageDetail, UpdatePageRequest } from "@/types/api";

interface FormState {
  slug: string;
  title: string;
  bodyJson: string | null;
  excerpt: string;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  metaDescription: string;
  isPublished: boolean;
  isMembersOnly: boolean;
}

const emptyForm: FormState = {
  slug: "",
  title: "",
  bodyJson: null,
  excerpt: "",
  heroImageUrl: null,
  heroImageWebpUrl: null,
  heroImageAlt: null,
  metaDescription: "",
  isPublished: false,
  isMembersOnly: false,
};

export function PageEditorPage() {
  const { id } = useParams<{ id?: string }>();
  const isNew = !id || id === "new";
  const navigate = useNavigate();

  const [form, setForm] = useState<FormState>(emptyForm);
  const [original, setOriginal] = useState<PageDetail | null>(null);
  const [loading, setLoading] = useState(!isNew);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);

  // Auto-generate slug from title when creating, until the user edits the slug.
  const [slugAutoGen, setSlugAutoGen] = useState(isNew);

  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    pagesApi.get(id!).then((p) => {
      if (cancelled) return;
      setOriginal(p);
      setForm({
        slug: p.slug,
        title: p.title,
        bodyJson: p.bodyJson,
        excerpt: p.excerpt ?? "",
        heroImageUrl: p.heroImageUrl,
        heroImageWebpUrl: p.heroImageWebpUrl,
        heroImageAlt: p.heroImageAlt,
        metaDescription: p.metaDescription ?? "",
        isPublished: p.isPublished,
        isMembersOnly: p.isMembersOnly,
      });
      setSlugAutoGen(false);
      setLoading(false);
    }).catch(() => {
      if (cancelled) return;
      setErrors(["Could not load page."]);
      setLoading(false);
    });
    return () => { cancelled = true; };
  }, [id, isNew]);

  const handleTitleChange = (next: string) => {
    setForm((f) => ({
      ...f,
      title: next,
      slug: slugAutoGen ? slugify(next) : f.slug,
    }));
  };

  const handleSlugChange = (next: string) => {
    setSlugAutoGen(false);
    setForm((f) => ({ ...f, slug: next }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);

    const body: CreatePageRequest | UpdatePageRequest = {
      slug: form.slug,
      title: form.title,
      bodyJson: form.bodyJson ?? "",
      excerpt: form.excerpt || null,
      heroImageUrl: form.heroImageUrl,
      heroImageWebpUrl: form.heroImageWebpUrl,
      heroImageAlt: form.heroImageAlt,
      metaDescription: form.metaDescription || null,
      isPublished: form.isPublished,
      isMembersOnly: form.isMembersOnly,
    };

    try {
      if (isNew) {
        const created = await pagesApi.create(body);
        navigate(`/admin/pages/${created.id}`);
      } else {
        const updated = await pagesApi.update(id!, body);
        setOriginal(updated);
        setSuccess(true);
      }
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Save failed."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  const handleSoftDelete = async () => {
    if (!id || !original) return;
    if (!window.confirm("Soft-delete this page? It can be restored from the Deleted tab.")) return;
    await pagesApi.softDelete(id);
    navigate("/admin/pages");
  };

  const handleRestore = async () => {
    if (!id) return;
    await pagesApi.restore(id);
    const fresh = await pagesApi.get(id);
    setOriginal(fresh);
    setForm((f) => ({ ...f, slug: fresh.slug }));
  };

  if (loading) return <p className="text-muted-foreground">Loading…</p>;

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">{isNew ? "New page" : "Edit page"}</h1>
        {original && original.isSystemPage && (
          <span className="rounded-full border bg-muted px-3 py-1 text-xs text-muted-foreground">
            System page — slug locked
          </span>
        )}
      </div>

      {errors.length > 0 && (
        <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          <ul className="list-disc pl-5">
            {errors.map((e) => <li key={e}>{e}</li>)}
          </ul>
        </div>
      )}
      {success && (
        <div role="status" className="rounded-md border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-800">
          Saved.
        </div>
      )}
      {original?.isDeleted && (
        <div role="status" className="rounded-md border border-amber-300 bg-amber-50 p-3 text-sm text-amber-800">
          This page is in the deleted tab. <button type="button" onClick={handleRestore} className="font-semibold underline">Restore</button> to make it editable.
        </div>
      )}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Field label="Title" required>
          <input
            value={form.title}
            required
            onChange={(e) => handleTitleChange(e.target.value)}
            className="input"
          />
        </Field>
        <Field
          label="Slug"
          required
          hint={slugAutoGen ? "Auto-generating from title; edit to lock." : (original ? "Changing the slug breaks any external links." : undefined)}
        >
          <input
            value={form.slug}
            required
            disabled={original?.isSystemPage ?? false}
            onChange={(e) => handleSlugChange(e.target.value)}
            className="input"
          />
        </Field>
      </div>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Hero image</legend>
        <ImageUpload
          ariaLabel="Hero image"
          value={{
            url: form.heroImageUrl,
            webpUrl: form.heroImageWebpUrl,
            alt: form.heroImageAlt,
          }}
          onChange={(next) => setForm((f) => ({
            ...f,
            heroImageUrl: next.url,
            heroImageWebpUrl: next.webpUrl,
            heroImageAlt: next.alt,
          }))}
        />
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Body</legend>
        <TipTapFullEditor
          ariaLabel="Page body"
          valueJson={form.bodyJson}
          onChangeJson={(json) => setForm((f) => ({ ...f, bodyJson: json }))}
          placeholder="Write the page body here…"
        />
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">SEO & summary</legend>
        <Field label="Excerpt" hint="Optional. Auto-generated from the body's text if left blank.">
          <textarea
            value={form.excerpt}
            maxLength={500}
            onChange={(e) => setForm((f) => ({ ...f, excerpt: e.target.value }))}
            className="input min-h-20 py-2"
          />
        </Field>
        <Field label="Meta description" hint="Used for search-engine snippets. Up to 300 chars.">
          <textarea
            value={form.metaDescription}
            maxLength={300}
            onChange={(e) => setForm((f) => ({ ...f, metaDescription: e.target.value }))}
            className="input min-h-16 py-2"
          />
        </Field>
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Visibility</legend>
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={form.isPublished}
            onChange={(e) => setForm((f) => ({ ...f, isPublished: e.target.checked }))}
          />
          Published
        </label>
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={form.isMembersOnly}
            onChange={(e) => setForm((f) => ({ ...f, isMembersOnly: e.target.checked }))}
          />
          Members only — anonymous visitors will see a 404
        </label>
      </fieldset>

      <div className="flex flex-wrap items-center gap-2">
        <button
          type="submit"
          disabled={submitting}
          className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
        >
          {submitting ? "Saving…" : isNew ? "Create page" : "Save changes"}
        </button>
        {!isNew && original && !original.isDeleted && !original.isSystemPage && (
          <button
            type="button"
            onClick={handleSoftDelete}
            className="inline-flex h-10 items-center justify-center rounded-md border border-destructive/30 bg-card px-4 text-sm text-destructive hover:bg-destructive/10"
          >
            Delete
          </button>
        )}
      </div>

      <style>{`
        .input {
          height: 2.5rem;
          width: 100%;
          border-radius: 0.375rem;
          border: 1px solid hsl(var(--input));
          background: hsl(var(--background));
          padding: 0 0.75rem;
          font-size: 0.875rem;
        }
        textarea.input { height: auto; }
      `}</style>
    </form>
  );
}

function Field({ label, hint, required, children }: { label: string; hint?: string; required?: boolean; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium">
        {label}{required && <span className="text-destructive"> *</span>}
      </span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted-foreground">{hint}</span>}
    </label>
  );
}
