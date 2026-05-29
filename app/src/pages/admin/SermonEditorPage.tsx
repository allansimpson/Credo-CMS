import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { sermonsApi, type SermonDetail, type SermonTagInput, type UpdateSermonRequest } from "@/lib/api/sermons";
import { sermonSeriesApi, type SermonSeriesListItem } from "@/lib/api/sermonSeries";
import { leadersApi } from "@/lib/api/leaders";
import type { Leader } from "@/types/api";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { useToast } from "@/components/shared/admin/Toast";
import { ConfirmDialog } from "@/components/shared/admin/ConfirmDialog";
import { TipTapFullEditor } from "@/components/shared/TipTapFullEditor";
import { ScriptureReferenceInput } from "@/components/shared/ScriptureReferenceInput";
import { TagAutocomplete, type SelectedTag } from "@/components/shared/TagAutocomplete";
import type { ScriptureReference } from "@/lib/bible/scripture";

interface FormState {
  slug: string;
  title: string;
  descriptionJson: string | null;
  thumbnailBlobUrl: string | null;
  thumbnailWebpBlobUrl: string | null;
  publishedAt: string;
  transcript: string;
  speakerLeaderId: string | null;
  speakerNameFreeText: string;
  speakerMode: "leader" | "free";
  sermonSeriesId: string | null;
  serviceType: import("@/lib/api/sermons").ServiceType;
  isPublished: boolean;
  isMembersOnly: boolean;
  tags: SelectedTag[];
  scriptureReferences: ScriptureReference[];
}

function fromDetail(d: SermonDetail): FormState {
  return {
    slug: d.slug,
    title: d.title,
    descriptionJson: d.descriptionJson,
    thumbnailBlobUrl: d.thumbnailBlobUrl,
    thumbnailWebpBlobUrl: d.thumbnailWebpBlobUrl,
    publishedAt: d.publishedAt.slice(0, 10),
    transcript: d.transcript ?? "",
    speakerLeaderId: d.speakerLeaderId,
    speakerNameFreeText: d.speakerNameFreeText ?? "",
    speakerMode: d.speakerLeaderId ? "leader" : "free",
    sermonSeriesId: d.sermonSeriesId,
    serviceType: d.serviceType ?? "AmWorship",
    isPublished: d.isPublished,
    isMembersOnly: d.isMembersOnly,
    tags: d.tags.map((t) => ({ id: t.id, name: t.name })),
    scriptureReferences: d.scriptureReferences.map((r) => ({
      book: r.book, chapterStart: r.chapterStart,
      verseStart: r.verseStart, chapterEnd: r.chapterEnd, verseEnd: r.verseEnd,
    })),
  };
}

function toApi(f: FormState): UpdateSermonRequest {
  return {
    slug: f.slug,
    title: f.title,
    descriptionJson: f.descriptionJson,
    thumbnailBlobUrl: f.thumbnailBlobUrl,
    thumbnailWebpBlobUrl: f.thumbnailWebpBlobUrl,
    publishedAt: new Date(f.publishedAt + "T12:00:00").toISOString(),
    transcript: f.transcript || null,
    transcriptSource: f.transcript ? 2 : 0, // Uploaded if user edited; None otherwise
    speakerLeaderId: f.speakerMode === "leader" ? f.speakerLeaderId : null,
    speakerNameFreeText: f.speakerMode === "free" ? (f.speakerNameFreeText || null) : null,
    sermonSeriesId: f.sermonSeriesId,
    serviceType: f.serviceType,
    isPublished: f.isPublished,
    isMembersOnly: f.isMembersOnly,
    tags: f.tags.map((t) => ({ id: t.id, name: t.name } as SermonTagInput)),
    attachmentDocumentIds: [],
    scriptureReferences: f.scriptureReferences.map((r) => ({
      book: r.book, chapterStart: r.chapterStart,
      verseStart: r.verseStart, chapterEnd: r.chapterEnd, verseEnd: r.verseEnd,
    })),
  };
}

