import { useEffect, useMemo, useRef, useState } from "react";
import { documentsApi, publicDocumentFileUrl } from "@/lib/api/documents";
import type { DocumentDto, UpdateDocumentMetadataRequest } from "@/types/api";

const DEFAULT_CATEGORIES = ["Bulletins", "Forms", "Policies", "Board Minutes", "Resources"];

export function DocumentsPage() {
  const [items, setItems] = useState<DocumentDto[]>([]);
  const [filter, setFilter] = useState<string>("");
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<DocumentDto | null>(null);
  const [errors, setErrors] = useState<string[]>([]);

  // Upload form state for new
  const [uploadForm, setUploadForm] = useState({
    title: "", category: DEFAULT_CATEGORIES[0], description: "",
    isPublished: true, isMembersOnly: true,
  });
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    documentsApi.list(filter || undefined, includeDeleted)
      .then((d) => { if (!cancelled) setItems(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [filter, includeDeleted]);

  const categories = useMemo(() => {
    const set = new Set<string>([...DEFAULT_CATEGORIES, ...items.map((d) => d.category)]);
    return Array.from(set).sort();
  }, [items]);

  const grouped = useMemo(() => {
    const m = new Map<string, DocumentDto[]>();
    for (const d of items) {
      const a = m.get(d.category) ?? [];
      a.push(d);
      m.set(d.category, a);
    }
    for (const a of m.values()) a.sort((x, y) => x.title.localeCompare(y.title));
    return Array.from(m.entries()).sort(([a], [b]) => a.localeCompare(b));
  }, [items]);

  const upload = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors([]);
    const file = fileRef.current?.files?.[0];
    if (!file) { setErrors(["Pick a file."]); return; }
    if (!uploadForm.title) { setErrors(["Title required."]); return; }
    try {
      const created = await documentsApi.upload(file, uploadForm);
      setItems((items) => [created, ...items]);
      setUploadForm({ title: "", category: DEFAULT_CATEGORIES[0], description: "",
        isPublished: true, isMembersOnly: true });
      if (fileRef.current) fileRef.current.value = "";
    } catch (err) {
      setErrors(getMessages(err));
    }
  };

  const saveMetadata = async (d: DocumentDto, patch: Partial<UpdateDocumentMetadataRequest>) => {
    const req: UpdateDocumentMetadataRequest = {
      title: d.title, description: d.description, category: d.category,
      isPublished: d.isPublished, isMembersOnly: d.isMembersOnly,
      ...patch,
    };
    const updated = await documentsApi.updateMetadata(d.id, req);
    setItems((items) => items.map((x) => x.id === updated.id ? updated : x));
    setEditing(null);
  };

  const softDelete = async (d: DocumentDto) => {
    if (!window.confirm(`Soft-delete "${d.title}"?`)) return;
    await documentsApi.softDelete(d.id);
    setItems((items) => items.filter((x) => x.id !== d.id));
  };

  const restore = async (d: DocumentDto) => {
    const restored = await documentsApi.restore(d.id);
    setItems((items) => items.map((x) => x.id === restored.id ? restored : x));
  };

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Documents</h1>
        <div className="flex flex-wrap items-center gap-3">
          <select value={filter} onChange={(e) => setFilter(e.target.value)}
            className="h-10 rounded-md border bg-background px-3 text-sm">
            <option value="">All categories</option>
            {categories.map((c) => <option key={c} value={c}>{c}</option>)}
          </select>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={includeDeleted}
              onChange={(e) => setIncludeDeleted(e.target.checked)} />
            Show deleted
          </label>
        </div>
      </div>

      <form onSubmit={upload} className="mt-6 space-y-3 rounded-lg border bg-card p-4">
        <h2 className="text-lg font-semibold">Upload PDF</h2>
        {errors.length > 0 && (
          <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-2 text-xs text-danger">
            <ul className="list-disc pl-4">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
          </div>
        )}
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Title" required>
            <input value={uploadForm.title} required maxLength={200}
              onChange={(e) => setUploadForm({ ...uploadForm, title: e.target.value })}
              className="input" />
          </Field>
          <Field label="Category" required>
            <select value={uploadForm.category} required
              onChange={(e) => setUploadForm({ ...uploadForm, category: e.target.value })}
              className="input">
              {DEFAULT_CATEGORIES.map((c) => <option key={c} value={c}>{c}</option>)}
            </select>
          </Field>
        </div>
        <Field label="Description">
          <textarea value={uploadForm.description} maxLength={500}
            onChange={(e) => setUploadForm({ ...uploadForm, description: e.target.value })}
            className="input min-h-16 py-2" />
        </Field>
        <Field label="PDF file" required>
          <input ref={fileRef} type="file" accept="application/pdf" required className="text-sm" />
        </Field>
        <div className="flex flex-wrap items-center gap-4">
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={uploadForm.isPublished}
              onChange={(e) => setUploadForm({ ...uploadForm, isPublished: e.target.checked })} />
            Published
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={uploadForm.isMembersOnly}
              onChange={(e) => setUploadForm({ ...uploadForm, isMembersOnly: e.target.checked })} />
            Members only
          </label>
          <button type="submit"
            className="ml-auto inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
            Upload
          </button>
        </div>
        <style>{`
          .input { height: 2.5rem; width: 100%; border-radius: 0.375rem;
            border: 1px solid hsl(var(--input)); background: hsl(var(--background));
            padding: 0 0.75rem; font-size: 0.875rem; }
          textarea.input { height: auto; }
        `}</style>
      </form>

      <div className="mt-6">
        {loading && <p className="text-muted">Loading…</p>}
        {!loading && items.length === 0 && <p className="text-muted">No documents.</p>}
        {grouped.map(([category, ds]) => (
          <section key={category} className="mb-6">
            <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-muted">{category}</h2>
            <ul className="divide-y rounded-lg border bg-card">
              {ds.map((d) => (
                <li key={d.id} className="flex flex-col gap-2 p-3 sm:flex-row sm:items-center sm:gap-4">
                  <div className="flex-1">
                    {editing?.id === d.id ? (
                      <input value={editing.title}
                        onChange={(e) => setEditing({ ...editing, title: e.target.value })}
                        className="h-9 w-full rounded-md border bg-background px-2 text-sm font-semibold" />
                    ) : (
                      <button type="button" onClick={() => setEditing(d)}
                        className="text-left font-semibold hover:underline">
                        {d.title}
                      </button>
                    )}
                    <p className="text-xs text-muted">
                      {Math.round(d.sizeBytes / 1024)} KB
                      {d.originalFilename && ` · ${d.originalFilename}`}
                      {d.isMembersOnly && " · Members only"}
                      {!d.isPublished && " · Draft"}
                      {d.isDeleted && " · Deleted"}
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-2 text-xs">
                    <a href={publicDocumentFileUrl(d.id)} target="_blank" rel="noreferrer"
                      className="text-primary hover:underline">Preview</a>
                    {editing?.id === d.id ? (
                      <>
                        <button type="button"
                          onClick={() => saveMetadata(d, { title: editing!.title })}
                          className="text-emerald-700 hover:underline">Save</button>
                        <button type="button" onClick={() => setEditing(null)}
                          className="text-muted hover:underline">Cancel</button>
                      </>
                    ) : (
                      <>
                        <button type="button"
                          onClick={() => saveMetadata(d, { isPublished: !d.isPublished })}
                          className="text-primary hover:underline">
                          {d.isPublished ? "Unpublish" : "Publish"}
                        </button>
                        {d.isDeleted ? (
                          <button type="button" onClick={() => restore(d)}
                            className="text-emerald-700 hover:underline">Restore</button>
                        ) : (
                          <button type="button" onClick={() => softDelete(d)}
                            className="text-danger hover:underline">Delete</button>
                        )}
                      </>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>
    </div>
  );
}

function Field({ label, required, children }: { label: string; required?: boolean; children: React.ReactNode }) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-medium">
        {label}{required && <span className="text-danger"> *</span>}
      </span>
      {children}
    </label>
  );
}

function getMessages(err: unknown): string[] {
  return typeof err === "object" && err !== null && "getMessages" in err
    ? (err as { getMessages: () => string[] }).getMessages()
    : ["Failed."];
}
