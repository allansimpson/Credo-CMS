import { lazy, Suspense, useEffect, useMemo, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { eventsApi, type PublicEvent } from "@/lib/api/events";
import {
  eventRegistrationApi,
  type EventRegistrationField,
  type SubmitRegistrationResponse,
} from "@/lib/api/eventRegistration";
import { ApiError } from "@/lib/apiClient";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { NotFoundPage } from "@/pages/NotFoundPage";

const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly }))
);

type FormValues = Record<string, string | string[] | boolean>;

export function EventRegisterPage() {
  const { slug } = useParams<{ slug: string }>();
  const { settings } = useSiteSettings();
  const [event, setEvent] = useState<PublicEvent | null>(null);
  const [fields, setFields] = useState<EventRegistrationField[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [values, setValues] = useState<FormValues>({});
  const [hp, setHp] = useState("");
  const openedAtRef = useRef<number>(Date.now());

  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [result, setResult] = useState<SubmitRegistrationResponse | null>(null);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    Promise.all([
      eventsApi.getPublic(slug),
      eventRegistrationApi.listPublicFields(slug),
    ])
      .then(([e, fs]) => {
        if (cancelled) return;
        setEvent(e);
        setFields(fs);
        setLoading(false);
        openedAtRef.current = Date.now();
      })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404) setNotFound(true);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [slug]);

  const orderedFields = useMemo(
    () => (fields ?? []).slice().sort((a, b) => a.displayOrder - b.displayOrder),
    [fields]
  );

  if (loading) return <p className="mx-auto max-w-2xl p-8 text-muted-foreground">Loading…</p>;
  if (notFound || !event) return <NotFoundPage />;

  const canRegister = event.registrationMode > 0
    && (!event.registrationOpensAt || new Date(event.registrationOpensAt) <= new Date())
    && (!event.registrationClosesAt || new Date(event.registrationClosesAt) > new Date());

  if (!canRegister && !result) {
    return (
      <article className="mx-auto max-w-2xl px-4 py-8">
        <SeoTags title={`Register · ${event.title}`} description={event.title} />
        <h1 className="text-2xl font-bold">{event.title}</h1>
        <p className="mt-4 text-muted-foreground">Registration is not currently open for this event.</p>
        <Link to={`/events/${event.slug}`} className="mt-4 inline-block text-sm text-primary hover:underline">
          ← Back to event
        </Link>
      </article>
    );
  }

  if (result) {
    return (
      <article className="mx-auto max-w-2xl px-4 py-8">
        <SeoTags title={`Registration confirmed · ${event.title}`} description={event.title} />
        <h1 className="text-2xl font-bold sm:text-3xl">
          {result.registration.status === 1 ? "You're on the waitlist" : "You're registered!"}
        </h1>
        <p className="mt-2 text-sm text-muted-foreground">
          A confirmation has been sent to <strong>{result.registration.submitterEmail}</strong>.
        </p>

        {event.registrationConfirmationMessageJson && (
          <div className="mt-6 border bg-card p-4">
            <Suspense fallback={null}>
              <TipTapReadOnly json={event.registrationConfirmationMessageJson} />
            </Suspense>
          </div>
        )}

        {result.cancelToken && (
          <div className="mt-6 border bg-muted/40 p-4 text-sm">
            <p className="font-semibold">Need to cancel?</p>
            <p className="mt-1 text-muted-foreground">
              Use this link any time before the event:
            </p>
            <p className="mt-2 break-all">
              <Link
                to={`/events/${event.slug}/register/cancel?token=${encodeURIComponent(result.cancelToken)}`}
                className="text-primary hover:underline"
              >
                {`${window.location.origin}/events/${event.slug}/register/cancel?token=${result.cancelToken}`}
              </Link>
            </p>
          </div>
        )}

        <div className="mt-6 flex flex-wrap gap-3">
          <Link
            to={`/events/${event.slug}`}
            className="inline-flex h-10 items-center justify-center border bg-card px-4 text-sm hover:bg-muted"
          >
            Back to event
          </Link>
          <Link
            to="/events"
            className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
          >
            All events
          </Link>
        </div>
      </article>
    );
  }

  function setFieldValue(id: string, value: string | string[] | boolean) {
    setValues((prev) => ({ ...prev, [id]: value }));
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (submitting) return;
    setErrors([]);
    setSubmitting(true);
    try {
      const res = await eventRegistrationApi.submit(slug!, {
        occurrenceDate: null,
        submitterName: name.trim(),
        submitterEmail: email.trim(),
        submitterPhone: phone.trim() || null,
        fieldValues: values as Record<string, unknown>,
        hp: hp || null,
        formOpenedElapsedMs: Date.now() - openedAtRef.current,
      });
      setResult(res);
    } catch (err) {
      if (err instanceof ApiError) setErrors(err.getMessages());
      else setErrors(["Something went wrong. Please try again."]);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <article className="mx-auto max-w-2xl px-4 py-8">
      <SeoTags
        title={`Register · ${event.title}`}
        description={`Register for ${event.title}${settings?.churchName ? ` at ${settings.churchName}` : ""}`}
      />
      <Link to={`/events/${event.slug}`} className="text-sm text-primary hover:underline">
        ← Back to event
      </Link>
      <h1 className="mt-2 text-2xl font-bold sm:text-3xl">{event.title}</h1>
      <p className="mt-1 text-sm text-muted-foreground">
        {new Date(event.startsAt).toLocaleString()}
        {event.location && ` · ${event.location}`}
      </p>

      {errors.length > 0 && (
        <div className="mt-4 border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
          <ul className="list-inside list-disc">
            {errors.map((er, i) => <li key={i}>{er}</li>)}
          </ul>
        </div>
      )}

      <form onSubmit={onSubmit} className="mt-6 space-y-4" noValidate>
        {/* Honeypot — visually hidden, must stay empty */}
        <div aria-hidden="true" className="absolute left-[-9999px] h-0 w-0 overflow-hidden">
          <label>
            Leave this field empty
            <input
              type="text"
              tabIndex={-1}
              autoComplete="off"
              value={hp}
              onChange={(e) => setHp(e.target.value)}
            />
          </label>
        </div>

        <FieldRow label="Full name" required>
          <input
            type="text" required
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="h-10 w-full border bg-background px-3 text-sm"
            autoComplete="name"
          />
        </FieldRow>

        <FieldRow label="Email" required>
          <input
            type="email" required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="h-10 w-full border bg-background px-3 text-sm"
            autoComplete="email"
          />
        </FieldRow>

        <FieldRow label="Phone (optional)">
          <input
            type="tel"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            className="h-10 w-full border bg-background px-3 text-sm"
            autoComplete="tel"
          />
        </FieldRow>

        {orderedFields.map((f) => (
          <DynamicField
            key={f.id}
            field={f}
            value={values[f.id]}
            onChange={(v) => setFieldValue(f.id, v)}
          />
        ))}

        <div className="pt-2">
          <button
            type="submit" disabled={submitting}
            className="inline-flex h-11 items-center justify-center bg-primary px-6 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {submitting ? "Submitting…" : "Submit registration"}
          </button>
        </div>
      </form>
    </article>
  );
}

