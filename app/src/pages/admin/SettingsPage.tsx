import { useEffect, useState } from "react";
import { siteSettingsApi } from "@/lib/api/siteSettings";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { SiteSettings, UpdateSiteSettingsRequest } from "@/types/api";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import { ImageUpload } from "@/components/shared/ImageUpload";

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
        {active === "content" && <ContentTab />}
        {active === "email" && <PlaceholderTab name="Email & Notifications" phase="Phase 5" />}
        {active === "integrations" && <PlaceholderTab name="Integrations" phase="Phase 3+" />}
        {active === "privacy" && <PlaceholderTab name="Privacy & Security" phase="future phase" />}
        {active === "advanced" && <AdvancedTab />}
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

/**
 * Builds an UpdateSiteSettingsRequest from a SiteSettings DTO. Used by every
 * tab so each one round-trips the full record (Site Settings is a single
 * row; we always replace all fields at once and rely on the optimistic-
 * concurrency token to detect parallel edits).
 */
function buildRequest(s: SiteSettings): UpdateSiteSettingsRequest {
  return {
    churchName: s.churchName,
    tagline: s.tagline,
    logoUrl: s.logoUrl,
    primaryColor: s.primaryColor,
    accentColor: s.accentColor,
    contactEmail: s.contactEmail,
    contactPhone: s.contactPhone,
    contactAddress: s.contactAddress,
    facebookUrl: s.facebookUrl,
    instagramUrl: s.instagramUrl,
    youTubeUrl: s.youTubeUrl,
    xUrl: s.xUrl,
    tikTokUrl: s.tikTokUrl,
    otherSocialLabel: s.otherSocialLabel,
    otherSocialUrl: s.otherSocialUrl,
    footerText: s.footerText,
    defaultVersionRetentionCount: s.defaultVersionRetentionCount,
    leadersPageLabel: s.leadersPageLabel,
    leaderCategoriesJson: s.leaderCategoriesJson,
    documentCategoriesJson: s.documentCategoriesJson,
    maxDocumentSizeBytes: s.maxDocumentSizeBytes,
    maxImageSizeBytes: s.maxImageSizeBytes,
    imageMaxWidth: s.imageMaxWidth,
    imageQuality: s.imageQuality,
    membersWelcomeText: s.membersWelcomeText,
    homepageHeroCtaLabel: s.homepageHeroCtaLabel,
    homepageHeroCtaLink: s.homepageHeroCtaLink,
    defaultMetaDescription: s.defaultMetaDescription,
    rowVersion: s.rowVersion,
  };
}

interface SettingsFormState {
  settings: SiteSettings | null;
  setSettings: (s: SiteSettings) => void;
  loading: boolean;
  errors: string[];
  success: boolean;
  submitting: boolean;
  submit: () => Promise<void>;
}

function useSettingsForm(): SettingsFormState {
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

  const submit = async () => {
    if (!settings) return;
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);
    try {
      const updated = await siteSettingsApi.update(buildRequest(settings));
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
  };

  return { settings, setSettings, loading, errors, success, submitting, submit };
}

function FormBanner({ errors, success }: { errors: string[]; success: boolean }) {
  return (
    <>
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
    </>
  );
}

function SubmitButton({ submitting }: { submitting: boolean }) {
  return (
    <button
      type="submit"
      disabled={submitting}
      className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
    >
      {submitting ? "Saving…" : "Save changes"}
    </button>
  );
}

function BrandingTab() {
  const { settings, setSettings, loading, errors, success, submitting, submit } = useSettingsForm();

  if (loading) return <p className="text-muted-foreground">Loading…</p>;
  if (!settings) return <p className="text-destructive">Could not load settings.</p>;

  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); void submit(); };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} success={success} />

      <Section title="Identity">
        <Field label="Church name" required>
          <input value={settings.churchName} required onChange={(e) => setSettings({ ...settings, churchName: e.target.value })} className="input" />
        </Field>
        <Field label="Tagline">
          <input value={settings.tagline ?? ""} onChange={(e) => setSettings({ ...settings, tagline: e.target.value })} className="input" />
        </Field>
      </Section>

      <fieldset className="space-y-4 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Logo</legend>
        <ImageUpload
          ariaLabel="Logo upload"
          hint="Stored in blob storage. Both an optimized variant and a WebP variant are generated; the public site uses the WebP variant via a <picture> element."
          value={{ url: settings.logoUrl, webpUrl: null, alt: null }}
          onChange={(next) => setSettings({ ...settings, logoUrl: next.url })}
        />
      </fieldset>

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

      <SubmitButton submitting={submitting} />
      <Styles />
    </form>
  );
}

