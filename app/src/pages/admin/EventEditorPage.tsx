import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { eventsApi, type EventDetail, type EventRequest, type EventVisibility, type EventRegistrationMode } from "@/lib/api/events";
import { slugify } from "@/lib/slug";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapFullEditor } from "@/components/shared/TipTapFullEditor";
import {
  buildRRule, parseRRule, WEEKDAYS, type RecurrenceState, type Weekday,
} from "@/lib/recurrence";

interface FormState {
  slug: string;
  title: string;
  descriptionJson: string | null;
  startsAt: string;
  endsAt: string;
  allDay: boolean;
  location: string;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  visibility: EventVisibility | null;
  recurrence: RecurrenceState;
  recurrenceEnd: "none" | "until" | "count";
  recurrenceEndDate: string;
  recurrenceCount: number;
  registrationMode: EventRegistrationMode;
  capacity: number | null;
  waitlistEnabled: boolean;
  registrationOpensAt: string;
  registrationClosesAt: string;
  registrationConfirmationMessageJson: string | null;
  externalRegistrationUrl: string;
  isPublished: boolean;
}

const empty: FormState = {
  slug: "", title: "", descriptionJson: null,
  startsAt: nowDateTimeLocal(), endsAt: "",
  allDay: false, location: "",
  heroImageUrl: null, heroImageWebpUrl: null, heroImageAlt: null,
  visibility: null,
  recurrence: { pattern: "none", weekday: null, monthDay: null },
  recurrenceEnd: "none", recurrenceEndDate: "", recurrenceCount: 10,
  registrationMode: 0, capacity: null, waitlistEnabled: false,
  registrationOpensAt: "", registrationClosesAt: "",
  registrationConfirmationMessageJson: null,
  externalRegistrationUrl: "",
  isPublished: false,
};

function fromDetail(d: EventDetail): FormState {
  const rec = parseRRule(d.recurrenceRule);
  return {
    slug: d.slug,
    title: d.title,
    descriptionJson: d.descriptionJson,
    startsAt: d.startsAt.slice(0, 16),
    endsAt: d.endsAt ? d.endsAt.slice(0, 16) : "",
    allDay: d.allDay,
    location: d.location ?? "",
    heroImageUrl: d.heroImageUrl,
    heroImageWebpUrl: d.heroImageWebpUrl,
    heroImageAlt: d.heroImageAlt,
    visibility: d.visibility,
    recurrence: rec,
    recurrenceEnd: d.recurrenceCount ? "count" : d.recurrenceEndDate ? "until" : "none",
    recurrenceEndDate: d.recurrenceEndDate ? d.recurrenceEndDate.slice(0, 10) : "",
    recurrenceCount: d.recurrenceCount ?? 10,
    registrationMode: d.registrationMode,
    capacity: d.capacity,
    waitlistEnabled: d.waitlistEnabled,
    registrationOpensAt: d.registrationOpensAt ? d.registrationOpensAt.slice(0, 16) : "",
    registrationClosesAt: d.registrationClosesAt ? d.registrationClosesAt.slice(0, 16) : "",
    registrationConfirmationMessageJson: d.registrationConfirmationMessageJson,
    externalRegistrationUrl: d.externalRegistrationUrl ?? "",
    isPublished: d.isPublished,
  };
}

