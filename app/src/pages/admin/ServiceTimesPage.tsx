import { useEffect, useState } from "react";
import { serviceTimesApi } from "@/lib/api/serviceTimes";
import type {
  CreateServiceTimeRequest,
  DayOfWeek,
  ServiceTime,
} from "@/types/api";

const DAYS: { value: DayOfWeek; label: string }[] = [
  { value: 0, label: "Sunday" }, { value: 1, label: "Monday" },
  { value: 2, label: "Tuesday" }, { value: 3, label: "Wednesday" },
  { value: 4, label: "Thursday" }, { value: 5, label: "Friday" },
  { value: 6, label: "Saturday" },
];

interface FormState {
  name: string;
  dayOfWeek: DayOfWeek;
  startTime: string;   // "HH:mm"
  endTime: string;     // "HH:mm" or empty
  location: string;
  notes: string;
  displayOrder: number;
  isActive: boolean;
}

const emptyForm: FormState = {
  name: "", dayOfWeek: 0, startTime: "09:00", endTime: "", location: "", notes: "",
  displayOrder: 0, isActive: true,
};

export function ServiceTimesPage() {
  const [items, setItems] = useState<ServiceTime[]>([]);
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [editing, setEditing] = useState<ServiceTime | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    serviceTimesApi.list(includeDeleted)
      .then((d) => { if (!cancelled) setItems(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [includeDeleted]);

  const startNew = () => { setEditing(null); setForm(emptyForm); setErrors([]); };

  const startEdit = (s: ServiceTime) => {
    setEditing(s);
    setForm({
      name: s.name, dayOfWeek: s.dayOfWeek,
      startTime: s.startTime.slice(0, 5),
      endTime: s.endTime ? s.endTime.slice(0, 5) : "",
      location: s.location ?? "", notes: s.notes ?? "",
      displayOrder: s.displayOrder, isActive: s.isActive,
    });
    setErrors([]);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const req: CreateServiceTimeRequest = {
      name: form.name, dayOfWeek: form.dayOfWeek,
      startTime: form.startTime + ":00",
      endTime: form.endTime ? form.endTime + ":00" : null,
      location: form.location || null, notes: form.notes || null,
      displayOrder: Number(form.displayOrder), isActive: form.isActive,
    };
    try {
      const saved = editing
        ? await serviceTimesApi.update(editing.id, req)
        : await serviceTimesApi.create(req);
      setItems((items) => {
        const without = items.filter((x) => x.id !== saved.id);
        return [...without, saved].sort((a, b) =>
          a.dayOfWeek - b.dayOfWeek || a.displayOrder - b.displayOrder
        );
      });
      startNew();
    } catch (err) {
      const m = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Save failed."];
      setErrors(m);
    }
  };

  const softDelete = async (s: ServiceTime) => {
    if (!window.confirm(`Soft-delete ${s.name}?`)) return;
    await serviceTimesApi.softDelete(s.id);
    setItems((items) => items.filter((x) => x.id !== s.id));
    if (editing?.id === s.id) startNew();
  };

  const restore = async (s: ServiceTime) => {
    const restored = await serviceTimesApi.restore(s.id);
    setItems((items) => items.map((x) => x.id === restored.id ? restored : x));
  };

  const grouped = DAYS.map(d => ({
    day: d,
    items: items.filter((s) => s.dayOfWeek === d.value)
      .sort((a, b) => a.displayOrder - b.displayOrder),
  }));

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Service Times</h1>
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={includeDeleted}
            onChange={(e) => setIncludeDeleted(e.target.checked)}
          />
          Show deleted
        </label>
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_360px]">
        <div>
          {loading && <p className="text-muted-foreground">Loading…</p>}
          {!loading && items.length === 0 && (
            <p className="text-muted-foreground">No service times yet. Add one to get started.</p>
          )}
          {grouped.filter((g) => g.items.length > 0).map((g) => (
            <div key={g.day.value} className="mb-6">
              <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-muted-foreground">{g.day.label}</h2>
              <ul className="divide-y rounded-lg border bg-card">
                {g.items.map((s) => (
                  <li key={s.id} className="flex flex-col gap-2 p-3 sm:flex-row sm:items-center sm:gap-4">
                    <div className="flex-1">
                      <button
                        type="button"
                        onClick={() => startEdit(s)}
                        className="text-left font-semibold hover:underline"
                      >
                        {s.name}
                      </button>
                      <p className="text-xs text-muted-foreground">
                        {s.startTime.slice(0, 5)}{s.endTime && ` – ${s.endTime.slice(0, 5)}`}
                        {s.location && ` · ${s.location}`}
                        {!s.isActive && " · Inactive"}
                        {s.isDeleted && " · Deleted"}
                      </p>
                    </div>
                    {s.isDeleted ? (
                      <button type="button" onClick={() => restore(s)}
                        className="text-xs font-semibold text-emerald-700 hover:underline">
                        Restore
                      </button>
                    ) : (
                      <button type="button" onClick={() => softDelete(s)}
                        className="text-xs text-destructive hover:underline">
                        Delete
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <form onSubmit={submit} className="space-y-3 rounded-lg border bg-card p-4">
          <h2 className="text-lg font-semibold">{editing ? "Edit service time" : "New service time"}</h2>

          {errors.length > 0 && (
            <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-2 text-xs text-destructive">
              <ul className="list-disc pl-4">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
            </div>
          )}

          <Field label="Name" required>
            <input value={form.name} required maxLength={150}
              onChange={(e) => setForm({ ...form, name: e.target.value })} className="input" />
          </Field>
          <div className="grid grid-cols-2 gap-2">
            <Field label="Day">
              <select value={form.dayOfWeek}
                onChange={(e) => setForm({ ...form, dayOfWeek: Number(e.target.value) as DayOfWeek })}
                className="input">
                {DAYS.map((d) => <option key={d.value} value={d.value}>{d.label}</option>)}
              </select>
            </Field>
            <Field label="Order"><input type="number" value={form.displayOrder}
              onChange={(e) => setForm({ ...form, displayOrder: Number(e.target.value) })} className="input" /></Field>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <Field label="Start">
              <input type="time" value={form.startTime} required
                onChange={(e) => setForm({ ...form, startTime: e.target.value })} className="input" />
            </Field>
            <Field label="End">
              <input type="time" value={form.endTime}
                onChange={(e) => setForm({ ...form, endTime: e.target.value })} className="input" />
            </Field>
          </div>
          <Field label="Location"><input value={form.location} maxLength={200}
            onChange={(e) => setForm({ ...form, location: e.target.value })} className="input" /></Field>
          <Field label="Notes"><textarea value={form.notes} maxLength={500}
            onChange={(e) => setForm({ ...form, notes: e.target.value })}
            className="input min-h-16 py-2" /></Field>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={form.isActive}
              onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
            Active (shown on the public service-times page)
          </label>

          <div className="flex gap-2">
            <button type="submit"
              className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
              {editing ? "Save changes" : "Create"}
            </button>
            {editing && (
              <button type="button" onClick={startNew}
                className="inline-flex h-10 items-center justify-center rounded-md border bg-card px-3 text-sm hover:bg-muted">
                Cancel
              </button>
            )}
          </div>

          <style>{`
            .input {
              height: 2.5rem; width: 100%; border-radius: 0.375rem;
              border: 1px solid hsl(var(--input)); background: hsl(var(--background));
              padding: 0 0.75rem; font-size: 0.875rem;
            }
            textarea.input { height: auto; }
          `}</style>
        </form>
      </div>
    </div>
  );
}

function Field({ label, required, children }: { label: string; required?: boolean; children: React.ReactNode }) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-medium">
        {label}{required && <span className="text-destructive"> *</span>}
      </span>
      {children}
    </label>
  );
}
