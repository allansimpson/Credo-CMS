import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { eventsApi, type EventDetail } from "@/lib/api/events";
import {
  eventRegistrationApi,
  type CreateRegistrationFieldRequest,
  type EventRegistration,
  type EventRegistrationField,
  type EventRegistrationFieldType,
} from "@/lib/api/eventRegistration";

const FIELD_TYPE_LABELS: Record<EventRegistrationFieldType, string> = {
  0: "Short text", 1: "Long text", 2: "Number", 3: "Date",
  4: "Single select", 5: "Multi-select", 6: "Yes/No", 7: "Email", 8: "Phone",
};

const STATUS_LABELS: Record<number, string> = {
  0: "Confirmed", 1: "Waitlisted", 2: "Canceled",
};

export function EventRegistrationsAdminPage() {
  const { id } = useParams<{ id: string }>();
  const eventId = id!;

  const [evt, setEvt] = useState<EventDetail | null>(null);
  const [fields, setFields] = useState<EventRegistrationField[]>([]);
  const [registrations, setRegistrations] = useState<EventRegistration[]>([]);
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);

  async function reload() {
    setLoading(true);
    try {
      const [d, fs, rs] = await Promise.all([
        eventsApi.get(eventId),
        eventRegistrationApi.listFields(eventId),
        eventRegistrationApi.listRegistrations(eventId,
          statusFilter === "" ? undefined : Number(statusFilter)),
      ]);
      setEvt(d);
      setFields(fs);
      setRegistrations(rs);
    } catch {
      setErr("Could not load registration data.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { reload(); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, [eventId, statusFilter]);

  if (loading && !evt) return <p className="text-muted-foreground">Loading…</p>;
  if (err) return <p className="text-destructive">{err}</p>;
  if (!evt) return null;

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <Link to={`/admin/events/${eventId}`} className="text-xs text-muted-foreground hover:underline">
            ← Back to event
          </Link>
          <h1 className="text-2xl font-bold">{evt.title}</h1>
          <p className="text-sm text-muted-foreground">Registration management</p>
        </div>
        <a
          href={eventRegistrationApi.csvExportUrl(eventId)}
          className="inline-flex h-10 items-center justify-center border bg-card px-4 text-sm hover:bg-muted"
          download
        >
          Export CSV
        </a>
      </div>

      <FieldsManager
        eventId={eventId}
        fields={fields}
        onChanged={reload}
      />

      <RegistrationsList
        eventId={eventId}
        fields={fields}
        registrations={registrations}
        statusFilter={statusFilter}
        onStatusFilter={setStatusFilter}
        onChanged={reload}
      />
    </div>
  );
}

function FieldsManager({
  eventId, fields, onChanged,
}: { eventId: string; fields: EventRegistrationField[]; onChanged: () => void }) {
  const [editingId, setEditingId] = useState<string | "new" | null>(null);

  return (
    <section className="border bg-card">
      <header className="flex items-center justify-between border-b p-3">
        <h2 className="text-sm font-semibold">Custom registration fields</h2>
        <button type="button" onClick={() => setEditingId("new")}
          className="inline-flex h-8 items-center justify-center bg-primary px-3 text-xs font-semibold text-primary-foreground hover:bg-primary/90">
          Add field
        </button>
      </header>

      {fields.length === 0 && editingId !== "new" && (
        <p className="p-3 text-sm text-muted-foreground">No custom fields yet — name, email, phone are always collected.</p>
      )}

      <ul className="divide-y">
        {fields.map((f) => (
          <li key={f.id} className="p-3">
            {editingId === f.id ? (
              <FieldForm
                eventId={eventId}
                fieldId={f.id}
                initial={f}
                onCancel={() => setEditingId(null)}
                onSaved={() => { setEditingId(null); onChanged(); }}
              />
            ) : (
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <p className="text-sm font-medium">{f.label} {f.required && <span className="text-destructive">*</span>}</p>
                  <p className="text-xs text-muted-foreground">
                    {FIELD_TYPE_LABELS[f.fieldType]}{f.options?.length ? ` · ${f.options.length} options` : ""}
                  </p>
                </div>
                <div className="flex gap-2">
                  <button type="button" onClick={() => setEditingId(f.id)}
                    className="text-xs text-primary hover:underline">Edit</button>
                  <button type="button"
                    onClick={async () => {
                      if (!window.confirm(`Remove "${f.label}"?`)) return;
                      await eventRegistrationApi.removeField(eventId, f.id);
                      onChanged();
                    }}
                    className="text-xs text-destructive hover:underline">Remove</button>
                </div>
              </div>
            )}
          </li>
        ))}
        {editingId === "new" && (
          <li className="p-3">
            <FieldForm
              eventId={eventId}
              initial={null}
              onCancel={() => setEditingId(null)}
              onSaved={() => { setEditingId(null); onChanged(); }}
              defaultDisplayOrder={(fields.at(-1)?.displayOrder ?? -1) + 1}
            />
          </li>
        )}
      </ul>
    </section>
  );
}

function FieldForm({
  eventId, fieldId, initial, onSaved, onCancel, defaultDisplayOrder = 0,
}: {
  eventId: string; fieldId?: string;
  initial: EventRegistrationField | null;
  onSaved: () => void; onCancel: () => void;
  defaultDisplayOrder?: number;
}) {
  const [label, setLabel] = useState(initial?.label ?? "");
  const [fieldType, setFieldType] = useState<EventRegistrationFieldType>(initial?.fieldType ?? 0);
  const [required, setRequired] = useState(initial?.required ?? false);
  const [helpText, setHelpText] = useState(initial?.helpText ?? "");
  const [optionsText, setOptionsText] = useState(initial?.options?.join("\n") ?? "");
  const [textMaxLength, setTextMaxLength] = useState<string>(initial?.textMaxLength?.toString() ?? "");
  const [numberMin, setNumberMin] = useState<string>(initial?.numberMin?.toString() ?? "");
  const [numberMax, setNumberMax] = useState<string>(initial?.numberMax?.toString() ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const wantsOptions = fieldType === 4 || fieldType === 5;
  const wantsTextLen = fieldType === 0 || fieldType === 1;
  const wantsNumberRange = fieldType === 2;

  async function save(e: React.FormEvent) {
    e.preventDefault();
    if (!label.trim()) { setError("Label is required."); return; }
    setSubmitting(true); setError(null);
    const body: CreateRegistrationFieldRequest = {
      label: label.trim(),
      fieldType,
      required,
      helpText: helpText.trim() || null,
      options: wantsOptions
        ? optionsText.split(/\r?\n/).map((s) => s.trim()).filter(Boolean)
        : null,
      textMaxLength: wantsTextLen && textMaxLength ? parseInt(textMaxLength, 10) : null,
      numberMin: wantsNumberRange && numberMin ? parseFloat(numberMin) : null,
      numberMax: wantsNumberRange && numberMax ? parseFloat(numberMax) : null,
      displayOrder: initial?.displayOrder ?? defaultDisplayOrder,
    };
    try {
      if (fieldId) await eventRegistrationApi.updateField(eventId, fieldId, body);
      else await eventRegistrationApi.addField(eventId, body);
      onSaved();
    } catch {
      setError("Save failed.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={save} className="space-y-2">
      {error && <p className="text-sm text-destructive">{error}</p>}
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <label className="block text-sm">
          <span className="block text-xs font-medium">Label</span>
          <input value={label} onChange={(e) => setLabel(e.target.value)}
            maxLength={200}
            className="mt-1 h-9 w-full border bg-background px-2 text-sm" />
        </label>
        <label className="block text-sm">
          <span className="block text-xs font-medium">Type</span>
          <select value={fieldType}
            onChange={(e) => setFieldType(parseInt(e.target.value, 10) as EventRegistrationFieldType)}
            className="mt-1 h-9 w-full border bg-background px-2 text-sm">
            {Object.entries(FIELD_TYPE_LABELS).map(([v, l]) => (
              <option key={v} value={v}>{l}</option>
            ))}
          </select>
        </label>
      </div>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" checked={required} onChange={(e) => setRequired(e.target.checked)} />
        Required
      </label>
      <label className="block text-sm">
        <span className="block text-xs font-medium">Help text</span>
        <input value={helpText} onChange={(e) => setHelpText(e.target.value)}
          className="mt-1 h-9 w-full border bg-background px-2 text-sm" />
      </label>
      {wantsOptions && (
        <label className="block text-sm">
          <span className="block text-xs font-medium">Options (one per line)</span>
          <textarea value={optionsText} onChange={(e) => setOptionsText(e.target.value)}
            rows={4}
            className="mt-1 w-full border bg-background px-2 py-1 text-sm" />
        </label>
      )}
      {wantsTextLen && (
        <label className="block text-sm">
          <span className="block text-xs font-medium">Max length</span>
          <input type="number" min={1} value={textMaxLength}
            onChange={(e) => setTextMaxLength(e.target.value)}
            className="mt-1 h-9 w-32 border bg-background px-2 text-sm" />
        </label>
      )}
      {wantsNumberRange && (
        <div className="grid grid-cols-2 gap-2 text-sm">
          <label>
            <span className="block text-xs font-medium">Min</span>
            <input type="number" step="any" value={numberMin}
              onChange={(e) => setNumberMin(e.target.value)}
              className="mt-1 h-9 w-full border bg-background px-2 text-sm" />
          </label>
          <label>
            <span className="block text-xs font-medium">Max</span>
            <input type="number" step="any" value={numberMax}
              onChange={(e) => setNumberMax(e.target.value)}
              className="mt-1 h-9 w-full border bg-background px-2 text-sm" />
          </label>
        </div>
      )}
      <div className="flex gap-2 pt-1">
        <button type="submit" disabled={submitting}
          className="inline-flex h-8 items-center justify-center bg-primary px-3 text-xs font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
          {submitting ? "Saving…" : "Save field"}
        </button>
        <button type="button" onClick={onCancel}
          className="inline-flex h-8 items-center justify-center border bg-card px-3 text-xs hover:bg-muted">
          Cancel
        </button>
      </div>
    </form>
  );
}

function RegistrationsList({
  eventId, fields, registrations, statusFilter, onStatusFilter, onChanged,
}: {
  eventId: string;
  fields: EventRegistrationField[];
  registrations: EventRegistration[];
  statusFilter: number | "";
  onStatusFilter: (v: number | "") => void;
  onChanged: () => void;
}) {
  const fieldMap = useMemo(() => new Map(fields.map((f) => [f.id, f])), [fields]);

  return (
    <section className="border bg-card">
      <header className="flex flex-wrap items-center justify-between gap-2 border-b p-3">
        <h2 className="text-sm font-semibold">Registrations ({registrations.length})</h2>
        <label className="text-sm">
          <span className="mr-1 text-xs text-muted-foreground">Status:</span>
          <select value={statusFilter}
            onChange={(e) => onStatusFilter(e.target.value === "" ? "" : Number(e.target.value) as 0 | 1 | 2)}
            className="h-8 border bg-background px-2 text-sm">
            <option value="">All</option>
            <option value="0">Confirmed</option>
            <option value="1">Waitlisted</option>
            <option value="2">Canceled</option>
          </select>
        </label>
      </header>

      {registrations.length === 0 ? (
        <p className="p-3 text-sm text-muted-foreground">No registrations yet.</p>
      ) : (
        <ul className="divide-y">
          {registrations.map((r) => (
            <li key={r.id} className="p-3 text-sm">
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <p className="font-medium">{r.submitterName} <span className="text-muted-foreground">· {r.submitterEmail}</span></p>
                  <p className="text-xs text-muted-foreground">
                    {STATUS_LABELS[r.status]} · {new Date(r.submittedAt).toLocaleString()}
                    {r.submitterPhone && ` · ${r.submitterPhone}`}
                  </p>
                </div>
                <div className="flex gap-3 text-xs">
                  {r.status !== 2 && (
                    <button type="button"
                      onClick={async () => {
                        const reason = window.prompt("Cancellation note (optional)?") ?? undefined;
                        if (!window.confirm("Cancel this registration?")) return;
                        await eventRegistrationApi.cancelRegistration(eventId, r.id, reason);
                        onChanged();
                      }}
                      className="text-destructive hover:underline">Cancel</button>
                  )}
                  {r.status === 0 && (
                    <button type="button"
                      onClick={async () => {
                        await eventRegistrationApi.resendConfirmation(eventId, r.id);
                        window.alert("Confirmation marked for resend.");
                      }}
                      className="text-primary hover:underline">Resend confirmation</button>
                  )}
                </div>
              </div>
              {Object.keys(r.fieldValues ?? {}).length > 0 && (
                <dl className="mt-2 grid grid-cols-1 gap-x-4 gap-y-1 text-xs sm:grid-cols-2">
                  {Object.entries(r.fieldValues).map(([fid, v]) => {
                    const f = fieldMap.get(fid);
                    if (!f) return null;
                    const display = Array.isArray(v) ? v.join(", ") : String(v);
                    return (
                      <div key={fid}>
                        <dt className="text-muted-foreground">{f.label}</dt>
                        <dd>{display}</dd>
                      </div>
                    );
                  })}
                </dl>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