function toApi(f: FormState): EventRequest {
  return {
    slug: f.slug,
    title: f.title,
    descriptionJson: f.descriptionJson,
    startsAt: new Date(f.startsAt).toISOString(),
    endsAt: f.endsAt ? new Date(f.endsAt).toISOString() : null,
    allDay: f.allDay,
    location: f.location || null,
    heroImageUrl: f.heroImageUrl,
    heroImageWebpUrl: f.heroImageWebpUrl,
    heroImageAlt: f.heroImageAlt,
    visibility: f.visibility,
    recurrenceRule: buildRRule(f.recurrence),
    recurrenceEndDate:
      f.recurrence.pattern !== "none" && f.recurrenceEnd === "until" && f.recurrenceEndDate
        ? new Date(f.recurrenceEndDate).toISOString()
        : null,
    recurrenceCount:
      f.recurrence.pattern !== "none" && f.recurrenceEnd === "count" ? f.recurrenceCount : null,
    registrationMode: f.registrationMode,
    capacity: f.capacity,
    waitlistEnabled: f.waitlistEnabled,
    registrationOpensAt: f.registrationOpensAt ? new Date(f.registrationOpensAt).toISOString() : null,
    registrationClosesAt: f.registrationClosesAt ? new Date(f.registrationClosesAt).toISOString() : null,
    registrationConfirmationMessageJson: f.registrationConfirmationMessageJson,
    externalRegistrationUrl: f.externalRegistrationUrl || null,
    isPublished: f.isPublished,
  };
}

function nowDateTimeLocal(): string {
  const now = new Date();
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
  return now.toISOString().slice(0, 16);
}

