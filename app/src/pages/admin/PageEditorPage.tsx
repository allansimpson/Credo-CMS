import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Eye, Globe, History, AlertTriangle } from "lucide-react";
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
  template: import("@/types/api").PageTemplate;
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
  template: "Standard",
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
  const [pendingNavTarget, setPendingNavTarget] = useState<string | null>(null);

  const applyPageToForm = useCallback((p: PageDetail) => {
    // If an unpublished draft exists, the editor loads draft fields so the
    // admin picks up where they left off. Live columns are still visible
    // via the "What visitors see" indicator in the sidebar.
    const src = p.hasUnpublishedDraft && p.draft ? p.draft : p;
    setForm({
      slug: p.slug,
      title: src.title,
      bodyJson: src.bodyJson,
      excerpt: src.excerpt ?? "",
      heroImageUrl: src.heroImageUrl,
      heroImageWebpUrl: src.heroImageWebpUrl,
      heroImageAlt: src.heroImageAlt,
      metaDescription: src.metaDescription ?? "",
      isPublished: p.isPublished,
      isMembersOnly: src.isMembersOnly,
      template: src.template ?? "Standard",
    });
  }, []);

  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    pagesApi.get(id!).then((p) => {
      if (cancelled) return;
      setOriginal(p);
      applyPageToForm(p);
      setSlugAutoGen(false);
      setDirty(false);
      setLoading(false);
    }).catch(() => {
      if (cancelled) return;
      setErrors(["Could not load page."]);
      setLoading(false);
    });
    return () => { cancelled = true; };
  }, [id, isNew, applyPageToForm]);

  const update = (patch: Partial<FormState>) => {
    setForm((f) => ({ ...f, ...patch }));
    setDirty(true);
    setSuccess(false);
  };

  // ── Unsaved-changes guard ──────────────────────────────────────────────
  // Tab close / refresh: browser-native beforeunload prompt.
  useEffect(() => {
    if (!dirty) return;
    const handler = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      // Setting returnValue is what triggers the prompt in modern browsers.
      e.returnValue = "";
    };
    window.addEventListener("beforeunload", handler);
    return () => window.removeEventListener("beforeunload", handler);
  }, [dirty]);

  // In-app navigation: intercept anchor clicks that would leave the editor.
  // React Router's useBlocker isn't available without a data-router setup,
  // so we listen at the document level. External / new-tab / hash links and
  // anchors with explicit data-allow-nav are passed through.
  useEffect(() => {
    if (!dirty) return;
    const handler = (e: MouseEvent) => {
      if (e.defaultPrevented) return;
      if (e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
      const anchor = (e.target as HTMLElement | null)?.closest("a");
      if (!anchor) return;
      const href = anchor.getAttribute("href");
      if (!href) return;
      if (anchor.target === "_blank") return;
      if (href.startsWith("http://") || href.startsWith("https://") || href.startsWith("//")) return;
      if (href.startsWith("#") || href.startsWith("mailto:") || href.startsWith("tel:")) return;
      if (anchor.dataset.allowNav === "true") return;
      // Same-document SPA link with pending changes — intercept.
      e.preventDefault();
      setPendingNavTarget(href);
    };
    document.addEventListener("click", handler, true);
    return () => document.removeEventListener("click", handler, true);
  }, [dirty]);

  const cancelPendingNav = () => setPendingNavTarget(null);

  const continueWithoutSaving = () => {
    const target = pendingNavTarget;
    setDirty(false);
    setPendingNavTarget(null);
    if (target) navigate(target);
  };

  const saveDraftAndContinue = async () => {
    const target = pendingNavTarget;
    const saved = await saveDraft();
    if (saved && target) {
      setPendingNavTarget(null);
      navigate(target);
    } else if (!saved) {
      // Save failed — keep the modal open so the user can retry or discard.
    }
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
    template: form.template,
  });

  /** Save the current form. For a published page, the backend stashes the
   * edits in the Draft* columns so the live page is untouched. For an
   * unpublished page (or a brand-new one), edits go straight to the live
   * columns since there's no live version to protect. */
  const saveDraft = async (): Promise<PageDetail | null> => {
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);
    const body = buildBody();
    try {
      if (isNew) {
        const created = await pagesApi.create(body);
        navigate(`/admin/pages/${created.id}`);
        return created;
      }
      const updated = await pagesApi.update(id!, body);
      setOriginal(updated);
      applyPageToForm(updated);
      setSuccess(true);
      setDirty(false);
      return updated;
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Save failed."];
      setErrors(messages);
      return null;
    } finally {
      setSubmitting(false);
    }
  };

  /** Save current edits then promote to live. Two requests: update writes
   * the draft, publish copies draft → live. */
  const publishNow = async () => {
    if (isNew) {
      // Brand-new page: create published immediately.
      const created = await saveDraft();
      if (!created) return;
      await pagesApi.publish(created.id).catch(() => {/* surfaced by next get */});
      const fresh = await pagesApi.get(created.id);
      setOriginal(fresh);
      applyPageToForm(fresh);
      setSuccess(true);
      setDirty(false);
      return;
    }
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);
    try {
      if (dirty) {
        const updated = await pagesApi.update(id!, buildBody());
        setOriginal(updated);
      }
      const published = await pagesApi.publish(id!);
      setOriginal(published);
      applyPageToForm(published);
      setSuccess(true);
      setDirty(false);
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Publish failed."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  const discardDraft = async () => {
    if (!id || !original?.hasUnpublishedDraft) return;
    if (!window.confirm("Discard pending draft changes? The live page will stay as it is.")) return;
    setSubmitting(true);
    try {
      const updated = await pagesApi.discardDraft(id);
      setOriginal(updated);
      applyPageToForm(updated);
      setSuccess(true);
      setDirty(false);
    } finally {
      setSubmitting(false);
    }
  };

  const unpublish = async () => {
    if (!id || !original?.isPublished) return;
    if (!window.confirm("Take the live page offline? Visitors will see a 404 at /" + form.slug + " until you publish again.")) return;
    setSubmitting(true);
    try {
      const updated = await pagesApi.unpublish(id);
      setOriginal(updated);
      applyPageToForm(updated);
      setSuccess(true);
      setDirty(false);
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
          {!dirty && original?.hasUnpublishedDraft && <Chip tone="warn" dot>Draft pending</Chip>}
          {success && <Chip tone="success" dot>Saved</Chip>}
          <span className="font-mono text-[11px] text-muted">
            Last saved · {lastSavedLabel}
          </span>
          <Btn iconLeft={<History className="h-3.5 w-3.5" />}>History</Btn>
          {!isNew && (
            <Btn
              iconLeft={<Eye className="h-3.5 w-3.5" />}
              onClick={() => window.open(`/${form.slug}?preview=1`, "_blank")}
            >
              Preview
            </Btn>
          )}
          {!isNew && original?.hasUnpublishedDraft && (
            <Btn
              type="button"
              disabled={submitting}
              onClick={() => void discardDraft()}
            >
              Discard draft
            </Btn>
          )}
          <Btn
            type="submit"
            disabled={submitting || (!dirty && !isNew)}
            onClick={() => void saveDraft()}
          >
            {submitting ? "Saving…" : "Save draft"}
          </Btn>
          <Btn
            type="button"
            variant="accent"
            disabled={submitting || (!isNew && !dirty && !original?.hasUnpublishedDraft && original?.isPublished === true)}
            onClick={() => void publishNow()}
          >
            {original?.isPublished && original?.hasUnpublishedDraft
              ? "Publish changes"
              : "Publish"}
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
                <span>Live status</span>
                {original?.isPublished
                  ? <Chip tone="success" dot>Published</Chip>
                  : <Chip tone="warn" dot>Draft</Chip>}
              </div>
              {original?.hasUnpublishedDraft && (
                <div className="flex items-center justify-between border-t border-border-soft pt-3">
                  <span className="text-xs">
                    Pending draft saved{" "}
                    {original.draft?.savedAt
                      ? new Date(original.draft.savedAt).toLocaleString()
                      : ""}
                  </span>
                </div>
              )}
              <div className="flex items-center justify-between">
                <span>Members only</span>
                <SwitchFlat
                  label="Members only"
                  checked={form.isMembersOnly}
                  onChange={(v) => update({ isMembersOnly: v })}
                />
              </div>
              {!isNew && original?.isPublished && (
                <button
                  type="button"
                  onClick={() => void unpublish()}
                  disabled={submitting}
                  className="block w-full border border-danger/30 bg-card px-3 py-2 text-xs font-medium text-danger hover:bg-danger/10 disabled:opacity-50"
                >
                  Unpublish
                </button>
              )}
              {original?.isSystemPage && (
                <p className="border-t border-border-soft pt-3 text-xs text-muted">
                  System page — slug locked
                </p>
              )}
            </div>
          </section>

          <section className="border border-border bg-panel">
            <header className="border-b border-border-soft px-5 py-3">
              <h3 className="font-heading text-sm font-semibold">Template</h3>
            </header>
            <div className="p-5">
              <select
                aria-label="Page template"
                value={form.template}
                onChange={(e) => update({ template: e.target.value as FormState["template"] })}
                className="w-full border border-border bg-background px-3 py-2 text-sm focus-visible:border-accent focus-visible:outline-none"
              >
                <option value="Standard">Standard</option>
                <option value="About">About</option>
                <option value="ImNew">I'm New</option>
                <option value="Beliefs">Beliefs</option>
                <option value="Contact">Contact</option>
              </select>
              <p className="mt-2 text-[11px] text-muted">
                Controls the public page layout. Standard uses the default rich-text renderer.
              </p>
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

      {pendingNavTarget && (
        <UnsavedChangesModal
          submitting={submitting}
          onStay={cancelPendingNav}
          onDiscard={continueWithoutSaving}
          onSaveAndLeave={() => void saveDraftAndContinue()}
        />
      )}
    </form>
  );
}

