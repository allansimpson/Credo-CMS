import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { newsApi } from "@/lib/api/news";
import { siteSettingsApi } from "@/lib/api/siteSettings";
import { slugify } from "@/lib/slug";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapFullEditor } from "@/components/shared/TipTapFullEditor";
import { ConfirmDialog } from "@/components/shared/admin/ConfirmDialog";
import type { CreateNewsItemRequest, NewsDetail, UpdateNewsItemRequest } from "@/types/api";

interface FormState {
  slug: string;
  title: string;
  bodyJson: string | null;
  excerpt: string;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  metaDescription: string;
  category: string;
  isPublished: boolean;
  isMembersOnly: boolean;
  expiresAt: string;
  calendarDate: string;
}

const empty: FormState = {
  slug: "", title: "", bodyJson: null, excerpt: "",
  heroImageUrl: null, heroImageWebpUrl: null, heroImageAlt: null,
  metaDescription: "",
  category: "",
  isPublished: false, isMembersOnly: true,
  expiresAt: "", calendarDate: "",
};

function toApi(f: FormState): CreateNewsItemRequest | UpdateNewsItemRequest {
  return {
    slug: f.slug,
    title: f.title,
    bodyJson: f.bodyJson ?? "",
    excerpt: f.excerpt || null,
    heroImageUrl: f.heroImageUrl,
    heroImageWebpUrl: f.heroImageWebpUrl,
    heroImageAlt: f.heroImageAlt,
    metaDescription: f.metaDescription || null,
    category: f.category || null,
    isPublished: f.isPublished,
    isMembersOnly: f.isMembersOnly,
    expiresAt: f.expiresAt ? new Date(f.expiresAt).toISOString() : null,
    calendarDate: f.calendarDate ? new Date(f.calendarDate).toISOString() : null,
  };
}

function fromDetail(n: NewsDetail): FormState {
  return {
    slug: n.slug,
    title: n.title,
    bodyJson: n.bodyJson,
    excerpt: n.excerpt ?? "",
    heroImageUrl: n.heroImageUrl,
    heroImageWebpUrl: n.heroImageWebpUrl,
    heroImageAlt: n.heroImageAlt,
    metaDescription: n.metaDescription ?? "",
    category: n.category ?? "",
    isPublished: n.isPublished,
    isMembersOnly: n.isMembersOnly,
    expiresAt: n.expiresAt ? n.expiresAt.slice(0, 16) : "",
    calendarDate: n.calendarDate ? n.calendarDate.slice(0, 16) : "",
  };
}

