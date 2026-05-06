import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Plus } from "lucide-react";
import {
  adminClassOfferingsApi,
  adminClassSlotsApi,
  type AdminClassOffering,
  type AdminClassSlotDetail,
  type CreateClassSlotRequest,
  type UpdateClassSlotRequest,
} from "@/lib/api/classes";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import { slugify } from "@/lib/slug";
import {
  Btn,
  Chip,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";

interface FormState {
  slug: string;
  name: string;
  audienceAgeGroup: string;
  generalMeetingTime: string;
  defaultRoom: string;
  descriptionJson: string | null;
  imageBlobUrl: string | null;
  imageWebpBlobUrl: string | null;
  imageAltText: string | null;
  isActive: boolean;
  displayOrder: number;
}

const emptyForm: FormState = {
  slug: "",
  name: "",
  audienceAgeGroup: "",
  generalMeetingTime: "",
  defaultRoom: "",
  descriptionJson: null,
  imageBlobUrl: null,
  imageWebpBlobUrl: null,
  imageAltText: null,
  isActive: true,
  displayOrder: 0,
};

type Tab = "details" | "offerings";

export function ClassSlotEditorPage() {
  const { id } = useParams<{ id?: string }>();
  const isNew = !id || id === "new";
  const navigate = useNavigate();

  const [form, setForm] = useState<FormState>(emptyForm);
  const [original, setOriginal] = useState<AdminClassSlotDetail | null>(null);
  const [loading, setLoading] = useState(!isNew);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [activeTab, setActiveTab] = useState<Tab>("details");
  const [slugAutoGen, setSlugAutoGen] = useState(isNew);

  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    adminClassSlotsApi.get(id!)
      .then((s) => {
        if (cancelled) return;
        setOriginal(s);
        setForm({
          slug: s.slug,
          name: s.name,
          audienceAgeGroup: s.audienceAgeGroup,
          generalMeetingTime: s.generalMeetingTime ?? "",
          defaultRoom: s.defaultRoom ?? "",
          descriptionJson: s.descriptionJson,
          imageBlobUrl: s.imageBlobUrl,
          imageWebpBlobUrl: s.imageWebpBlobUrl,
          imageAltText: s.imageAltText,
          isActive: s.isActive,
          displayOrder: s.displayOrder,
        });
        setSlugAutoGen(false);
        setLoading(false);
      })
      .catch(() => {
        if (cancelled) return;
        setErrors(["Could not load class slot."]);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [id, isNew]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);

    const body: CreateClassSlotRequest | UpdateClassSlotRequest = {
      slug: form.slug,
      name: form.name,
      audienceAgeGroup: form.audienceAgeGroup,
      generalMeetingTime: form.generalMeetingTime || null,
      defaultRoom: form.defaultRoom || null,
      descriptionJson: form.descriptionJson,
      imageBlobUrl: form.imageBlobUrl,
      imageWebpBlobUrl: form.imageWebpBlobUrl,
      imageAltText: form.imageAltText,
      isActive: form.isActive,
      displayOrder: form.displayOrder,
    };

    try {
      if (isNew) {
        const created = await adminClassSlotsApi.create(body);
        navigate(`/admin/class-slots/${created.id}`);
      } else {
        const updated = await adminClassSlotsApi.update(id!, body);
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

  const handleDelete = async () => {
    if (!id) return;
    if (!window.confirm("Soft-delete this slot? Existing offerings remain in the database.")) return;
    await adminClassSlotsApi.softDelete(id);
    navigate("/admin/class-slots");
  };

  if (loading) return <p className="text-muted">Loading…</p>;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={isNew ? "New class slot" : `Editing · /${form.slug}`}
        title={form.name || "Untitled slot"}
        actions={
          !isNew && original && (
            <Btn variant="danger" onClick={handleDelete}>Delete slot</Btn>
          )
        }
      />

      {!isNew && (
        <nav className="flex border-b border-border" aria-label="Slot sections">
          <TabButton active={activeTab === "details"} onClick={() => setActiveTab("details")}>
            Details
          </TabButton>
          <TabButton active={activeTab === "offerings"} onClick={() => setActiveTab("offerings")}>
            Offerings
          </TabButton>
        </nav>
      )}

      {activeTab === "details" && (
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

          <section className="space-y-4">
            <SectionHead number="01" title="Identity" />
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label="Name" required>
                <input
                  required
                  value={form.name}
                  onChange={(e) => {
                    setForm({
                      ...form,
                      name: e.target.value,
                      slug: slugAutoGen ? slugify(e.target.value) : form.slug,
                    });
                  }}
                  className="input"
                />
              </Field>
              <Field
                label="Slug"
                required
                hint={slugAutoGen ? "Auto-generating from name." : "Editing manually."}
              >
                <input
                  required
                  value={form.slug}
                  onChange={(e) => { setSlugAutoGen(false); setForm({ ...form, slug: e.target.value }); }}
                  className="input"
                />
              </Field>
              <Field label="Audience age group" required hint='e.g. "Adults", "Youth", "Children"'>
                <input
                  required
                  value={form.audienceAgeGroup}
                  onChange={(e) => setForm({ ...form, audienceAgeGroup: e.target.value })}
                  className="input"
                />
              </Field>
              <Field label="Display order" hint="Lower numbers come first within an age group.">
                <input
                  type="number"
                  value={form.displayOrder}
                  onChange={(e) => setForm({ ...form, displayOrder: Number(e.target.value) || 0 })}
                  className="input"
                />
              </Field>
            </div>
          </section>

          <section className="space-y-4">
            <SectionHead number="02" title="Meeting" />
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label="General meeting time" hint="Public. Single-line summary.">
                <input
                  value={form.generalMeetingTime}
                  maxLength={200}
                  onChange={(e) => setForm({ ...form, generalMeetingTime: e.target.value })}
                  className="input"
                />
              </Field>
              <Field label="Default room" hint="Members-only. Hidden from anonymous viewers.">
                <input
                  value={form.defaultRoom}
                  maxLength={200}
                  onChange={(e) => setForm({ ...form, defaultRoom: e.target.value })}
                  className="input"
                />
              </Field>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              Slot is active (inactive slots are hidden from the public list)
            </label>
          </section>

          <section className="space-y-4">
            <SectionHead number="03" title="Image" />
            <ImageUpload
              ariaLabel="Slot image"
              value={{
                url: form.imageBlobUrl,
                webpUrl: form.imageWebpBlobUrl,
                alt: form.imageAltText,
              }}
              onChange={(next) => setForm({
                ...form,
                imageBlobUrl: next.url,
                imageWebpBlobUrl: next.webpUrl,
                imageAltText: next.alt,
              })}
            />
          </section>

          <section className="space-y-4">
            <SectionHead number="04" title="Description" />
            <TipTapEditor
              ariaLabel="Slot description"
              valueJson={form.descriptionJson}
              onChangeJson={(json) => setForm({ ...form, descriptionJson: json })}
              placeholder="Describe who this class is for…"
            />
          </section>

          <div>
            <Btn type="submit" variant="accent" size="lg" disabled={submitting}>
              {submitting ? "Saving…" : isNew ? "Create slot" : "Save changes"}
            </Btn>
          </div>

          <Styles />
        </form>
      )}

      {activeTab === "offerings" && id && <OfferingsTab slotId={id} />}
    </div>
  );
}

// ---- offerings sub-tab ---------------------------------------------------

function OfferingsTab({ slotId }: { slotId: string }) {
  const navigate = useNavigate();
  const [offerings, setOfferings] = useState<AdminClassOffering[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    adminClassOfferingsApi
      .list({ classSlotId: slotId })
      .then((d) => { if (!cancelled) { setOfferings(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load offerings."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [slotId]);

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Offerings ({offerings?.length ?? 0})</h2>
        <Btn
          iconLeft={<Plus className="h-4 w-4" />}
          variant="accent"
          onClick={() => navigate(`/admin/class-offerings/new?slotId=${slotId}`)}
        >
          New offering
        </Btn>
      </div>

      {loading && <p className="text-muted">Loading…</p>}
      {error && <p className="text-danger">{error}</p>}
      {!loading && offerings && offerings.length === 0 && (
        <p className="text-muted">No offerings yet for this slot.</p>
      )}

      {!loading && offerings && offerings.length > 0 && (
        <ul className="divide-y border border-border bg-panel">
          {offerings.map((o) => {
            const start = new Date(o.startDate);
            const end = new Date(o.endDate);
            const status = end < today ? "past" : start > today ? "upcoming" : "current";
            return (
              <li key={o.id} className="flex flex-wrap items-center gap-3 px-5 py-3">
                <Chip
                  tone={status === "current" ? "success" : status === "upcoming" ? "accent" : "muted"}
                  dot={status !== "past"}
                >
                  {status[0].toUpperCase() + status.slice(1)}
                </Chip>
                <div className="min-w-0 flex-1">
                  <Link
                    to={`/admin/class-offerings/${o.id}`}
                    className="font-semibold hover:underline"
                  >
                    {o.subject}
                  </Link>
                  <p className="font-mono text-xs text-muted">
                    {o.startDate} → {o.endDate}
                  </p>
                </div>
                {o.teacherFreeText && (
                  <span className="text-sm text-muted">{o.teacherFreeText}</span>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}

// ---- bits ----------------------------------------------------------------

function TabButton({
  active, onClick, children,
}: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={
        "px-4 py-3 text-sm transition-colors " +
        (active
          ? "border-b-2 border-accent font-semibold text-foreground"
          : "text-muted hover:text-foreground")
      }
    >
      {children}
    </button>
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