function ContentTab() {
  const { settings, setSettings, loading, errors, success, submitting, submit } = useSettingsForm();

  if (loading) return <p className="text-muted-foreground">Loading…</p>;
  if (!settings) return <p className="text-destructive">Could not load settings.</p>;

  const leaderCats = parseCategories(settings.leaderCategoriesJson);
  const docCats = parseCategories(settings.documentCategoriesJson);

  const setLeaderCats = (next: string[]) =>
    setSettings({ ...settings, leaderCategoriesJson: JSON.stringify(next) });
  const setDocCats = (next: string[]) =>
    setSettings({ ...settings, documentCategoriesJson: JSON.stringify(next) });

  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); void submit(); };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} success={success} />

      <Section title="Homepage hero">
        <Field label="CTA button label" required>
          <input
            value={settings.homepageHeroCtaLabel}
            required
            maxLength={100}
            onChange={(e) => setSettings({ ...settings, homepageHeroCtaLabel: e.target.value })}
            className="input"
          />
        </Field>
        <Field label="CTA button link" required hint="Either an absolute URL or an in-page anchor like #service-times.">
          <input
            value={settings.homepageHeroCtaLink}
            required
            maxLength={500}
            onChange={(e) => setSettings({ ...settings, homepageHeroCtaLink: e.target.value })}
            className="input"
          />
        </Field>
      </Section>

      <Section title="Leaders page">
        <Field label="Page label" required hint='Public label, e.g. "Our Leaders" or "Elders".'>
          <input
            value={settings.leadersPageLabel}
            required
            maxLength={100}
            onChange={(e) => setSettings({ ...settings, leadersPageLabel: e.target.value })}
            className="input"
          />
        </Field>
        <Field label="Leader categories" hint="Drives the admin category dropdown and the public grouping.">
          <CategoryListEditor values={leaderCats} onChange={setLeaderCats} />
        </Field>
      </Section>

      <Section title="Documents">
        <Field label="Document categories" hint="Drives admin filtering and public grouping on /documents.">
          <CategoryListEditor values={docCats} onChange={setDocCats} />
        </Field>
      </Section>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Members welcome message</legend>
        <p className="text-xs text-muted-foreground">
          Shown on the homepage to authenticated members only. Leave blank to hide the welcome block.
        </p>
        <TipTapEditor
          valueJson={settings.membersWelcomeText}
          onChangeJson={(json) => setSettings({ ...settings, membersWelcomeText: json })}
          ariaLabel="Members welcome message"
          placeholder="Welcome our members back…"
        />
      </fieldset>

      <SubmitButton submitting={submitting} />
      <Styles />
    </form>
  );
}