export function SermonEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [original, setOriginal] = useState<SermonDetail | null>(null);
  const [form, setForm] = useState<FormState | null>(null);
  const [series, setSeries] = useState<SermonSeriesListItem[]>([]);
  const [leaders, setLeaders] = useState<Leader[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const { toast } = useToast();

  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    Promise.all([
      sermonsApi.get(id),
      sermonSeriesApi.list({ pageSize: 100 }),
      leadersApi.list(),
    ]).then(([detail, seriesList, leadersList]) => {
      if (cancelled) return;
      setOriginal(detail);
      const f = fromDetail(detail);
      if (!f.speakerLeaderId) {
        const defaultLeader = leadersList.find((l) => l.category === "Ministers" && l.displayOrder <= 1);
        if (defaultLeader) {
          f.speakerLeaderId = defaultLeader.id;
          f.speakerMode = "leader";
        }
      }
      setForm(f);
      setSeries(seriesList.items);
      setLeaders(leadersList);
    }).catch(() => { if (!cancelled) setErrors(["Could not load sermon."]); });
    return () => { cancelled = true; };
  }, [id]);

  if (!form || !original) return <p className="text-muted">Loading…</p>;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true); setErrors([]);
    try {
      const updated = await sermonsApi.update(id!, toApi(form));
      setOriginal(updated);
      toast("success", "Sermon saved.");
    } catch (err) {
      const m = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Save failed."];
      setErrors(m);
      toast("error", m[0]);
    } finally {
      setSubmitting(false);
    }
  };

  const performSoftDelete = async () => {
    setDeleteConfirmOpen(false);
    await sermonsApi.softDelete(id!);
    navigate("/admin/sermons");
  };

  return (
    <>
    <ConfirmDialog
      open={deleteConfirmOpen}
      tone="danger"
      title="Soft-delete this sermon?"
      message="The sermon will be moved to the deleted tab. It stays in the archive and can be restored later — visitors won't see it in the meantime."
      confirmLabel="Soft-delete"
      onConfirm={performSoftDelete}
      onCancel={() => setDeleteConfirmOpen(false)}
    />
    <form onSubmit={submit} className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Edit sermon</h1>
        <a href={`https://www.youtube.com/watch?v=${original.youTubeVideoId}`} target="_blank" rel="noreferrer"
          className="text-sm text-primary hover:underline">Open on YouTube ↗</a>
      </div>

      {errors.length > 0 && (
        <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          <ul className="list-disc pl-5">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
        </div>
      )}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Field label="Title" required>
          <input value={form.title} required maxLength={200}
            onChange={(e) => setForm({ ...form, title: e.target.value })} className="input" />
        </Field>
        <Field label="Slug" required>
          <input value={form.slug} required
            onChange={(e) => setForm({ ...form, slug: e.target.value })} className="input" />
        </Field>
      </div>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Thumbnail</legend>
        <ImageUpload
          ariaLabel="Thumbnail"
          value={{ url: form.thumbnailBlobUrl, webpUrl: form.thumbnailWebpBlobUrl, alt: form.title }}
          onChange={(next) => setForm({ ...form, thumbnailBlobUrl: next.url, thumbnailWebpBlobUrl: next.webpUrl })}
        />
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Description</legend>
        <TipTapFullEditor
          ariaLabel="Sermon description"
          valueJson={form.descriptionJson}
          onChangeJson={(json) => setForm({ ...form, descriptionJson: json })}
          placeholder="Sermon description…"
        />
      </fieldset>

      <fieldset className="grid grid-cols-1 gap-3 border bg-card p-4 sm:grid-cols-2">
        <legend className="px-2 text-sm font-semibold">Speaker & Series</legend>
        <Field label="Speaker mode">
          <select value={form.speakerMode}
            onChange={(e) => setForm({ ...form, speakerMode: e.target.value as "leader" | "free" })}
            className="input">
            <option value="leader">Linked to a Leader</option>
            <option value="free">Free-text (guest speaker)</option>
          </select>
        </Field>
        {form.speakerMode === "leader" ? (
          <Field label="Leader">
            <select aria-label="Leader" value={form.speakerLeaderId ?? ""}
              onChange={(e) => setForm({ ...form, speakerLeaderId: e.target.value || null })}
              className="input">
              <option value="">— Select a leader —</option>
              {leaders.map((l) => (
                <option key={l.id} value={l.id}>{l.fullName}{l.title ? ` — ${l.title}` : ""}</option>
              ))}
            </select>
          </Field>
        ) : (
          <Field label="Speaker name">
            <input value={form.speakerNameFreeText} maxLength={200}
              onChange={(e) => setForm({ ...form, speakerNameFreeText: e.target.value })}
              className="input" />
          </Field>
        )}
        <Field label="Series">
          <select value={form.sermonSeriesId ?? ""}
            onChange={(e) => setForm({ ...form, sermonSeriesId: e.target.value || null })}
            className="input">
            <option value="">— No series —</option>
            {series.map((s) => <option key={s.id} value={s.id}>{s.title}</option>)}
          </select>
        </Field>
        <Field label="Service type" required>
          <select value={form.serviceType}
            onChange={(e) => setForm({ ...form, serviceType: e.target.value as FormState["serviceType"] })}
            className="input">
            <option value="AmBibleClass">AM Bible Class</option>
            <option value="AmWorship">AM Worship</option>
            <option value="PmWorship">PM Worship</option>
            <option value="WednesdayNight">Wednesday Night</option>
            <option value="Special">Special</option>
          </select>
        </Field>
        <Field label="Sermon date" required>
          <input type="date" value={form.publishedAt} required
            onChange={(e) => setForm({ ...form, publishedAt: e.target.value })} className="input" />
        </Field>
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Tags</legend>
        <TagAutocomplete
          ariaLabel="Sermon tags"
          value={form.tags}
          onChange={(next) => setForm({ ...form, tags: next })}
        />
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Scripture references</legend>
        {form.scriptureReferences.map((ref, i) => (
          <ScriptureReferenceInput
            key={i}
            value={ref}
            onChange={(next) => setForm({
              ...form,
              scriptureReferences: form.scriptureReferences.map((r, idx) => idx === i ? next : r),
            })}
            onRemove={() => setForm({
              ...form,
              scriptureReferences: form.scriptureReferences.filter((_, idx) => idx !== i),
            })}
          />
        ))}
        <button type="button"
          onClick={() => setForm({
            ...form,
            scriptureReferences: [...form.scriptureReferences,
              { book: 45, chapterStart: 1, verseStart: null, chapterEnd: null, verseEnd: null }],
          })}
          className="inline-flex h-9 items-center justify-center border bg-card px-3 text-sm hover:bg-panel-alt">
          + Add reference
        </button>
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Transcript</legend>
        <textarea value={form.transcript}
          onChange={(e) => setForm({ ...form, transcript: e.target.value })}
          className="input min-h-32 py-2"
          placeholder="Pulled from YouTube timedtext if available; paste a transcript here to override." />
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Visibility</legend>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.isPublished}
            onChange={(e) => setForm({ ...form, isPublished: e.target.checked })} />
          Published
        </label>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.isMembersOnly}
            onChange={(e) => setForm({ ...form, isMembersOnly: e.target.checked })} />
          Members only
        </label>
      </fieldset>

      <div className="flex flex-wrap gap-2">
        <button type="submit" disabled={submitting}
          className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
          {submitting ? "Saving…" : "Save changes"}
        </button>
        {!original.isDeleted && (
          <button type="button" onClick={() => setDeleteConfirmOpen(true)}
            className="inline-flex h-10 items-center justify-center border border-danger/30 bg-card px-4 text-sm text-danger hover:bg-danger/10">
            Delete
          </button>
        )}
      </div>

      <style>{`
        .input { height: 2.5rem; width: 100%;
          border: 1px solid hsl(var(--input)); background: hsl(var(--background));
          padding: 0 0.75rem; font-size: 0.875rem; }
        textarea.input { height: auto; }
      `}</style>
    </form>
    </>
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
