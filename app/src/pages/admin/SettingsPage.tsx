import { useEffect, useState } from "react";
import { siteSettingsApi } from "@/lib/api/siteSettings";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { SiteSettings, UpdateSiteSettingsRequest } from "@/types/api";

const TABS = [
  { id: "branding", label: "Branding" },
  { id: "content", label: "Content" },
  { id: "email", label: "Email & Notifications" },
  { id: "integrations", label: "Integrations" },
  { id: "privacy", label: "Privacy & Security" },
  { id: "advanced", label: "Advanced" },
] as const;

type TabId = typeof TABS[number]["id"];

export function SettingsPage() {
  const [active, setActive] = useState<TabId>("branding");

  return (
    <div>
      <h1 className="text-2xl font-bold">Site Settings</h1>

      <div className="mt-4 border-b">
        <div className="flex flex-wrap gap-1">
          {TABS.map((t) => (
            <button
              key={t.id}
              type="button"
              onClick={() => setActive(t.id)}
              className={
                "h-10 px-4 text-sm transition-colors " +
                (active === t.id
                  ? "border-b-2 border-accent text-foreground font-semibold"
                  : "text-muted-foreground hover:text-foreground")
              }
            >
              {t.label}
            </button>
          ))}
        </div>
      </div>

      <div className="mt-6">
        {active === "branding" && <BrandingTab />}
        {active === "content" && <PlaceholderTab name="Content" phase="Phase 2" />}
        {active === "email" && <PlaceholderTab name="Email & Notifications" phase="Phase 5" />}
        {active === "integrations" && <PlaceholderTab name="Integrations" phase="Phase 3+" />}
        {active === "privacy" && <PlaceholderTab name="Privacy & Security" phase="future phase" />}
        {active === "advanced" && <PlaceholderTab name="Advanced" phase="future phase" />}
      </div>
    </div>
  );
}

function PlaceholderTab({ name, phase }: { name: string; phase: string }) {
  return (
    <div className="rounded-lg border bg-card p-6">
      <h2 className="text-lg font-semibold">{name}</h2>
      <p className="mt-2 text-sm text-muted-foreground">
        Settings for {name.toLowerCase()} arrive in {phase}. The tab is shown here so the
        layout is consistent from day one.
      </p>
    </div>
  );
}

