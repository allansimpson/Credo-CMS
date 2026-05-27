import { useEffect, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  adminClassOfferingsApi,
  adminClassSlotsApi,
  type AdminClassOffering,
  type AdminClassSlotListItem,
  type CreateClassOfferingRequest,
  type UpdateClassOfferingRequest,
} from "@/lib/api/classes";
import { leadersApi } from "@/lib/api/leaders";
import type { Leader } from "@/types/api";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import {
  Btn,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";

interface FormState {
  classSlotId: string;
  subject: string;
  descriptionJson: string | null;
  startDate: string;
  endDate: string;
  teacherMode: "leader" | "freeText";
  teacherLeaderId: string;
  teacherFreeText: string;
  detailedScheduleJson: string | null;
  materialsNeeded: string;
}

const today = new Date().toISOString().slice(0, 10);
const todayPlus30 = new Date(Date.now() + 30 * 86_400_000).toISOString().slice(0, 10);

const emptyForm: FormState = {
  classSlotId: "",
  subject: "",
  descriptionJson: null,
  startDate: today,
  endDate: todayPlus30,
  teacherMode: "leader",
  teacherLeaderId: "",
  teacherFreeText: "",
  detailedScheduleJson: null,
  materialsNeeded: "",
};

export function ClassOfferingEditorPage() {
  const { id } = useParams<{ id?: string }>();
  const isNew = !id || id === "new";
  const navigate = useNavigate();
  const [params] = useSearchParams();

  const [form, setForm] = useState<FormState>({
    ...emptyForm,
    classSlotId: params.get("slotId") ?? "",
  });
  const [original, setOriginal] = useState<AdminClassOffering | null>(null);
  const [slots, setSlots] = useState<AdminClassSlotListItem[]>([]);
  const [leaders, setLeaders] = useState<Leader[]>([]);
  const [loading, setLoading] = useState(!isNew);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    Promise.all([
      adminClassSlotsApi.list(undefined, true).catch(() => []),
      leadersApi.list().catch(() => []),
    ]).then(([s, l]) => { setSlots(s); setLeaders(l); });
  }, []);

  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    adminClassOfferingsApi
      .get(id!)
      .then((o) => {
        if (cancelled) return;
        setOriginal(o);
        setForm({
          classSlotId: o.classSlotId,
          subject: o.subject,
          descriptionJson: o.descriptionJson,
          startDate: o.startDate,
          endDate: o.endDate,
          teacherMode: o.teacherLeaderId ? "leader" : "freeText",
          teacherLeaderId: o.teacherLeaderId ?? "",
          teacherFreeText: o.teacherFreeText ?? "",
          detailedScheduleJson: o.detailedScheduleJson,
          materialsNeeded: o.materialsNeeded ?? "",
        });
        setLoading(false);
      })
      .catch(() => {
        if (cancelled) return;
        setErrors(["Could not load offering."]);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [id, isNew]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true); setErrors([]); setSuccess(false);

    const teacherLeaderId = form.teacherMode === "leader" ? (form.teacherLeaderId || null) : null;
    const teacherFreeText = form.teacherMode === "freeText"
      ? (form.teacherFreeText.trim() || null)
      : null;

    const body: CreateClassOfferingRequest | UpdateClassOfferingRequest = {
      classSlotId: form.classSlotId,
      subject: form.subject,
      descriptionJson: form.descriptionJson,
      startDate: form.startDate,
      endDate: form.endDate,
      teacherLeaderId,
      teacherFreeText,
      detailedScheduleJson: form.detailedScheduleJson,
      materialsNeeded: form.materialsNeeded || null,
    };

    try {
      if (isNew) {
        const created = await adminClassOfferingsApi.create(body);
        navigate(`/admin/class-offerings/${created.id}`);
      } else {
        const updated = await adminClassOfferingsApi.update(id!, body);
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
    if (!window.confirm("Soft-delete this offering?")) return;
    await adminClassOfferingsApi.softDelete(id);
    navigate("/admin/class-offerings");
  };

  if (loading) return <p className="text-muted">Loading…</p>;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={isNew ? "New offering" : "Editing offering"}
        title={form.subject || "Untitled offering"}
        actions={
          !isNew && original && (
            <Btn variant="danger" onClick={handleDelete}>Delete offering</Btn>
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

        <section className="space-y-4">
          <SectionHead number="01" title="Slot & subject" />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Class slot" required>
              <select
                required
                value={form.classSlotId}
                onChange={(e) => setForm({ ...form, classSlotId: e.target.value })}
                className="input"
              >
                <option value="">— Choose a slot —</option>
                {slots.map((s) => (
                  <option key={s.id} value={s.id}>{s.name} · {s.audienceAgeGroup}</option>
                ))}
              </select>
            </Field>
            <Field label="Subject" required hint="e.g. Romans · Marriage & Family · Genesis 1–11">
              <input
                required
                value={form.subject}
                maxLength={200}
                onChange={(e) => setForm({ ...form, subject: e.target.value })}
                className="input"
              />
            </Field>
          </div>
        </section>

        <section className="space-y-4">
          <SectionHead number="02" title="Date range" />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label="Start date" required>
              <input
                type="date"
                required
                value={form.startDate}
                onChange={(e) => setForm({ ...form, startDate: e.target.value })}
                className="input"
              />
            </Field>
            <Field label="End date" required hint="Must be on or after the start date.">
              <input
                type="date"
                required
                value={form.endDate}
                onChange={(e) => setForm({ ...form, endDate: e.target.value })}
                className="input"
              />
            </Field>
          </div>
        </section>

        <section className="space-y-4">
          <SectionHead number="03" title="Teacher (members-only)" />
          <fieldset className="space-y-3">
            <legend className="sr-only">Teacher source</legend>
            <label className="flex items-start gap-3 text-sm">
              <input
                type="radio"
                name="teacherMode"
                checked={form.teacherMode === "leader"}
                onChange={() => setForm({ ...form, teacherMode: "leader" })}
                className="mt-1"
              />
              <span>
                <span className="block font-medium">Pick a Leader</span>
                <span className="text-xs text-muted">Links to a Leader profile.</span>
              </span>
            </label>
            <label className="flex items-start gap-3 text-sm">
              <input
                type="radio"
                name="teacherMode"
                checked={form.teacherMode === "freeText"}
                onChange={() => setForm({ ...form, teacherMode: "freeText" })}
                className="mt-1"
              />
              <span>
                <span className="block font-medium">Free text</span>
                <span className="text-xs text-muted">Use when the teacher isn't a Leader profile (e.g. visiting speaker).</span>
              </span>
            </label>
          </fieldset>

          {form.teacherMode === "leader" ? (
            <Field label="Teacher (Leader)">
              <select
                value={form.teacherLeaderId}
                onChange={(e) => setForm({ ...form, teacherLeaderId: e.target.value })}
                className="input"
              >
                <option value="">— No teacher set —</option>
                {leaders.map((l) => (
                  <option key={l.id} value={l.id}>{l.fullName}{l.title ? ` · ${l.title}` : ""}</option>
                ))}
              </select>
            </Field>
          ) : (
            <Field label="Teacher (free text)">
              <input
                value={form.teacherFreeText}
                maxLength={200}
                onChange={(e) => setForm({ ...form, teacherFreeText: e.target.value })}
                className="input"
              />
            </Field>
          )}
        </section>

        <section className="space-y-4">
          <SectionHead number="04" title="Description (public)" />
          <TipTapEditor
            ariaLabel="Offering description"
            valueJson={form.descriptionJson}
            onChangeJson={(json) => setForm({ ...form, descriptionJson: json })}
            placeholder="What is this curriculum about?"
          />
        </section>

        <section className="space-y-4">
          <SectionHead number="05" title="Detailed schedule (members-only)" />
          <TipTapEditor
            ariaLabel="Detailed weekly schedule"
            valueJson={form.detailedScheduleJson}
            onChangeJson={(json) => setForm({ ...form, detailedScheduleJson: json })}
            placeholder="Week 1: …"
          />
        </section>

        <section className="space-y-4">
          <SectionHead number="06" title="Materials (members-only)" />
          <Field label="Materials needed" hint="Plain text. Up to 1000 chars.">
            <textarea
              value={form.materialsNeeded}
              maxLength={1000}
              onChange={(e) => setForm({ ...form, materialsNeeded: e.target.value })}
              className="min-h-24 w-full border border-border bg-background p-2 text-sm focus-visible:border-accent focus-visible:outline-none"
            />
          </Field>
        </section>

        <div>
          <Btn type="submit" variant="accent" size="lg" disabled={submitting}>
            {submitting ? "Saving…" : isNew ? "Create offering" : "Save changes"}
          </Btn>
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
