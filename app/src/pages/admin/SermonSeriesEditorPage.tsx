import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { sermonSeriesApi, type SermonSeriesDetail, type SermonSeriesRequest } from "@/lib/api/sermonSeries";
import { siteSettingsApi } from "@/lib/api/siteSettings";
import { slugify } from "@/lib/slug";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapFullEditor } from "@/components/shared/TipTapFullEditor";
import { ScriptureReferenceInput } from "@/components/shared/ScriptureReferenceInput";
import type { ScriptureReference } from "@/lib/bible/scripture";

interface FormState {
  slug: string;
  title: string;
  descriptionJson: string | null;
  bannerImageUrl: string | null;
  bannerImageWebpUrl: string | null;
  bannerImageAlt: string | null;
  startDate: string;
  endDate: string;
  context: string;
  scopeLabel: string;
  plannedParts: string;
  scriptureReferences: ScriptureReference[];
}

const empty: FormState = {
  slug: "", title: "", descriptionJson: null,
  bannerImageUrl: null, bannerImageWebpUrl: null, bannerImageAlt: null,
  startDate: new Date().toISOString().slice(0, 10), endDate: "",
  context: "", scopeLabel: "", plannedParts: "",
  scriptureReferences: [],
};

function fromDetail(d: SermonSeriesDetail): FormState {
  return {
    slug: d.slug,
    title: d.title,
    descriptionJson: d.descriptionJson,
    bannerImageUrl: d.bannerImageUrl,
    bannerImageWebpUrl: d.bannerImageWebpUrl,
    bannerImageAlt: d.bannerImageAlt,
    startDate: d.startDate,
    endDate: d.endDate ?? "",
    context: d.context ?? "",
    scopeLabel: d.scopeLabel ?? "",
    plannedParts: d.plannedParts !== null ? String(d.plannedParts) : "",
    scriptureReferences: d.scriptureReferences.map((r) => ({
      book: r.book, chapterStart: r.chapterStart,
      verseStart: r.verseStart, chapterEnd: r.chapterEnd, verseEnd: r.verseEnd,
    })),
  };
}

function toApi(f: FormState): SermonSeriesRequest {
  const parsedPlannedParts = f.plannedParts.trim() === "" ? null : Number(f.plannedParts);
  return {
    slug: f.slug,
    title: f.title,
    descriptionJson: f.descriptionJson,
    bannerImageUrl: f.bannerImageUrl,
    bannerImageWebpUrl: f.bannerImageWebpUrl,
    bannerImageAlt: f.bannerImageAlt,
    startDate: f.startDate,
    endDate: f.endDate || null,
    context: f.context.trim() || null,
    scopeLabel: f.scopeLabel.trim() || null,
    plannedParts: parsedPlannedParts !== null && Number.isFinite(parsedPlannedParts) ? parsedPlannedParts : null,
    scriptureReferences: f.scriptureReferences.map((r) => ({
      book: r.book, chapterStart: r.chapterStart,
      verseStart: r.verseStart, chapterEnd: r.chapterEnd, verseEnd: r.verseEnd,
    })),
  };
}