export function NewsEditorPage() {
  const { id } = useParams<{ id?: string }>();
  const isNew = !id || id === "new";
  const navigate = useNavigate();

  const [form, setForm] = useState<FormState>(empty);
  const [original, setOriginal] = useState<NewsDetail | null>(null);
  const [loading, setLoading] = useState(!isNew);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [slugAuto, setSlugAuto] = useState(isNew);
  const [categories, setCategories] = useState<string[]>([]);
  const [deleteOpen, setDeleteOpen] = useState(false);

  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    newsApi.get(id!).then((n) => {
      if (cancelled) return;
      setOriginal(n);
      setForm(fromDetail(n));
      setSlugAuto(false);
      setLoading(false);
    }).catch(() => { if (!cancelled) { setErrors(["Could not load news item."]); setLoading(false); } });
    return () => { cancelled = true; };
  }, [id, isNew]);

  useEffect(() => {
    let cancelled = false;
    siteSettingsApi.getAdmin().then((s) => {
      if (cancelled) return;
      try {
        const parsed = JSON.parse(s.newsCategoriesJson);
        if (Array.isArray(parsed)) {
          setCategories(parsed.filter((x): x is string => typeof x === "string" && x.length > 0));
        }
      } catch { /* leave empty */ }
    }).catch(() => { /* leave categories empty */ });
    return () => { cancelled = true; };
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true); setErrors([]); setSuccess(false);
    try {
      if (isNew) {
        const created = await newsApi.create(toApi(form));
        navigate(`/admin/news/${created.id}`);
      } else {
        const updated = await newsApi.update(id!, toApi(form));
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

  const performSoftDelete = async () => {
    if (!id) return;
    setDeleteOpen(false);
    await newsApi.softDelete(id);
    navigate("/admin/news");
  };

  const handleRestore = async () => {
    if (!id) return;
    await newsApi.restore(id);
    const fresh = await newsApi.get(id);
    setOriginal(fresh);
    setForm(fromDetail(fresh));
  };

  if (loading) return <p className="text-muted">Loading…</p>;

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <h1 className="text-2xl font-bold">{isNew ? "New news item" : "Edit news item"}</h1>

      {errors.length > 0 && (
        <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          <ul className="list-disc pl-5">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
        </div>
      )}
      {success && (
        <div role="status" className="rounded-md border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-800">Saved.</div>
      )}
      {original?.isDeleted && (
        <div role="status" className="rounded-md border border-amber-300 bg-amber-50 p-3 text-sm text-amber-800">
          This item is in Trash. <button type="button" onClick={handleRestore} className="font-semibold underline">Restore</button> to make it editable.
        </div>
      )}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Field label="Title" required>
          <input
            value={form.title}
            required
            onChange={(e) => setForm((f) => ({
              ...f,
              title: e.target.value,
              slug: slugAuto ? slugify(e.target.value) : f.slug,
            }))}
            className="input"
          />
        </Field>
        <Field label="Slug" required hint={slugAuto ? "Auto-generating from title; edit to lock." : undefined}>
          <input
            value={form.slug}
            required
            onChange={(e) => { setSlugAuto(false); setForm((f) => ({ ...f, slug: e.target.value })); }}
            className="input"
          />
        </Field>
      </div>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Hero image</legend>
        <ImageUpload
          ariaLabel="Hero image"
          value={{ url: form.heroImageUrl, webpUrl: form.heroImageWebpUrl, alt: form.heroImageAlt }}
          onChange={(next) => setForm((f) => ({
            ...f, heroImageUrl: next.url, heroImageWebpUrl: next.webpUrl, heroImageAlt: next.alt,
          }))}
        />
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Body</legend>
        <TipTapFullEditor
          ariaLabel="News body"
          valueJson={form.bodyJson}
          onChangeJson={(json) => setForm((f) => ({ ...f, bodyJson: json }))}
          placeholder="Write the news item body…"
        />
      </fieldset>

      <fieldset className="grid grid-cols-1 gap-3 rounded-lg border bg-card p-4 sm:grid-cols-2">
        <legend className="px-2 text-sm font-semibold">Categorize</legend>
        <Field
          label="Category"
          hint="Optional. Manage the list in Site Settings → Content."
        >
          <select
            aria-label="Category"
            value={form.category}
            onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))}
            className="input"
          >
            <option value="">&mdash; Uncategorized &mdash;</option>
            {categories.map((c) => <option key={c} value={c}>{c}</option>)}
            {form.category && !categories.includes(form.category) && (
              <option value={form.category}>{form.category} (not in current list)</option>
            )}
          </select>
        </Field>
      </fieldset>

      <fieldset className="grid grid-cols-1 gap-3 rounded-lg border bg-card p-4 sm:grid-cols-2">
        <legend className="px-2 text-sm font-semibold">Schedule</legend>
        <Field label="Calendar date" hint="Optional. The date the announcement is associated with.">
          <input
            type="datetime-local"
            value={form.calendarDate}
            onChange={(e) => setForm((f) => ({ ...f, calendarDate: e.target.value }))}
            className="input"
          />
        </Field>
        <Field label="Expires at" hint="Optional. After this UTC time the item disappears from public listings.">
          <input
            type="datetime-local"
            value={form.expiresAt}
            onChange={(e) => setForm((f) => ({ ...f, expiresAt: e.target.value }))}
            className="input"
          />
        </Field>
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">SEO & summary</legend>
        <Field label="Excerpt" hint="Auto-generated from the body if blank.">
          <textarea value={form.excerpt} maxLength={500}
            onChange={(e) => setForm((f) => ({ ...f, excerpt: e.target.value }))}
            className="input min-h-20 py-2" />
        </Field>
        <Field label="Meta description" hint="Up to 300 chars.">
          <textarea value={form.metaDescription} maxLength={300}
            onChange={(e) => setForm((f) => ({ ...f, metaDescription: e.target.value }))}
            className="input min-h-16 py-2" />
        </Field>
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Visibility</legend>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.isPublished}
            onChange={(e) => setForm((f) => ({ ...f, isPublished: e.target.checked }))} />
          Published
        </label>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.isMembersOnly}
            onChange={(e) => setForm((f) => ({ ...f, isMembersOnly: e.target.checked }))} />
          Members only — anonymous visitors will see a 404 (default for News)
        </label>
      </fieldset>

      <div className="flex flex-wrap items-center gap-2">
        <button type="submit" disabled={submitting}
          className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
          {submitting ? "Saving…" : isNew ? "Create item" : "Save changes"}
        </button>
        {!isNew && original && !original.isDeleted && (
          <button type="button" onClick={() => setDeleteOpen(true)}
            className="inline-flex h-10 items-center justify-center rounded-md border border-danger/30 bg-card px-4 text-sm text-danger hover:bg-danger/10">
            Delete
          </button>
        )}
      </div>

      <ConfirmDialog
        open={deleteOpen}
        title="Move this news item to Trash?"
        message={
          original
            ? `"${original.title}" will be moved to Trash. You can restore it or permanently delete it from there.`
            : "This news item will be moved to Trash. You can restore it or permanently delete it from there."
        }
        confirmLabel="Move to Trash"
        onConfirm={performSoftDelete}
        onCancel={() => setDeleteOpen(false)}
      />

      <style>{`
        .input {
          height: 2.5rem; width: 100%; border-radius: 0.375rem;
          border: 1px solid hsl(var(--input)); background: hsl(var(--background));
          padding: 0 0.75rem; font-size: 0.875rem;
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
        {label}{required && <span className="text-danger"> *</span>}
      </span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted">{hint}</span>}
    </label>
  );
}