function AdvancedTab() {
  const { settings, setSettings, loading, errors, success, submitting, submit } = useSettingsForm();
  const [rebuildState, setRebuildState] = useState<"idle" | "queued">("idle");

  if (loading) return <p className="text-muted-foreground">Loading…</p>;
  if (!settings) return <p className="text-destructive">Could not load settings.</p>;

  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); void submit(); };
  const handleRebuild = async () => {
    setRebuildState("queued");
    try {
      const { searchApi } = await import("@/lib/api/search");
      await searchApi.rebuild();
    } catch {
      // The rebuild API is admin-only; if it fails (e.g. permissions or
      // backend down), we surface a transient error but the SettingsPage
      // doesn't carry banner state for this nested action.
    }
    window.setTimeout(() => setRebuildState("idle"), 4000);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} success={success} />

      <Section title="Image uploads">
        <Field label="Max width (px)" hint="Wider images are resized down on upload. Range: 800–5000.">
          <input
            type="number"
            min={800}
            max={5000}
            value={settings.imageMaxWidth}
            onChange={(e) => setSettings({ ...settings, imageMaxWidth: Number(e.target.value) })}
            className="input"
          />
        </Field>
        <Field label="JPEG / WebP quality" hint="60–95. Lower = smaller files but more visible compression.">
          <input
            type="number"
            min={60}
            max={95}
            value={settings.imageQuality}
            onChange={(e) => setSettings({ ...settings, imageQuality: Number(e.target.value) })}
            className="input"
          />
        </Field>
        <Field label="Max image size (MB)" hint="1–50 MB. Uploads larger than this are rejected.">
          <input
            type="number"
            min={1}
            max={50}
            value={Math.round(settings.maxImageSizeBytes / (1024 * 1024))}
            onChange={(e) =>
              setSettings({ ...settings, maxImageSizeBytes: Number(e.target.value) * 1024 * 1024 })
            }
            className="input"
          />
        </Field>
      </Section>

      <Section title="Document uploads">
        <Field label="Max document size (MB)" hint="1–200 MB. Limits PDF uploads under /admin/documents.">
          <input
            type="number"
            min={1}
            max={200}
            value={Math.round(settings.maxDocumentSizeBytes / (1024 * 1024))}
            onChange={(e) =>
              setSettings({ ...settings, maxDocumentSizeBytes: Number(e.target.value) * 1024 * 1024 })
            }
            className="input"
          />
        </Field>
      </Section>

      <Section title="SEO">
        <Field label="Default meta description" hint="Used when an entity has no description and no excerpt to fall back on. Up to 300 chars.">
          <textarea
            value={settings.defaultMetaDescription ?? ""}
            maxLength={300}
            onChange={(e) => setSettings({ ...settings, defaultMetaDescription: e.target.value })}
            className="input min-h-20 py-2"
          />
        </Field>
      </Section>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Search</legend>
        <p className="text-xs text-muted-foreground">
          Re-indexes all published Pages, News items, Leaders, and Documents. Safe to run; triggers a
          background job.
        </p>
        <button
          type="button"
          onClick={handleRebuild}
          disabled={rebuildState === "queued"}
          className="inline-flex h-9 items-center justify-center rounded-md border bg-card px-3 text-sm hover:bg-muted disabled:opacity-50"
        >
          {rebuildState === "queued" ? "Rebuilding…" : "Rebuild search index"}
        </button>
      </fieldset>

      <SubmitButton submitting={submitting} />
      <Styles />
    </form>
  );
}

interface CategoryListEditorProps {
  values: string[];
  onChange: (next: string[]) => void;
}

function CategoryListEditor({ values, onChange }: CategoryListEditorProps) {
  const update = (i: number, v: string) => {
    const next = values.slice();
    next[i] = v;
    onChange(next);
  };
  const remove = (i: number) => onChange(values.filter((_, idx) => idx !== i));
  const add = () => onChange([...values, ""]);
  const moveUp = (i: number) => {
    if (i === 0) return;
    const next = values.slice();
    [next[i - 1], next[i]] = [next[i], next[i - 1]];
    onChange(next);
  };
  const moveDown = (i: number) => {
    if (i === values.length - 1) return;
    const next = values.slice();
    [next[i + 1], next[i]] = [next[i], next[i + 1]];
    onChange(next);
  };

  return (
    <div className="space-y-2">
      {values.map((v, i) => (
        <div key={i} className="flex gap-2">
          <input
            value={v}
            onChange={(e) => update(i, e.target.value)}
            className="input flex-1"
            aria-label={`Category ${i + 1}`}
          />
          <button type="button" onClick={() => moveUp(i)} aria-label="Move up" className="iconbtn">↑</button>
          <button type="button" onClick={() => moveDown(i)} aria-label="Move down" className="iconbtn">↓</button>
          <button type="button" onClick={() => remove(i)} aria-label="Remove" className="iconbtn text-destructive">✕</button>
        </div>
      ))}
      <button
        type="button"
        onClick={add}
        className="inline-flex h-8 items-center justify-center rounded-md border px-3 text-xs hover:bg-muted"
      >
        + Add category
      </button>
    </div>
  );
}

function parseCategories(json: string): string[] {
  try {
    const parsed = JSON.parse(json);
    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === "string") : [];
  } catch {
    return [];
  }
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

function Styles() {
  return (
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
      .iconbtn {
        height: 2.5rem;
        width: 2.5rem;
        border-radius: 0.375rem;
        border: 1px solid hsl(var(--input));
        background: hsl(var(--background));
        font-size: 0.875rem;
      }
      .iconbtn:hover {
        background: hsl(var(--muted));
      }
    `}</style>
  );
}