function BrandingTab() {
  const { reload } = useSiteSettings();
  const [settings, setSettings] = useState<SiteSettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    siteSettingsApi.getAdmin()
      .then((s) => { setSettings(s); setLoading(false); })
      .catch(() => setLoading(false));
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!settings) return;
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);
    try {
      const req: UpdateSiteSettingsRequest = {
        churchName: settings.churchName,
        tagline: settings.tagline,
        logoUrl: settings.logoUrl,
        primaryColor: settings.primaryColor,
        accentColor: settings.accentColor,
        contactEmail: settings.contactEmail,
        contactPhone: settings.contactPhone,
        contactAddress: settings.contactAddress,
        facebookUrl: settings.facebookUrl,
        instagramUrl: settings.instagramUrl,
        youTubeUrl: settings.youTubeUrl,
        xUrl: settings.xUrl,
        tikTokUrl: settings.tikTokUrl,
        otherSocialLabel: settings.otherSocialLabel,
        otherSocialUrl: settings.otherSocialUrl,
        footerText: settings.footerText,
        defaultVersionRetentionCount: settings.defaultVersionRetentionCount,
        rowVersion: settings.rowVersion,
      };
      const updated = await siteSettingsApi.update(req);
      setSettings(updated);
      setSuccess(true);
      await reload();
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Failed to save."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) return <p className="text-muted-foreground">Loading…</p>;
  if (!settings) return <p className="text-destructive">Could not load settings.</p>;

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {errors.length > 0 && (
        <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          <ul className="list-disc pl-5">
            {errors.map((err) => <li key={err}>{err}</li>)}
          </ul>
        </div>
      )}
      {success && (
        <div role="status" className="rounded-md border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-800">
          Settings saved.
        </div>
      )}

      <Section title="Identity">
        <Field label="Church name" required>
          <input value={settings.churchName} required onChange={(e) => setSettings({ ...settings, churchName: e.target.value })} className="input" />
        </Field>
        <Field label="Tagline">
          <input value={settings.tagline ?? ""} onChange={(e) => setSettings({ ...settings, tagline: e.target.value })} className="input" />
        </Field>
        <Field label="Logo URL" hint="Phase 1: paste a URL. Real upload arrives in Phase 2.">
          <input value={settings.logoUrl ?? ""} onChange={(e) => setSettings({ ...settings, logoUrl: e.target.value })} className="input" />
        </Field>
      </Section>

      <Section title="Colors">
        <Field label="Primary color">
          <div className="flex items-center gap-2">
            <input type="color" value={settings.primaryColor} onChange={(e) => setSettings({ ...settings, primaryColor: e.target.value })} className="h-10 w-12 rounded border" />
            <input value={settings.primaryColor} onChange={(e) => setSettings({ ...settings, primaryColor: e.target.value })} className="input" />
          </div>
        </Field>
        <Field label="Accent color">
          <div className="flex items-center gap-2">
            <input type="color" value={settings.accentColor} onChange={(e) => setSettings({ ...settings, accentColor: e.target.value })} className="h-10 w-12 rounded border" />
            <input value={settings.accentColor} onChange={(e) => setSettings({ ...settings, accentColor: e.target.value })} className="input" />
          </div>
        </Field>
      </Section>

      <Section title="Contact">
        <Field label="Email"><input type="email" value={settings.contactEmail ?? ""} onChange={(e) => setSettings({ ...settings, contactEmail: e.target.value })} className="input" /></Field>
        <Field label="Phone"><input value={settings.contactPhone ?? ""} onChange={(e) => setSettings({ ...settings, contactPhone: e.target.value })} className="input" /></Field>
        <Field label="Address"><input value={settings.contactAddress ?? ""} onChange={(e) => setSettings({ ...settings, contactAddress: e.target.value })} className="input" /></Field>
      </Section>

      <Section title="Social links">
        <Field label="Facebook"><input value={settings.facebookUrl ?? ""} onChange={(e) => setSettings({ ...settings, facebookUrl: e.target.value })} className="input" /></Field>
        <Field label="Instagram"><input value={settings.instagramUrl ?? ""} onChange={(e) => setSettings({ ...settings, instagramUrl: e.target.value })} className="input" /></Field>
        <Field label="YouTube"><input value={settings.youTubeUrl ?? ""} onChange={(e) => setSettings({ ...settings, youTubeUrl: e.target.value })} className="input" /></Field>
        <Field label="X (Twitter)"><input value={settings.xUrl ?? ""} onChange={(e) => setSettings({ ...settings, xUrl: e.target.value })} className="input" /></Field>
        <Field label="TikTok"><input value={settings.tikTokUrl ?? ""} onChange={(e) => setSettings({ ...settings, tikTokUrl: e.target.value })} className="input" /></Field>
        <Field label="Other (label)"><input value={settings.otherSocialLabel ?? ""} onChange={(e) => setSettings({ ...settings, otherSocialLabel: e.target.value })} className="input" /></Field>
        <Field label="Other (URL)"><input value={settings.otherSocialUrl ?? ""} onChange={(e) => setSettings({ ...settings, otherSocialUrl: e.target.value })} className="input" /></Field>
      </Section>

      <Section title="Footer">
        <Field label="Footer text"><input value={settings.footerText ?? ""} onChange={(e) => setSettings({ ...settings, footerText: e.target.value })} className="input" /></Field>
      </Section>

      <Section title="Versioning">
        <Field label="Default retention (5–50)">
          <input
            type="number"
            min={5}
            max={50}
            value={settings.defaultVersionRetentionCount}
            onChange={(e) => setSettings({ ...settings, defaultVersionRetentionCount: Number(e.target.value) })}
            className="input"
          />
        </Field>
      </Section>

      <button
        type="submit"
        disabled={submitting}
        className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
      >
        {submitting ? "Saving…" : "Save changes"}
      </button>

      <style>{`
        .input {
          height: 2.5rem;
          width: 100%;
          border-radius: 0.375rem;
          border: 1px solid hsl(var(--input));
          background: hsl(var(--background));
          padding: 0 0.75rem;
          font-size: 0.875rem;
        }
      `}</style>
    </form>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <fieldset className="space-y-4 rounded-lg border bg-card p-4">
      <legend className="px-2 text-sm font-semibold">{title}</legend>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">{children}</div>
    </fieldset>
  );
}

function Field({ label, hint, required, children }: { label: string; hint?: string; required?: boolean; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium">
        {label}{required && <span className="text-destructive"> *</span>}
      </span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted-foreground">{hint}</span>}
    </label>
  );
}
