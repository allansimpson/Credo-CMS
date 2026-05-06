import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Eye, Globe, History } from "lucide-react";
import { pagesApi } from "@/lib/api/pages";
import { slugify } from "@/lib/slug";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapFullEditor } from "@/components/shared/TipTapFullEditor";
import type { CreatePageRequest, PageDetail, UpdatePageRequest } from "@/types/api";
import {
  Btn,
  Chip,
  Field,
  MetaLabel,
  SectionHead,
  SwitchFlat,
} from "@/components/shared/admin/EditorialPrimitives";

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
  const [dirty, setDirty] = useState(false);
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
      setDirty(false);
      setLoading(false);
    }).catch(() => {
      if (cancelled) return;
      setErrors(["Could not load page."]);
      setLoading(false);
    });
    return () => { cancelled = true; };
  }, [id, isNew]);

  const update = (patch: Partial<FormState>) => {
    setForm((f) => ({ ...f, ...patch }));
    setDirty(true);
    setSuccess(false);
  };

  const handleTitleChange = (next: string) =>
    update({ title: next, ...(slugAutoGen ? { slug: slugify(next) } : {}) });

  const handleSlugChange = (next: string) => {
    setSlugAutoGen(false);
    update({ slug: next });
  };

  const buildBody = (): CreatePageRequest | UpdatePageRequest => ({
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
  });

  const submit = async (publish?: boolean) => {
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);
    const body = buildBody();
    if (typeof publish === "boolean") body.isPublished = publish;

    try {
      if (isNew) {
        const created = await pagesApi.create(body);
        navigate(`/admin/pages/${created.id}`);
      } else {
        const updated = await pagesApi.update(id!, body);
        setOriginal(updated);
        setForm((f) => ({ ...f, isPublished: updated.isPublished }));
        setSuccess(true);
        setDirty(false);
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

  const wordCount = useMemo(() => approxWordCount(form.bodyJson), [form.bodyJson]);
  const readMin = Math.max(1, Math.round(wordCount / 220));

  if (loading) return <p className="text-muted">Loading…</p>;

  const lastSavedLabel = original
    ? new Date(original.modifiedAt).toLocaleString()
    : "never";

  return (
    <form
      onSubmit={(e) => { e.preventDefault(); void submit(); }}
      className="space-y-6"
    >
      {/* Editor command bar */}
      <header className="relative flex flex-wrap items-center gap-4 border border-border bg-panel px-5 py-3">
        <span aria-hidden className="absolute inset-y-0 left-0 w-[3px] bg-accent" />
        <div className="min-w-0 flex-1">
          <MetaLabel>
            {isNew ? "Editing · new page" : `Editing · ${original?.slug ?? "—"}`}
          </MetaLabel>
          <p className="mt-1 truncate font-heading text-base font-semibold">
            {form.title || "Untitled page"}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {dirty && <Chip tone="warn" dot>Unsaved changes</Chip>}
          {success && <Chip tone="success" dot>Saved</Chip>}
          <span className="font-mono text-[11px] text-muted">
            Last saved · {lastSavedLabel}
          </span>
          <Btn iconLeft={<History className="h-3.5 w-3.5" />}>History</Btn>
          {!isNew && (
            <Btn
              iconLeft={<Eye className="h-3.5 w-3.5" />}
              onClick={() => window.open(`/${form.slug}`, "_blank")}
            >
              Preview
            </Btn>
          )}
          <Btn
            type="submit"
            disabled={submitting}
            onClick={() => void submit(false)}
          >
            {submitting ? "Saving…" : "Save draft"}
          </Btn>
          <Btn
            type="button"
            variant="accent"
            disabled={submitting}
            onClick={() => void submit(true)}
          >
            Publish
          </Btn>
        </div>
      </header>

      {errors.length > 0 && (
        <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          <ul className="list-disc pl-5">
            {errors.map((e) => <li key={e}>{e}</li>)}
          </ul>
        </div>
      )}
      {original?.isDeleted && (
        <div role="status" className="border border-warn/40 bg-warn/10 p-3 text-sm text-warn">
          This page is in the deleted tab.{" "}
          <button type="button" onClick={handleRestore} className="font-semibold underline">
            Restore
          </button>{" "}
          to make it editable.
        </div>
      )}

      <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
        {/* Left column */}
        <div className="space-y-6">
          <div>
            <MetaLabel>
              Public page · /{form.slug || "new"}
            </MetaLabel>
            <input
              value={form.title}
              required
              onChange={(e) => handleTitleChange(e.target.value)}
              placeholder="Untitled page"
              className="mt-3 w-full border-0 border-b border-transparent bg-transparent font-heading text-[40px] font-semibold leading-tight tracking-[-0.025em] focus-visible:border-accent focus-visible:outline-none"
            />
            <div className="mt-3 flex items-center gap-3 border-t border-border-soft pt-3">
              <Globe className="h-4 w-4 text-muted" />
              <span className="font-mono text-sm text-fg-soft">/{form.slug}</span>
              <button
                type="button"
                onClick={() => setSlugAutoGen((v) => !v)}
                disabled={original?.isSystemPage ?? false}
                className="text-xs text-accent hover:underline disabled:opacity-50"
              >
                {slugAutoGen ? "Lock slug" : "Edit slug"}
              </button>
              {!slugAutoGen && (
                <input
                  value={form.slug}
                  required
                  disabled={original?.isSystemPage ?? false}
                  onChange={(e) => handleSlugChange(e.target.value)}
                  className="ml-auto h-7 w-56 border border-border bg-background px-2 font-mono text-xs focus-visible:border-accent focus-visible:outline-none"
                />
              )}
            </div>
          </div>

          {/* Hero */}
          <section className="space-y-3">
            <SectionHead number="01" title="Hero image" />
            <ImageUpload
              ariaLabel="Hero image"
              value={{
                url: form.heroImageUrl,
                webpUrl: form.heroImageWebpUrl,
                alt: form.heroImageAlt,
              }}
              onChange={(next) =>
                update({
                  heroImageUrl: next.url,
                  heroImageWebpUrl: next.webpUrl,
                  heroImageAlt: next.alt,
                })
              }
            />
          </section>

          {/* Body */}
          <section className="space-y-3">
            <SectionHead number="02" title="Body" />
            <div className="border border-border bg-panel">
              <TipTapFullEditor
                ariaLabel="Page body"
                valueJson={form.bodyJson}
                onChangeJson={(json) => update({ bodyJson: json })}
                placeholder="Write the page body here…"
              />
              <footer
                className="flex items-center justify-between border-t border-border-soft px-4 py-2 font-mono text-[11px] text-muted"
                style={{ fontVariantNumeric: "tabular-nums" }}
              >
                <span>{wordCount} words</span>
                <span>{readMin}m {Math.round((wordCount / 220 - readMin) * 60)}s read</span>
              </footer>
            </div>
          </section>

          {/* SEO */}
          <section className="space-y-3">
            <SectionHead number="03" title="SEO & summary" />
            <Field label="Excerpt" hint="Optional. Auto-generated from the body's text if left blank.">
              <textarea
                value={form.excerpt}
                maxLength={500}
                onChange={(e) => update({ excerpt: e.target.value })}
                className="min-h-20 w-full border border-border bg-background p-2 text-sm focus-visible:border-accent focus-visible:outline-none"
              />
            </Field>
            <Field label="Meta description" hint="Used for search-engine snippets. Up to 300 chars.">
              <textarea
                value={form.metaDescription}
                maxLength={300}
                onChange={(e) => update({ metaDescription: e.target.value })}
                className="min-h-16 w-full border border-border bg-background p-2 text-sm focus-visible:border-accent focus-visible:outline-none"
              />
            </Field>
          </section>
        </div>

        {/* Right aside */}
        <aside className="space-y-6 lg:sticky lg:top-4 lg:self-start">
          <section className="border border-border bg-panel">
            <header className="border-b border-border-soft px-5 py-3">
              <h3 className="font-heading text-sm font-semibold">Publishing</h3>
            </header>
            <div className="space-y-4 p-5 text-sm">
              <div className="flex items-center justify-between">
                <span>Status</span>
                {form.isPublished
                  ? <Chip tone="success" dot>Published</Chip>
                  : <Chip tone="warn" dot>Draft</Chip>}
              </div>
              <div className="flex items-center justify-between">
                <span>Members only</span>
                <SwitchFlat
                  label="Members only"
                  checked={form.isMembersOnly}
                  onChange={(v) => update({ isMembersOnly: v })}
                />
              </div>
              <div className="flex items-center justify-between">
                <span>Published</span>
                <SwitchFlat
                  label="Published"
                  checked={form.isPublished}
                  onChange={(v) => update({ isPublished: v })}
                />
              </div>
              {original?.isSystemPage && (
                <p className="border-t border-border-soft pt-3 text-xs text-muted">
                  System page — slug locked
                </p>
              )}
            </div>
          </section>

          <section className="border border-border bg-panel">
            <header className="border-b border-border-soft px-5 py-3">
              <h3 className="font-heading text-sm font-semibold">Schedule</h3>
            </header>
            <div className="space-y-3 p-5 text-sm">
              <div>
                <p className="text-[11px] uppercase tracking-wider text-muted">Last saved</p>
                <p className="font-mono text-xs">{lastSavedLabel}</p>
              </div>
            </div>
          </section>

          {!isNew && original && !original.isDeleted && !original.isSystemPage && (
            <Btn
              type="button"
              variant="danger"
              size="lg"
              onClick={handleSoftDelete}
              className="w-full"
            >
              Delete page
            </Btn>
          )}
        </aside>
      </div>
    </form>
  );
}

function approxWordCount(json: string | null): number {
  if (!json) return 0;
  try {
    const parsed = JSON.parse(json);
    return countTextWords(parsed);
  } catch {
    return 0;
  }
}

function countTextWords(node: unknown): number {
  if (!node) return 0;
  if (typeof node !== "object") return 0;
  const n = node as { text?: string; content?: unknown[] };
  let total = 0;
  if (typeof n.text === "string") {
    total += n.text.split(/\s+/).filter(Boolean).length;
  }
  if (Array.isArray(n.content)) {
    for (const child of n.content) total += countTextWords(child);
  }
  return total;
}