export function SermonSeriesEditorPage() {
  const { id } = useParams<{ id?: string }>();
  const isNew = !id || id === "new";
  const navigate = useNavigate();

  const [form, setForm] = useState<FormState>(empty);
  const [original, setOriginal] = useState<SermonSeriesDetail | null>(null);
  const [loading, setLoading] = useState(!isNew);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [slugAuto, setSlugAuto] = useState(isNew);
  const [contexts, setContexts] = useState<string[]>([]);

  useEffect(() => {
    let cancelled = false;
    siteSettingsApi.getAdmin().then((s) => {
      if (cancelled) return;
      try {
        const parsed = JSON.parse(s.sermonContextsJson);
        if (Array.isArray(parsed)) {
          setContexts(parsed.filter((x): x is string => typeof x === "string" && x.length > 0));
        }
      } catch { /* leave empty */ }
    }).catch(() => { /* leave empty */ });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    sermonSeriesApi.get(id!)
      .then((d) => {
        if (cancelled) return;
        setOriginal(d);
        setForm(fromDetail(d));
        setSlugAuto(false);
        setLoading(false);
      })
      .catch(() => { if (!cancelled) { setErrors(["Could not load series."]); setLoading(false); } });
    return () => { cancelled = true; };
  }, [id, isNew]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true); setErrors([]); setSuccess(false);
    try {
      if (isNew) {
        const created = await sermonSeriesApi.create(toApi(form));
        navigate(`/admin/sermon-series/${created.id}`);
      } else {
        const updated = await sermonSeriesApi.update(id!, toApi(form));
        setOriginal(updated);
        setSuccess(true);
      }
    } catch (err) {
      const m = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Save failed."];
      setErrors(m);
    } finally {
      setSubmitting(false);
    }
  };

  const softDelete = async () => {
    if (!id) return;
    if (!window.confirm("Soft-delete this sermon series?")) return;
    await sermonSeriesApi.softDelete(id);
    navigate("/admin/sermon-series");
  };

  if (loading) return <p className="text-muted">Loading…</p>;

  return (
    <form onSubmit={submit} className="space-y-6">
      <h1 className="text-2xl font-bold">{isNew ? "New sermon series" : "Edit sermon series"}</h1>

      {errors.length > 0 && (
        <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          <ul className="list-disc pl-5">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
        </div>
      )}
      {success && (
        <div role="status" className="border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-800">Saved.</div>
      )}
      {original?.isDeleted && (
        <div role="status" className="border border-amber-300 bg-amber-50 p-3 text-sm text-amber-800">
          This series is soft-deleted.
        </div>
      )}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Field label="Title" required>
          <input value={form.title} required maxLength={200}
            onChange={(e) => setForm((f) => ({
              ...f, title: e.target.value,
              slug: slugAuto ? slugify(e.target.value) : f.slug,
            }))}
            className="input" />
        </Field>
        <Field label="Slug" required hint={slugAuto ? "Auto-generating from title; edit to lock." : undefined}>
          <input value={form.slug} required
            onChange={(e) => { setSlugAuto(false); setForm((f) => ({ ...f, slug: e.target.value })); }}
            className="input" />
        </Field>
      </div>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Banner image</legend>
        <ImageUpload
          ariaLabel="Banner image"
          value={{ url: form.bannerImageUrl, webpUrl: form.bannerImageWebpUrl, alt: form.bannerImageAlt }}
          onChange={(next) => setForm((f) => ({
            ...f, bannerImageUrl: next.url, bannerImageWebpUrl: next.webpUrl, bannerImageAlt: next.alt,
          }))}
        />
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Description</legend>
        <TipTapFullEditor
          ariaLabel="Series description"
          valueJson={form.descriptionJson}
          onChangeJson={(json) => setForm((f) => ({ ...f, descriptionJson: json }))}
          placeholder="Describe the series…"
        />
      </fieldset>

      <fieldset className="grid grid-cols-1 gap-3 border bg-card p-4 sm:grid-cols-2">
        <legend className="px-2 text-sm font-semibold">Schedule</legend>
        <Field label="Start date" required>
          <input type="date" required value={form.startDate}
            onChange={(e) => setForm((f) => ({ ...f, startDate: e.target.value }))} className="input" />
        </Field>
        <Field label="End date" hint="Leave blank for ongoing.">
          <input type="date" value={form.endDate}
            onChange={(e) => setForm((f) => ({ ...f, endDate: e.target.value }))} className="input" />
        </Field>
      </fieldset>

      <fieldset className="grid grid-cols-1 gap-3 border bg-card p-4 sm:grid-cols-3">
        <legend className="px-2 text-sm font-semibold">Categorize</legend>
        <Field label="Context" hint="Teaching track. Manage the list in Site Settings → Content → Sermons.">
          <select
            aria-label="Sermon context"
            value={form.context}
            onChange={(e) => setForm((f) => ({ ...f, context: e.target.value }))}
            className="input"
          >
            <option value="">&mdash; Unset &mdash;</option>
            {contexts.map((c) => <option key={c} value={c}>{c}</option>)}
            {form.context && !contexts.includes(form.context) && (
              <option value={form.context}>{form.context} (not in current list)</option>
            )}
          </select>
        </Field>
        <Field label="Scope label" hint='Optional. e.g. "Hebrews", "Luke 14–15". Auto-derived from scripture refs if blank.'>
          <input
            value={form.scopeLabel}
            maxLength={120}
            onChange={(e) => setForm((f) => ({ ...f, scopeLabel: e.target.value }))}
            className="input"
          />
        </Field>
        <Field label="Planned parts" hint="Optional. Expected number of messages — drives the progress bar on the public by-series page.">
          <input
            type="number"
            min={1}
            max={200}
            value={form.plannedParts}
            aria-label="Planned parts"
            onChange={(e) => setForm((f) => ({ ...f, plannedParts: e.target.value }))}
            className="input"
            placeholder="e.g. 6"
          />
        </Field>
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Scripture references</legend>
        {form.scriptureReferences.map((ref, i) => (
          <ScriptureReferenceInput
            key={i}
            value={ref}
            onChange={(next) => setForm((f) => ({
              ...f,
              scriptureReferences: f.scriptureReferences.map((r, idx) => idx === i ? next : r),
            }))}
            onRemove={() => setForm((f) => ({
              ...f,
              scriptureReferences: f.scriptureReferences.filter((_, idx) => idx !== i),
            }))}
          />
        ))}
        <button type="button"
          onClick={() => setForm((f) => ({
            ...f,
            scriptureReferences: [...f.scriptureReferences,
              { book: 45, chapterStart: 1, verseStart: null, chapterEnd: null, verseEnd: null }],
          }))}
          className="inline-flex h-9 items-center justify-center border bg-card px-3 text-sm hover:bg-panel-alt">
          + Add reference
        </button>
      </fieldset>

      <div className="flex flex-wrap gap-2">
        <button type="submit" disabled={submitting}
          className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
          {submitting ? "Saving…" : isNew ? "Create series" : "Save changes"}
        </button>
        {!isNew && original && !original.isDeleted && (
          <button type="button" onClick={softDelete}
            className="inline-flex h-10 items-center justify-center border border-danger/30 bg-card px-4 text-sm text-danger hover:bg-danger/10">
            Delete
          </button>
        )}
      </div>

      <style>{`
        .input { height: 2.5rem; width: 100%;
          border: 1px solid hsl(var(--input)); background: hsl(var(--background));
          padding: 0 0.75rem; font-size: 0.875rem; }
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
