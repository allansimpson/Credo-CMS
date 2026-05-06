import { useEffect, useState } from "react";
import { announcementApi } from "@/lib/api/announcement";
import type { AnnouncementBanner, AnnouncementSeverity } from "@/types/api";

export function AnnouncementPage() {
  const [banner, setBanner] = useState<AnnouncementBanner | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    let cancelled = false;
    announcementApi.get()
      .then((b) => { if (!cancelled) { setBanner(b); setLoading(false); } })
      .catch(() => { if (!cancelled) { setErrors(["Could not load announcement banner."]); setLoading(false); } });
    return () => { cancelled = true; };
  }, []);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!banner) return;
    setSubmitting(true); setErrors([]); setSuccess(false);
    try {
      const updated = await announcementApi.update({
        isActive: banner.isActive,
        severity: banner.severity,
        message: banner.message,
        linkUrl: banner.linkUrl,
        linkLabel: banner.linkLabel,
        startsAt: banner.startsAt,
        endsAt: banner.endsAt,
      });
      setBanner(updated);
      setSuccess(true);
    } catch (err) {
      const m = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Save failed."];
      setErrors(m);
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <p className="text-muted">Loading…</p>;
  if (!banner) return <p className="text-danger">{errors.join(" ")}</p>;

  return (
    <form onSubmit={submit} className="max-w-2xl space-y-4">
      <h1 className="text-2xl font-bold">Announcement banner</h1>
      <p className="text-sm text-muted">
        A site-wide banner shown above the public nav. Dismissable per-session by visitors.
      </p>

      {errors.length > 0 && (
        <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          <ul className="list-disc pl-5">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
        </div>
      )}
      {success && (
        <div role="status" className="rounded-md border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-800">Saved.</div>
      )}

      <fieldset className="space-y-4 rounded-lg border bg-card p-4">
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={banner.isActive}
            onChange={(e) => setBanner({ ...banner, isActive: e.target.checked })} />
          Active (show the banner)
        </label>

        <Field label="Severity">
          <div className="flex flex-wrap gap-3">
            {([0, 1, 2] as AnnouncementSeverity[]).map((s) => (
              <label key={s} className="flex items-center gap-2 text-sm">
                <input type="radio" name="severity" checked={banner.severity === s}
                  onChange={() => setBanner({ ...banner, severity: s })} />
                {SEVERITY_LABELS[s]}
              </label>
            ))}
          </div>
        </Field>

        <Field label="Message" required>
          <textarea value={banner.message} required maxLength={500}
            onChange={(e) => setBanner({ ...banner, message: e.target.value })}
            className="input min-h-20 py-2" />
        </Field>
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Link URL">
            <input value={banner.linkUrl ?? ""} maxLength={2000}
              onChange={(e) => setBanner({ ...banner, linkUrl: e.target.value || null })}
              className="input" />
          </Field>
          <Field label="Link label">
            <input value={banner.linkLabel ?? ""} maxLength={100}
              onChange={(e) => setBanner({ ...banner, linkLabel: e.target.value || null })}
              className="input" />
          </Field>
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Starts at" hint="Optional. UTC.">
            <input type="datetime-local"
              value={banner.startsAt ? banner.startsAt.slice(0, 16) : ""}
              onChange={(e) => setBanner({ ...banner, startsAt: e.target.value ? new Date(e.target.value).toISOString() : null })}
              className="input" />
          </Field>
          <Field label="Ends at" hint="Optional. UTC.">
            <input type="datetime-local"
              value={banner.endsAt ? banner.endsAt.slice(0, 16) : ""}
              onChange={(e) => setBanner({ ...banner, endsAt: e.target.value ? new Date(e.target.value).toISOString() : null })}
              className="input" />
          </Field>
        </div>
      </fieldset>

      <button type="submit" disabled={submitting}
        className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
        {submitting ? "Saving…" : "Save changes"}
      </button>

      <style>{`
        .input { height: 2.5rem; width: 100%; border-radius: 0.375rem;
          border: 1px solid hsl(var(--input)); background: hsl(var(--background));
          padding: 0 0.75rem; font-size: 0.875rem; }
        textarea.input { height: auto; }
      `}</style>
    </form>
  );
}

const SEVERITY_LABELS: Record<AnnouncementSeverity, string> = {
  0: "Info", 1: "Warning", 2: "Critical",
};

function Field({ label, required, hint, children }: { label: string; required?: boolean; hint?: string; children: React.ReactNode }) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-medium">
        {label}{required && <span className="text-danger"> *</span>}
      </span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted">{hint}</span>}
    </label>
  );
}