function UnsavedChangesModal({
  submitting,
  onStay,
  onDiscard,
  onSaveAndLeave,
}: {
  submitting: boolean;
  onStay: () => void;
  onDiscard: () => void;
  onSaveAndLeave: () => void;
}) {
  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="unsaved-changes-title"
      className="fixed inset-0 z-50 flex items-center justify-center bg-foreground/40 p-4"
    >
      <div className="w-full max-w-md border bg-popover text-foreground shadow-xl">
        <div className="flex items-start gap-3 border-b border-border-soft px-5 py-4">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-warn" />
          <div className="min-w-0">
            <h2 id="unsaved-changes-title" className="font-heading text-base font-semibold">
              Unsaved changes
            </h2>
            <p className="mt-1 text-sm text-fg-soft">
              You have edits that haven&rsquo;t been saved. Save them as a draft to come
              back to later, discard them, or stay on this page.
            </p>
          </div>
        </div>
        <div className="flex flex-wrap items-center justify-end gap-2 px-5 py-3">
          <button
            type="button"
            onClick={onStay}
            disabled={submitting}
            className="border bg-card px-3 py-2 text-xs font-medium hover:bg-panel-alt disabled:opacity-50"
          >
            Stay on this page
          </button>
          <button
            type="button"
            onClick={onDiscard}
            disabled={submitting}
            className="border border-danger/30 bg-card px-3 py-2 text-xs font-medium text-danger hover:bg-danger/10 disabled:opacity-50"
          >
            Discard changes
          </button>
          <button
            type="button"
            onClick={onSaveAndLeave}
            disabled={submitting}
            className="bg-primary px-3 py-2 text-xs font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {submitting ? "Saving…" : "Save as draft & leave"}
          </button>
        </div>
      </div>
    </div>
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