export function EventEditorPage() {
  const { id } = useParams<{ id?: string }>();
  const isNew = !id || id === "new";
  const navigate = useNavigate();

  const [form, setForm] = useState<FormState>(empty);
  const [original, setOriginal] = useState<EventDetail | null>(null);
  const [loading, setLoading] = useState(!isNew);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [slugAuto, setSlugAuto] = useState(isNew);

  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    eventsApi.get(id!).then((d) => {
      if (cancelled) return;
      setOriginal(d);
      setForm(fromDetail(d));
      setSlugAuto(false);
      setLoading(false);
    }).catch(() => { if (!cancelled) { setErrors(["Could not load event."]); setLoading(false); } });
    return () => { cancelled = true; };
  }, [id, isNew]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true); setErrors([]); setSuccess(false);
    try {
      if (isNew) {
        const created = await eventsApi.create(toApi(form));
        navigate(`/admin/events/${created.id}`);
      } else {
        const updated = await eventsApi.update(id!, toApi(form));
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
    if (!id || !window.confirm("Soft-delete this event?")) return;
    await eventsApi.softDelete(id);
    navigate("/admin/events");
  };

  const skipOccurrence = async (date: string) => {
    if (!id || !window.confirm(`Skip the occurrence on ${date}?`)) return;
    await eventsApi.skipOccurrence(id, date);
    window.alert("Occurrence skipped. Refresh to see it removed from the public calendar.");
  };

  if (loading) return <p className="text-muted">Loading…</p>;

  return (
    <form onSubmit={submit} className="space-y-6">
      <h1 className="text-2xl font-bold">{isNew ? "New event" : "Edit event"}</h1>

      {errors.length > 0 && (
        <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          <ul className="list-disc pl-5">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
        </div>
      )}
      {success && (
        <div role="status" className="border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-800">Saved.</div>
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
        <Field label="Slug" required hint={slugAuto ? "Auto-generating; edit to lock." : undefined}>
          <input value={form.slug} required
            onChange={(e) => { setSlugAuto(false); setForm((f) => ({ ...f, slug: e.target.value })); }}
            className="input" />
        </Field>
      </div>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Hero image</legend>
        <ImageUpload
          ariaLabel="Event hero image"
          value={{ url: form.heroImageUrl, webpUrl: form.heroImageWebpUrl, alt: form.heroImageAlt }}
          onChange={(next) => setForm((f) => ({
            ...f, heroImageUrl: next.url, heroImageWebpUrl: next.webpUrl, heroImageAlt: next.alt,
          }))}
        />
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Description</legend>
        <TipTapFullEditor
          ariaLabel="Event description"
          valueJson={form.descriptionJson}
          onChangeJson={(json) => setForm((f) => ({ ...f, descriptionJson: json }))}
          placeholder="Describe the event…"
        />
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">When &amp; where</legend>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Field label="Starts at" required>
            <input type="datetime-local" value={form.startsAt} required
              onChange={(e) => setForm((f) => ({ ...f, startsAt: e.target.value }))} className="input" />
          </Field>
          <Field label="Ends at" hint="Optional. Must be after start.">
            <input type="datetime-local" value={form.endsAt}
              onChange={(e) => setForm((f) => ({ ...f, endsAt: e.target.value }))} className="input" />
          </Field>
        </div>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.allDay}
            onChange={(e) => setForm((f) => ({ ...f, allDay: e.target.checked }))} />
          All-day event
        </label>
        <Field label="Location">
          <input value={form.location} maxLength={500}
            onChange={(e) => setForm((f) => ({ ...f, location: e.target.value }))}
            className="input" />
        </Field>
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Recurrence</legend>
        <Field label="Pattern">
          <select value={form.recurrence.pattern}
            onChange={(e) => setForm((f) => ({
              ...f,
              recurrence: parseRRule(buildPatternRRule(e.target.value as RecurrenceState["pattern"], f.recurrence)),
            }))}
            className="input">
            <option value="none">Does Not Repeat</option>
            <option value="daily">Daily</option>
            <option value="weekly">Weekly</option>
            <option value="monthly">Monthly</option>
          </select>
        </Field>
        {form.recurrence.pattern === "weekly" && (
          <Field label="Day of week">
            <select value={form.recurrence.weekday ?? ""}
              onChange={(e) => setForm((f) => ({
                ...f, recurrence: { ...f.recurrence, weekday: (e.target.value || null) as Weekday | null },
              }))}
              className="input">
              <option value="">— Select day —</option>
              {WEEKDAYS.map((d) => <option key={d.value} value={d.value}>{d.label}</option>)}
            </select>
          </Field>
        )}
        {form.recurrence.pattern === "monthly" && (
          <Field label="Day of month" hint="1–31. Months without that date are skipped.">
            <input type="number" min={1} max={31} value={form.recurrence.monthDay ?? ""}
              onChange={(e) => setForm((f) => ({
                ...f, recurrence: { ...f.recurrence, monthDay: e.target.value ? parseInt(e.target.value, 10) : null },
              }))}
              className="input" />
          </Field>
        )}
        {form.recurrence.pattern !== "none" && (
          <div className="space-y-2">
            <Field label="End condition">
              <select value={form.recurrenceEnd}
                onChange={(e) => setForm((f) => ({ ...f, recurrenceEnd: e.target.value as FormState["recurrenceEnd"] }))}
                className="input">
                <option value="none">No end (open-ended)</option>
                <option value="until">On a specific date</option>
                <option value="count">After N occurrences</option>
              </select>
            </Field>
            {form.recurrenceEnd === "until" && (
              <Field label="End date">
                <input type="date" value={form.recurrenceEndDate}
                  onChange={(e) => setForm((f) => ({ ...f, recurrenceEndDate: e.target.value }))}
                  className="input" />
              </Field>
            )}
            {form.recurrenceEnd === "count" && (
              <Field label="Count">
                <input type="number" min={1} value={form.recurrenceCount}
                  onChange={(e) => setForm((f) => ({ ...f, recurrenceCount: parseInt(e.target.value, 10) || 1 }))}
                  className="input" />
              </Field>
            )}
          </div>
        )}
      </fieldset>

      {!isNew && original?.recurrenceRule && (
        <fieldset className="space-y-3 border bg-card p-4">
          <legend className="px-2 text-sm font-semibold">Skip an occurrence</legend>
          <p className="text-xs text-muted">
            Use this to cancel a single date in the series. The skip is rendered as an EXDATE in iCal feeds.
          </p>
          <div className="flex items-end gap-2">
            <Field label="Date to skip">
              <input type="date" id="skip-date" className="input" />
            </Field>
            <button type="button"
              onClick={() => {
                const el = document.getElementById("skip-date") as HTMLInputElement | null;
                if (el?.value) skipOccurrence(el.value);
              }}
              className="inline-flex h-10 items-center justify-center border border-danger/30 bg-card px-4 text-sm text-danger hover:bg-danger/10">
              Skip
            </button>
          </div>
        </fieldset>
      )}

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Visibility</legend>
        <p className="text-xs text-muted">
          Required before publishing. No default — pick deliberately.
        </p>
        <div className="space-y-1">
          {[{ v: 0 as EventVisibility, label: "Public" }, { v: 1 as EventVisibility, label: "Members only" }].map((opt) => (
            <label key={opt.v} className="flex items-center gap-2 text-sm">
              <input type="radio" name="visibility" checked={form.visibility === opt.v}
                onChange={() => setForm((f) => ({ ...f, visibility: opt.v }))} />
              {opt.label}
            </label>
          ))}
        </div>
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Registration</legend>
        {!isNew && (
          <Link to={`/admin/events/${id}/registrations`}
            className="inline-flex h-8 items-center justify-center border bg-card px-3 text-xs hover:bg-panel-alt">
            Manage registrations & fields →
          </Link>
        )}
        <Field label="Mode">
          <select value={form.registrationMode}
            onChange={(e) => setForm((f) => ({ ...f, registrationMode: parseInt(e.target.value, 10) as EventRegistrationMode }))}
            className="input">
            <option value={0}>None</option>
            <option value={1}>RSVP optional</option>
            <option value={2}>Registration required</option>
          </select>
        </Field>
        {form.registrationMode > 0 && (
          <>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Field label="Capacity" hint="Leave blank for unlimited.">
                <input type="number" min={1} value={form.capacity ?? ""}
                  onChange={(e) => setForm((f) => ({ ...f, capacity: e.target.value ? parseInt(e.target.value, 10) : null }))}
                  className="input" />
              </Field>
              <label className="flex items-end gap-2 text-sm pb-2">
                <input type="checkbox" checked={form.waitlistEnabled}
                  onChange={(e) => setForm((f) => ({ ...f, waitlistEnabled: e.target.checked }))} />
                Enable waitlist when full
              </label>
            </div>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Field label="Opens at">
                <input type="datetime-local" value={form.registrationOpensAt}
                  onChange={(e) => setForm((f) => ({ ...f, registrationOpensAt: e.target.value }))}
                  className="input" />
              </Field>
              <Field label="Closes at">
                <input type="datetime-local" value={form.registrationClosesAt}
                  onChange={(e) => setForm((f) => ({ ...f, registrationClosesAt: e.target.value }))}
                  className="input" />
              </Field>
            </div>
            <Field label="External URL"
              hint="Optional. For paid events linked to Tithe.ly etc. Replaces native form.">
              <input value={form.externalRegistrationUrl} maxLength={2000}
                onChange={(e) => setForm((f) => ({ ...f, externalRegistrationUrl: e.target.value }))}
                className="input" />
            </Field>
            <Field label="Confirmation message">
              <TipTapFullEditor
                ariaLabel="Registration confirmation"
                valueJson={form.registrationConfirmationMessageJson}
                onChangeJson={(json) => setForm((f) => ({ ...f, registrationConfirmationMessageJson: json }))}
                placeholder="What to show on the confirmation page after registering…"
                minHeight={160}
              />
            </Field>
          </>
        )}
      </fieldset>

      <fieldset className="space-y-3 border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Publish</legend>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.isPublished}
            onChange={(e) => setForm((f) => ({ ...f, isPublished: e.target.checked }))} />
          Published (requires visibility above)
        </label>
      </fieldset>

      <div className="flex flex-wrap gap-2">
        <button type="submit" disabled={submitting}
          className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
          {submitting ? "Saving…" : isNew ? "Create event" : "Save changes"}
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
        textarea.input { height: auto; }
      `}</style>
    </form>
  );
}

function buildPatternRRule(pattern: RecurrenceState["pattern"], current: RecurrenceState): string | null {
  return buildRRule({
    pattern,
    weekday: pattern === "weekly" ? current.weekday : null,
    monthDay: pattern === "monthly" ? current.monthDay : null,
  });
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
