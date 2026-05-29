import { useEffect, useMemo, useState } from "react";
import { leadersApi } from "@/lib/api/leaders";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { useAuth } from "@/hooks/useAuth";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import type { CreateLeaderRequest, Leader } from "@/types/api";

interface FormState {
  fullName: string;
  title: string;
  category: string;
  bioJson: string | null;
  email: string;
  photoUrl: string | null;
  photoWebpUrl: string | null;
  photoAlt: string | null;
  displayOrder: number;
}

export function LeadersPage() {
  const { settings } = useSiteSettings();
  const { hasAnyRole } = useAuth();
  const isAdmin = hasAnyRole(["Administrator"]);

  // Categories come from Site Settings.
  const categories = useMemo<string[]>(() => {
    // settings is the public DTO (no LeaderCategoriesJson). Pull from the
    // public list response instead — categories on existing leaders are
    // authoritative until we wire the admin DTO call.
    return DEFAULT_CATEGORIES;
  }, [settings]);

  const [items, setItems] = useState<Leader[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<Leader | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm(categories[0]));
  const [errors, setErrors] = useState<string[]>([]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    leadersApi.list()
      .then((d) => { if (!cancelled) setItems(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  const startNew = () => { setEditing(null); setForm(emptyForm(categories[0])); setErrors([]); };
  const startEdit = (l: Leader) => {
    setEditing(l);
    setForm({
      fullName: l.fullName, title: l.title ?? "",
      category: l.category, bioJson: l.bioJson, email: l.email ?? "",
      photoUrl: l.photoUrl, photoWebpUrl: l.photoWebpUrl, photoAlt: l.photoAlt,
      displayOrder: l.displayOrder,
    });
    setErrors([]);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const req: CreateLeaderRequest = {
      fullName: form.fullName, title: form.title || null, category: form.category,
      bioJson: form.bioJson, email: form.email || null,
      photoUrl: form.photoUrl, photoWebpUrl: form.photoWebpUrl, photoAlt: form.photoAlt,
      displayOrder: Number(form.displayOrder),
    };
    try {
      const saved = editing
        ? await leadersApi.update(editing.id, req)
        : await leadersApi.create(req);
      setItems((items) => {
        const without = items.filter((x) => x.id !== saved.id);
        return [...without, saved].sort(byCategoryOrder);
      });
      startNew();
    } catch (err) {
      setErrors(getMessages(err));
    }
  };

  const handleDelete = async (l: Leader) => {
    if (!isAdmin) return;
    if (!window.confirm(`Permanently delete ${l.fullName}? This cannot be undone.`)) return;
    await leadersApi.delete(l.id);
    setItems((items) => items.filter((x) => x.id !== l.id));
    if (editing?.id === l.id) startNew();
  };

  const grouped = categories.map((c) => ({
    category: c,
    items: items.filter((l) => l.category === c).sort(byCategoryOrder),
  }));
  const uncategorized = items.filter((l) => !categories.includes(l.category));

  return (
    <div>
      <h1 className="text-2xl font-bold">Leaders</h1>

      <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_400px]">
        <div>
          {loading && <p className="text-muted">Loading…</p>}
          {!loading && items.length === 0 && (
            <p className="text-muted">No leaders yet.</p>
          )}
          {grouped.filter((g) => g.items.length > 0).map((g) => (
            <CategoryBlock key={g.category} title={g.category}
              items={g.items} onEdit={startEdit} onDelete={isAdmin ? handleDelete : null} />
          ))}
          {uncategorized.length > 0 && (
            <CategoryBlock title="Uncategorized" items={uncategorized}
              onEdit={startEdit} onDelete={isAdmin ? handleDelete : null} />
          )}
        </div>

        <form onSubmit={submit} className="space-y-3 rounded-lg border bg-card p-4">
          <h2 className="text-lg font-semibold">{editing ? "Edit leader" : "New leader"}</h2>

          {errors.length > 0 && (
            <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-2 text-xs text-danger">
              <ul className="list-disc pl-4">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
            </div>
          )}

          <Field label="Photo">
            <ImageUpload
              ariaLabel="Leader photo"
              value={{ url: form.photoUrl, webpUrl: form.photoWebpUrl, alt: form.photoAlt }}
              onChange={(next) => setForm({
                ...form, photoUrl: next.url, photoWebpUrl: next.webpUrl, photoAlt: next.alt,
              })}
            />
          </Field>
          <Field label="Full name" required>
            <input value={form.fullName} required maxLength={150}
              onChange={(e) => setForm({ ...form, fullName: e.target.value })} className="input" />
          </Field>
          <Field label="Title">
            <input value={form.title} maxLength={150}
              onChange={(e) => setForm({ ...form, title: e.target.value })} className="input" />
          </Field>
          <div className="grid grid-cols-2 gap-2">
            <Field label="Category" required>
              <select value={form.category} required
                onChange={(e) => setForm({ ...form, category: e.target.value })}
                className="input">
                {categories.map((c) => <option key={c} value={c}>{c}</option>)}
              </select>
            </Field>
            <Field label="Order">
              <input
                type="number"
                min={0}
                step={1}
                value={form.displayOrder}
                onChange={(e) => {
                  const n = Number(e.target.value);
                  setForm({ ...form, displayOrder: Number.isFinite(n) && n >= 0 ? n : 0 });
                }}
                className="input"
              />
            </Field>
          </div>
          <Field label="Email">
            <input type="email" value={form.email} maxLength={254}
              onChange={(e) => setForm({ ...form, email: e.target.value })} className="input" />
          </Field>
          <Field label="Bio">
            <TipTapEditor
              ariaLabel="Leader bio"
              valueJson={form.bioJson}
              onChangeJson={(json) => setForm({ ...form, bioJson: json })}
              placeholder="Short bio…"
            />
          </Field>

          <div className="flex gap-2">
            <button type="submit"
              className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
              {editing ? "Save changes" : "Create"}
            </button>
            {editing && (
              <button type="button" onClick={startNew}
                className="inline-flex h-10 items-center justify-center rounded-md border bg-card px-3 text-sm hover:bg-panel-alt">
                Cancel
              </button>
            )}
          </div>

          <style>{`
            .input { height: 2.5rem; width: 100%; border-radius: 0.375rem;
              border: 1px solid hsl(var(--input)); background: hsl(var(--background));
              padding: 0 0.75rem; font-size: 0.875rem; }
          `}</style>
        </form>
      </div>
    </div>
  );
}

function CategoryBlock({ title, items, onEdit, onDelete }: {
  title: string;
  items: Leader[];
  onEdit: (l: Leader) => void;
  onDelete: ((l: Leader) => void) | null;
}) {
  return (
    <div className="mb-6">
      <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-muted">{title}</h2>
      <ul className="grid gap-3 sm:grid-cols-2">
        {items.map((l) => (
          <li key={l.id} className="flex gap-3 rounded-lg border bg-card p-3">
            {l.photoUrl ? (
              <picture>
                {l.photoWebpUrl && <source srcSet={l.photoWebpUrl} type="image/webp" />}
                <img src={l.photoUrl} alt={l.photoAlt ?? l.fullName}
                  className="h-16 w-16 rounded-full object-cover" />
              </picture>
            ) : (
              <div className="h-16 w-16 rounded-full bg-panel-alt" />
            )}
            <div className="flex-1">
              <button type="button" onClick={() => onEdit(l)}
                className="block text-left font-semibold hover:underline">
                {l.fullName}
              </button>
              {l.title && <p className="text-xs text-muted">{l.title}</p>}
              {onDelete && (
                <button type="button" onClick={() => onDelete(l)}
                  className="mt-1 text-xs text-danger hover:underline">
                  Delete
                </button>
              )}
            </div>
          </li>
        ))}
      </ul>
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

const DEFAULT_CATEGORIES = ["Ministers", "Staff", "Elders", "Deacons"];

function emptyForm(category: string): FormState {
  return {
    fullName: "", title: "", category, bioJson: null, email: "",
    photoUrl: null, photoWebpUrl: null, photoAlt: null, displayOrder: 0,
  };
}

function byCategoryOrder(a: Leader, b: Leader) {
  if (a.category !== b.category) return a.category.localeCompare(b.category);
  if (a.displayOrder !== b.displayOrder) return a.displayOrder - b.displayOrder;
  return a.fullName.localeCompare(b.fullName);
}

function getMessages(err: unknown): string[] {
  return typeof err === "object" && err !== null && "getMessages" in err
    ? (err as { getMessages: () => string[] }).getMessages()
    : ["Save failed."];
}