function FieldRow({
  label, required, helpText, children,
}: { label: string; required?: boolean; helpText?: string | null; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="text-sm font-medium">
        {label}{required && <span className="text-destructive"> *</span>}
      </span>
      <div className="mt-1">{children}</div>
      {helpText && <p className="mt-1 text-xs text-muted-foreground">{helpText}</p>}
    </label>
  );
}

function DynamicField({
  field, value, onChange,
}: {
  field: EventRegistrationField;
  value: string | string[] | boolean | undefined;
  onChange: (v: string | string[] | boolean) => void;
}) {
  const common = "w-full border bg-background px-3 text-sm";
  switch (field.fieldType) {
    case 0: // ShortText
    case 7: // Email
    case 8: // Phone
      return (
        <FieldRow label={field.label} required={field.required} helpText={field.helpText}>
          <input
            type={field.fieldType === 7 ? "email" : field.fieldType === 8 ? "tel" : "text"}
            required={field.required}
            maxLength={field.textMaxLength ?? undefined}
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            className={`${common} h-10`}
          />
        </FieldRow>
      );
    case 1: // LongText
      return (
        <FieldRow label={field.label} required={field.required} helpText={field.helpText}>
          <textarea
            required={field.required}
            maxLength={field.textMaxLength ?? undefined}
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            className={`${common} min-h-24 py-2`}
          />
        </FieldRow>
      );
    case 2: // Number
      return (
        <FieldRow label={field.label} required={field.required} helpText={field.helpText}>
          <input
            type="number" required={field.required}
            min={field.numberMin ?? undefined} max={field.numberMax ?? undefined}
            step="any"
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            className={`${common} h-10`}
          />
        </FieldRow>
      );
    case 3: // Date
      return (
        <FieldRow label={field.label} required={field.required} helpText={field.helpText}>
          <input
            type="date" required={field.required}
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            className={`${common} h-10`}
          />
        </FieldRow>
      );
    case 4: // SingleSelect
      return (
        <FieldRow label={field.label} required={field.required} helpText={field.helpText}>
          <select
            required={field.required}
            value={(value as string) ?? ""}
            onChange={(e) => onChange(e.target.value)}
            className={`${common} h-10`}
          >
            <option value="">— Select —</option>
            {(field.options ?? []).map((opt) => (
              <option key={opt} value={opt}>{opt}</option>
            ))}
          </select>
        </FieldRow>
      );
    case 5: { // MultiSelect
      const arr = Array.isArray(value) ? value : [];
      return (
        <fieldset className="block">
          <legend className="text-sm font-medium">
            {field.label}{field.required && <span className="text-destructive"> *</span>}
          </legend>
          <div className="mt-2 space-y-1">
            {(field.options ?? []).map((opt) => (
              <label key={opt} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox" value={opt}
                  checked={arr.includes(opt)}
                  onChange={(e) => {
                    const next = e.target.checked ? [...arr, opt] : arr.filter((x) => x !== opt);
                    onChange(next);
                  }}
                />
                {opt}
              </label>
            ))}
          </div>
          {field.helpText && <p className="mt-1 text-xs text-muted-foreground">{field.helpText}</p>}
        </fieldset>
      );
    }
    case 6: // YesNo
      return (
        <fieldset className="block">
          <legend className="text-sm font-medium">
            {field.label}{field.required && <span className="text-destructive"> *</span>}
          </legend>
          <div className="mt-2 flex gap-4 text-sm">
            <label className="flex items-center gap-2">
              <input type="radio" name={field.id} value="yes"
                checked={value === true}
                onChange={() => onChange(true)} />
              Yes
            </label>
            <label className="flex items-center gap-2">
              <input type="radio" name={field.id} value="no"
                checked={value === false}
                onChange={() => onChange(false)} />
              No
            </label>
          </div>
          {field.helpText && <p className="mt-1 text-xs text-muted-foreground">{field.helpText}</p>}
        </fieldset>
      );
    default:
      return null;
  }
}
