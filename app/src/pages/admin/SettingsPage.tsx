import { useEffect, useState } from "react";
import { ChevronUp, ChevronDown, X } from "lucide-react";
import { siteSettingsApi } from "@/lib/api/siteSettings";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type {
  AdminNotificationFrequency,
  EmailProvider,
  SiteSettings,
  UpdateSiteSettingsRequest,
} from "@/types/api";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { useToast } from "@/components/shared/admin/Toast";
import { ApiError } from "@/lib/apiClient";
import {
  MetaLabel,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";
import { cn } from "@/lib/utils";

const TABS = [
  { id: "branding", label: "Branding" },
  { id: "content", label: "Content" },
  { id: "members", label: "Members & Community" },
  { id: "email", label: "Email & Notifications" },
  { id: "integrations", label: "Integrations" },
  { id: "privacy", label: "Privacy & Security" },
  { id: "advanced", label: "Advanced" },
] as const;

type TabId = typeof TABS[number]["id"];

export function SettingsPage() {
  const [active, setActive] = useState<TabId>("branding");
  const [errorTabs, setErrorTabs] = useState<Set<TabId>>(new Set());

  return (
    <div className="space-y-8">
      <PageHeader title="Site Settings" subtitle="Configure branding, content, and integrations." />

      <div className="grid gap-8 lg:grid-cols-[200px_minmax(0,720px)]">
        <nav aria-label="Settings sections" className="space-y-2">
          <MetaLabel>Sections</MetaLabel>
          <ul className="mt-3 space-y-px">
            {TABS.map((t) => (
              <li key={t.id}>
                <button
                  type="button"
                  onClick={() => setActive(t.id)}
                  className={cn(
                    "flex w-full items-center justify-between border-l-2 px-3 py-2 text-left text-sm transition-colors",
                    active === t.id
                      ? "border-accent bg-panel-alt font-semibold text-foreground"
                      : "border-transparent text-fg-soft hover:bg-panel-alt hover:text-foreground",
                    errorTabs.has(t.id) && active !== t.id && "border-danger text-danger",
                  )}
                >
                  {t.label}
                  {errorTabs.has(t.id) && (
                    <span className="h-2 w-2 shrink-0 rounded-full bg-danger" />
                  )}
                </button>
              </li>
            ))}
          </ul>
        </nav>

        <div className="min-w-0">
          {active === "branding" && <BrandingTab onErrorTabs={setErrorTabs} />}
          {active === "content" && <ContentTab onErrorTabs={setErrorTabs} />}
          {active === "members" && <MembersTab onErrorTabs={setErrorTabs} />}
          {active === "email" && <EmailTab onErrorTabs={setErrorTabs} />}
          {active === "integrations" && <IntegrationsTab onErrorTabs={setErrorTabs} />}
          {active === "privacy" && <PlaceholderTab name="Privacy & Security" />}
          {active === "advanced" && <AdvancedTab onErrorTabs={setErrorTabs} />}
        </div>
      </div>
    </div>
  );
}

function PlaceholderTab({ name }: { name: string }) {
  return (
    <div className="rounded-lg border bg-card p-6">
      <h2 className="text-lg font-semibold">{name}</h2>
      <p className="mt-2 text-sm text-muted">
        Settings for {name.toLowerCase()} are not yet implemented. The tab is shown here so the
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
    eventCategoriesJson: s.eventCategoriesJson,
    newsCategoriesJson: s.newsCategoriesJson,
    documentCategoriesJson: s.documentCategoriesJson,
    sermonContextsJson: s.sermonContextsJson,
    maxDocumentSizeBytes: s.maxDocumentSizeBytes,
    maxImageSizeBytes: s.maxImageSizeBytes,
    imageMaxWidth: s.imageMaxWidth,
    imageQuality: s.imageQuality,
    membersWelcomeText: s.membersWelcomeText,
    homepageHeroCtaLabel: s.homepageHeroCtaLabel,
    homepageHeroCtaLink: s.homepageHeroCtaLink,
    defaultMetaDescription: s.defaultMetaDescription,
    // Round-tripped on every save so partial-update flows from
    // individual tabs don't accidentally drop values.
    getInvolvedPageLabel: s.getInvolvedPageLabel,
    classesPageLabel: s.classesPageLabel,
    classAudienceAgeGroupsJson: s.classAudienceAgeGroupsJson,
    showRecentPastOnPublicClasses: s.showRecentPastOnPublicClasses,
    recentPastClassesLookbackDays: s.recentPastClassesLookbackDays,
    blogCategoriesJson: s.blogCategoriesJson,
    blogPageLabel: s.blogPageLabel,
    profanityWordlist: s.profanityWordlist,
    profanityAllowlist: s.profanityAllowlist,
    prayerRequestArchiveDays: s.prayerRequestArchiveDays,
    prayerRequestRequireApproval: s.prayerRequestRequireApproval,
    connectCardInterestsJson: s.connectCardInterestsJson,
    connectCardAcknowledgmentMessageJson: s.connectCardAcknowledgmentMessageJson,
    connectCardPageLabel: s.connectCardPageLabel,
    cloudflareTurnstileSiteKey: s.cloudflareTurnstileSiteKey,
    cloudflareTurnstileSecretKey: s.cloudflareTurnstileSecretKey,
    facebookOAuthAppId: s.facebookOAuthAppId,
    facebookOAuthAppSecret: s.facebookOAuthAppSecret,
    facebookLoginEnabled: s.facebookLoginEnabled,
    analyticsProvider: s.analyticsProvider,
    ga4MeasurementId: s.ga4MeasurementId,
    ga4ConsentBannerEnabled: s.ga4ConsentBannerEnabled,
    ga4ConsentBannerPosition: s.ga4ConsentBannerPosition,
    cookiePolicyPageId: s.cookiePolicyPageId,
    // Email & notifications — round-tripped even though the UI tab is placeholder.
    emailProvider: s.emailProvider,
    emailFromAddress: s.emailFromAddress,
    emailFromName: s.emailFromName,
    emailReplyToAddress: s.emailReplyToAddress,
    sendGridApiKey: s.sendGridApiKey,
    sendGridWebhookSecret: s.sendGridWebhookSecret,
    smtpHost: s.smtpHost,
    smtpPort: s.smtpPort,
    smtpUsername: s.smtpUsername,
    smtpPassword: s.smtpPassword,
    smtpUseSsl: s.smtpUseSsl,
    emailEnabled: s.emailEnabled,
    testEmailRecipient: s.testEmailRecipient,
    newsEmailTargetMode: s.newsEmailTargetMode,
    newsEmailTargetGroupIdsJson: s.newsEmailTargetGroupIdsJson,
    blogEmailTargetMode: s.blogEmailTargetMode,
    blogEmailTargetGroupIdsJson: s.blogEmailTargetGroupIdsJson,
    emailSubjectPrefixNews: s.emailSubjectPrefixNews,
    emailSubjectPrefixBlog: s.emailSubjectPrefixBlog,
    adminNotificationFrequency: s.adminNotificationFrequency,
    smsProvider: s.smsProvider,
    twilioAccountSid: s.twilioAccountSid,
    twilioAuthToken: s.twilioAuthToken,
    twilioFromNumber: s.twilioFromNumber,
    unsubscribeSigningKey: s.unsubscribeSigningKey,
    // Public Site design handoff
    template: s.template,
    rowVersion: s.rowVersion,
  };
}

type FieldErrors = Record<string, string[]>;

const FIELD_TAB_MAP: Record<string, TabId> = {
  ChurchName: "branding", Tagline: "branding", LogoUrl: "branding",
  PrimaryColor: "branding", AccentColor: "branding",
  ContactEmail: "branding", ContactPhone: "branding", ContactAddress: "branding",
  FacebookUrl: "branding", InstagramUrl: "branding", YouTubeUrl: "branding",
  XUrl: "branding", TikTokUrl: "branding", OtherSocialLabel: "branding", OtherSocialUrl: "branding",
  FooterText: "branding", DefaultVersionRetentionCount: "branding",
  HomepageHeroCtaLabel: "content", HomepageHeroCtaLink: "content",
  LeadersPageLabel: "content", LeaderCategoriesJson: "content",
  DocumentCategoriesJson: "content", MembersWelcomeText: "content",
  EventCategoriesJson: "content", NewsCategoriesJson: "content", SermonContextsJson: "content",
  GetInvolvedPageLabel: "members", ClassesPageLabel: "members",
  ClassAudienceAgeGroupsJson: "members", BlogCategoriesJson: "members", BlogPageLabel: "members",
  ProfanityWordlist: "members", ProfanityAllowlist: "members",
  PrayerRequestArchiveDays: "members", PrayerRequestRequireApproval: "members",
  ConnectCardInterestsJson: "members", ConnectCardPageLabel: "members",
  CloudflareTurnstileSiteKey: "integrations", CloudflareTurnstileSecretKey: "integrations",
  FacebookOAuthAppId: "integrations", FacebookOAuthAppSecret: "integrations",
  EmailProvider: "email", EmailFromAddress: "email", EmailFromName: "email",
  EmailReplyToAddress: "email", SendGridApiKey: "email", SendGridWebhookSecret: "email",
  SmtpHost: "email", SmtpPort: "email", SmtpUsername: "email", SmtpPassword: "email",
  SmtpUseSsl: "email", EmailEnabled: "email", TestEmailRecipient: "email",
  EmailSubjectPrefixNews: "email", EmailSubjectPrefixBlog: "email",
  AdminNotificationFrequency: "email",
  MaxDocumentSizeBytes: "advanced", MaxImageSizeBytes: "advanced",
  ImageMaxWidth: "advanced", ImageQuality: "advanced", DefaultMetaDescription: "advanced",
};

function tabsWithErrors(fieldErrors: FieldErrors): Set<TabId> {
  const tabs = new Set<TabId>();
  for (const field of Object.keys(fieldErrors)) {
    const tab = FIELD_TAB_MAP[field];
    if (tab) tabs.add(tab);
  }
  return tabs;
}

function fieldError(fieldErrors: FieldErrors, ...fields: string[]): string | undefined {
  for (const f of fields) {
    const errs = fieldErrors[f];
    if (errs?.length) return errs[0];
  }
  return undefined;
}

interface SettingsFormState {
  settings: SiteSettings | null;
  setSettings: (s: SiteSettings) => void;
  loading: boolean;
  errors: string[];
  fieldErrors: FieldErrors;
  success: boolean;
  submitting: boolean;
  submit: () => Promise<void>;
}

function useSettingsForm(): SettingsFormState {
  const { reload } = useSiteSettings();
  const [settings, setSettings] = useState<SiteSettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const { toast } = useToast();

  useEffect(() => {
    siteSettingsApi.getAdmin()
      .then((s) => { setSettings(s); setLoading(false); })
      .catch(() => setLoading(false));
  }, []);

  const submit = async () => {
    if (!settings) return;
    setSubmitting(true);
    setErrors([]);
    setFieldErrors({});
    try {
      const updated = await siteSettingsApi.update(buildRequest(settings));
      setSettings(updated);
      toast("success", "Settings saved.");
      await reload();
    } catch (err) {
      if (err instanceof ApiError) {
        const fe = err.getFieldErrors();
        if (Object.keys(fe).length > 0) {
          setFieldErrors(fe);
          toast("error", "Please fix the highlighted fields below before saving.");
        } else {
          const messages = err.getMessages();
          setErrors(messages);
          toast("error", messages[0] ?? "Failed to save.");
        }
      } else {
        setErrors(["Failed to save."]);
        toast("error", "Failed to save.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  return { settings, setSettings, loading, errors, fieldErrors, submitting, submit };
}

function FormBanner({ errors, fieldErrors }: { errors: string[]; fieldErrors: FieldErrors }) {
  const hasFieldErrors = Object.keys(fieldErrors).length > 0;
  return (
    <>
      {errors.length > 0 && (
        <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          <ul className="list-disc pl-5">
            {errors.map((err) => <li key={err}>{err}</li>)}
          </ul>
        </div>
      )}
      {hasFieldErrors && (
        <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          <p className="font-semibold">Please fix the highlighted fields below before saving.</p>
          <ul className="mt-2 list-disc pl-5 text-xs">
            {Object.entries(fieldErrors).map(([field, msgs]) => (
              <li key={field}><strong>{field}</strong>: {msgs.join("; ")}</li>
            ))}
          </ul>
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

function BrandingTab({ onErrorTabs }: { onErrorTabs: (s: Set<TabId>) => void }) {
  const { settings, setSettings, loading, errors, fieldErrors, submitting, submit } = useSettingsForm();

  useEffect(() => { onErrorTabs(tabsWithErrors(fieldErrors)); }, [fieldErrors, onErrorTabs]);

  if (loading) return <p className="text-muted">Loading…</p>;
  if (!settings) return <p className="text-danger">Could not load settings.</p>;

  const fe = (f: string) => fieldError(fieldErrors, f);
  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); void submit(); };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} fieldErrors={fieldErrors} />

      <Section number="01" title="Identity">
        <Field label="Church name" required error={fe("ChurchName")}>
          <input value={settings.churchName} required onChange={(e) => setSettings({ ...settings, churchName: e.target.value })} className="input" />
        </Field>
        <Field label="Tagline" error={fe("Tagline")}>
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

      <Section number="03" title="Palette">
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

      <Section number="04" title="Contact">
        <Field label="Email"><input type="email" value={settings.contactEmail ?? ""} onChange={(e) => setSettings({ ...settings, contactEmail: e.target.value })} className="input" /></Field>
        <Field label="Phone"><input value={settings.contactPhone ?? ""} onChange={(e) => setSettings({ ...settings, contactPhone: e.target.value })} className="input" /></Field>
        <Field label="Address"><input value={settings.contactAddress ?? ""} onChange={(e) => setSettings({ ...settings, contactAddress: e.target.value })} className="input" /></Field>
      </Section>

      <Section number="05" title="Social links">
        <Field label="Facebook"><input value={settings.facebookUrl ?? ""} onChange={(e) => setSettings({ ...settings, facebookUrl: e.target.value })} className="input" /></Field>
        <Field label="Instagram"><input value={settings.instagramUrl ?? ""} onChange={(e) => setSettings({ ...settings, instagramUrl: e.target.value })} className="input" /></Field>
        <Field label="YouTube"><input value={settings.youTubeUrl ?? ""} onChange={(e) => setSettings({ ...settings, youTubeUrl: e.target.value })} className="input" /></Field>
        <Field label="X (Twitter)"><input value={settings.xUrl ?? ""} onChange={(e) => setSettings({ ...settings, xUrl: e.target.value })} className="input" /></Field>
        <Field label="TikTok"><input value={settings.tikTokUrl ?? ""} onChange={(e) => setSettings({ ...settings, tikTokUrl: e.target.value })} className="input" /></Field>
        <Field label="Other (label)"><input value={settings.otherSocialLabel ?? ""} onChange={(e) => setSettings({ ...settings, otherSocialLabel: e.target.value })} className="input" /></Field>
        <Field label="Other (URL)"><input value={settings.otherSocialUrl ?? ""} onChange={(e) => setSettings({ ...settings, otherSocialUrl: e.target.value })} className="input" /></Field>
      </Section>

      <Section number="06" title="Footer">
        <Field label="Footer text"><input value={settings.footerText ?? ""} onChange={(e) => setSettings({ ...settings, footerText: e.target.value })} className="input" /></Field>
      </Section>

      <Section number="07" title="Versioning">
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

function ContentTab({ onErrorTabs }: { onErrorTabs: (s: Set<TabId>) => void }) {
  const { settings, setSettings, loading, errors, fieldErrors, submitting, submit } = useSettingsForm();
  useEffect(() => { onErrorTabs(tabsWithErrors(fieldErrors)); }, [fieldErrors, onErrorTabs]);

  if (loading) return <p className="text-muted">Loading…</p>;
  if (!settings) return <p className="text-danger">Could not load settings.</p>;

  const leaderCats = parseCategories(settings.leaderCategoriesJson);
  const eventCats = parseCategories(settings.eventCategoriesJson);
  const newsCats = parseCategories(settings.newsCategoriesJson);
  const docCats = parseCategories(settings.documentCategoriesJson);
  const sermonContexts = parseCategories(settings.sermonContextsJson);

  const setLeaderCats = (next: string[]) =>
    setSettings({ ...settings, leaderCategoriesJson: JSON.stringify(next) });
  const setEventCats = (next: string[]) =>
    setSettings({ ...settings, eventCategoriesJson: JSON.stringify(next) });
  const setNewsCats = (next: string[]) =>
    setSettings({ ...settings, newsCategoriesJson: JSON.stringify(next) });
  const setDocCats = (next: string[]) =>
    setSettings({ ...settings, documentCategoriesJson: JSON.stringify(next) });
  const setSermonContexts = (next: string[]) =>
    setSettings({ ...settings, sermonContextsJson: JSON.stringify(next) });

  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); void submit(); };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} fieldErrors={fieldErrors} />

      <Section number="01" title="Homepage hero">
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

      <Section number="02" title="Leaders page">
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

      <Section number="03" title="Events">
        <Field
          label="Event categories"
          hint="Drives the admin event-category dropdown and the public /events filter chips. Categories in use by an event cannot be removed until the events are removed or recategorized."
        >
          <CategoryListEditor values={eventCats} onChange={setEventCats} />
        </Field>
      </Section>

      <Section number="04" title="News">
        <Field
          label="News categories"
          hint="Drives the admin news-category dropdown. Categories in use by a news item cannot be removed until the items are removed or recategorized."
        >
          <CategoryListEditor values={newsCats} onChange={setNewsCats} />
        </Field>
      </Section>

      <Section number="05" title="Sermons">
        <Field
          label="Sermon contexts"
          hint="Teaching tracks shown on the admin Sermon Series editor and used as the colored marker on the public by-series page. Colors are assigned by position in this list, so reordering shifts the palette. A context still pinned to a series cannot be removed until the series is reassigned."
        >
          <CategoryListEditor values={sermonContexts} onChange={setSermonContexts} />
        </Field>
      </Section>

      <Section number="06" title="Documents">
        <Field label="Document categories" hint="Drives admin filtering and public grouping on /documents.">
          <CategoryListEditor values={docCats} onChange={setDocCats} />
        </Field>
      </Section>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Members welcome message</legend>
        <p className="text-xs text-muted">
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

function MembersTab({ onErrorTabs }: { onErrorTabs: (s: Set<TabId>) => void }) {
  const { settings, setSettings, loading, errors, fieldErrors, submitting, submit } = useSettingsForm();
  useEffect(() => { onErrorTabs(tabsWithErrors(fieldErrors)); }, [fieldErrors, onErrorTabs]);

  if (loading) return <p className="text-muted">Loading…</p>;
  if (!settings) return <p className="text-danger">Could not load settings.</p>;

  const ageGroups = parseCategories(settings.classAudienceAgeGroupsJson);
  const blogCats = parseCategories(settings.blogCategoriesJson);
  const interests = parseCategories(settings.connectCardInterestsJson);

  const setAgeGroups = (next: string[]) => setSettings({ ...settings, classAudienceAgeGroupsJson: JSON.stringify(next) });
  const setBlogCats = (next: string[]) => setSettings({ ...settings, blogCategoriesJson: JSON.stringify(next) });
  const setInterests = (next: string[]) => setSettings({ ...settings, connectCardInterestsJson: JSON.stringify(next) });

  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); void submit(); };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} fieldErrors={fieldErrors} />

      <Section number="01" title="Public page labels">
        <Field label="Get involved page label" required>
          <input
            value={settings.getInvolvedPageLabel}
            required
            maxLength={100}
            onChange={(e) => setSettings({ ...settings, getInvolvedPageLabel: e.target.value })}
            className="input"
          />
        </Field>
        <Field label="Classes page label" required>
          <input
            value={settings.classesPageLabel}
            required
            maxLength={100}
            onChange={(e) => setSettings({ ...settings, classesPageLabel: e.target.value })}
            className="input"
          />
        </Field>
        <Field label="Blog page label" required>
          <input
            value={settings.blogPageLabel}
            required
            maxLength={100}
            onChange={(e) => setSettings({ ...settings, blogPageLabel: e.target.value })}
            className="input"
          />
        </Field>
        <Field label="Connect card page label" required>
          <input
            value={settings.connectCardPageLabel}
            required
            maxLength={100}
            onChange={(e) => setSettings({ ...settings, connectCardPageLabel: e.target.value })}
            className="input"
          />
        </Field>
      </Section>

      <Section number="02" title="Classes">
        <Field label="Audience age groups" hint="Drives the dropdown on the class slot editor.">
          <CategoryListEditor values={ageGroups} onChange={setAgeGroups} />
        </Field>
        <Field label="Show recently-ended offerings" hint="When on, classes whose offerings ended within the lookback window still show on the public page.">
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={settings.showRecentPastOnPublicClasses}
              onChange={(e) => setSettings({ ...settings, showRecentPastOnPublicClasses: e.target.checked })}
            />
            Show recent past
          </label>
        </Field>
        <Field label="Recent-past lookback (days)">
          <input
            type="number" min={0} max={365}
            value={settings.recentPastClassesLookbackDays}
            onChange={(e) => setSettings({ ...settings, recentPastClassesLookbackDays: Number(e.target.value) || 0 })}
            className="input"
          />
        </Field>
      </Section>

      <Section number="03" title="Blog">
        <Field label="Categories" hint="Used by the admin category dropdown on the blog editor.">
          <CategoryListEditor values={blogCats} onChange={setBlogCats} />
        </Field>
      </Section>

      <Section number="04" title="Prayer requests">
        <Field label="Archive lookback (days)" hint="Answered prayers stay on the member list this many days before being implicitly archived.">
          <input
            type="number" min={0} max={365}
            value={settings.prayerRequestArchiveDays}
            onChange={(e) => setSettings({ ...settings, prayerRequestArchiveDays: Number(e.target.value) || 0 })}
            className="input"
          />
        </Field>
      </Section>

      <Section number="05" title="Connect card">
        <Field label="Interest checkboxes" hint="Each entry becomes a checkbox on the public connect-card form.">
          <CategoryListEditor values={interests} onChange={setInterests} />
        </Field>
        <Field label="Acknowledgment message (HTML/JSON)" hint="Optional. ProseMirror JSON; falls back to a default if blank.">
          <textarea
            value={settings.connectCardAcknowledgmentMessageJson ?? ""}
            onChange={(e) => setSettings({ ...settings, connectCardAcknowledgmentMessageJson: e.target.value || null })}
            className="input min-h-24 py-2"
          />
        </Field>
      </Section>

      <Section number="06" title="Profanity filter">
        <Field label="Wordlist" hint="Newline-delimited. Merged on top of the built-in baseline.">
          <textarea
            value={settings.profanityWordlist ?? ""}
            onChange={(e) => setSettings({ ...settings, profanityWordlist: e.target.value || null })}
            className="input min-h-24 py-2"
          />
        </Field>
        <Field label="Allowlist" hint="Newline-delimited. Suppresses matches in the merged set (false-positive recovery).">
          <textarea
            value={settings.profanityAllowlist ?? ""}
            onChange={(e) => setSettings({ ...settings, profanityAllowlist: e.target.value || null })}
            className="input min-h-24 py-2"
          />
        </Field>
      </Section>

      <SubmitButton submitting={submitting} />
      <Styles />
    </form>
  );
}

// Mirrors api/CredoCms.Domain/Email/EmailEnums.cs — keep in sync. Values are
// the enum *names* because the API serializes with JsonStringEnumConverter.
const EMAIL_PROVIDER_OPTIONS: Array<{ value: EmailProvider; label: string }> = [
  { value: "None", label: "None (log only)" },
  { value: "SendGrid", label: "SendGrid" },
  { value: "Smtp", label: "SMTP" },
];

const ADMIN_NOTIFICATION_OPTIONS: Array<{ value: AdminNotificationFrequency; label: string }> = [
  { value: "Off", label: "Off" },
  { value: "Every30Minutes", label: "Every 30 minutes" },
  { value: "Hourly", label: "Hourly" },
  { value: "Daily", label: "Daily" },
];

function EmailTab({ onErrorTabs }: { onErrorTabs: (s: Set<TabId>) => void }) {
  const { settings, setSettings, loading, errors, fieldErrors, submitting, submit } = useSettingsForm();
  const { toast } = useToast();
  const [testing, setTesting] = useState(false);
  const [overrideTo, setOverrideTo] = useState("");

  useEffect(() => { onErrorTabs(tabsWithErrors(fieldErrors)); }, [fieldErrors, onErrorTabs]);

  if (loading) return <p className="text-muted">Loading…</p>;
  if (!settings) return <p className="text-danger">Could not load settings.</p>;

  const fe = (f: string) => fieldError(fieldErrors, f);
  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); void submit(); };

  const provider = settings.emailProvider;
  const isSendGrid = provider === "SendGrid";
  const isSmtp = provider === "Smtp";

  const handleTest = async () => {
    setTesting(true);
    try {
      const result = await siteSettingsApi.testEmail({
        provider: settings.emailProvider,
        emailFromAddress: settings.emailFromAddress,
        emailFromName: settings.emailFromName,
        emailReplyToAddress: settings.emailReplyToAddress,
        sendGridApiKey: settings.sendGridApiKey,
        smtpHost: settings.smtpHost,
        smtpPort: settings.smtpPort,
        smtpUsername: settings.smtpUsername,
        smtpPassword: settings.smtpPassword,
        smtpUseSsl: settings.smtpUseSsl,
        testEmailRecipient: settings.testEmailRecipient,
        overrideToAddress: overrideTo.trim() || null,
      });
      if (result.success) {
        toast("success", result.note ?? "Test email sent.");
      } else {
        toast("error", result.errorMessage ?? "Test email failed.");
      }
    } catch {
      toast("error", "Test email request failed.");
    } finally {
      setTesting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} fieldErrors={fieldErrors} />

      <Section number="01" title="Provider">
        <Field label="Email provider" required error={fe("EmailProvider")}
          hint="Choose how outbound mail is routed. 'None' logs to the API console so you can preview without sending — useful for dev.">
          <select
            aria-label="Email provider"
            value={settings.emailProvider}
            onChange={(e) => setSettings({ ...settings, emailProvider: e.target.value as EmailProvider })}
            className="input"
          >
            {EMAIL_PROVIDER_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </Field>
        <Field label="Email enabled" hint="Master kill switch. Off means the LoggingEmailService short-circuits even when a provider is selected — keeps fresh deploys from accidentally sending mail.">
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={settings.emailEnabled}
              onChange={(e) => setSettings({ ...settings, emailEnabled: e.target.checked })} />
            Enable outbound email
          </label>
        </Field>
      </Section>

      <Section number="02" title="From / reply-to">
        <Field label="From address" required error={fe("EmailFromAddress")}>
          <input type="email" value={settings.emailFromAddress} required
            aria-label="From address"
            onChange={(e) => setSettings({ ...settings, emailFromAddress: e.target.value })}
            className="input" />
        </Field>
        <Field label="From name" required error={fe("EmailFromName")}>
          <input value={settings.emailFromName} required
            aria-label="From name"
            onChange={(e) => setSettings({ ...settings, emailFromName: e.target.value })}
            className="input" />
        </Field>
        <Field label="Reply-to address" hint="Optional. Leave blank to fall back to the From address." error={fe("EmailReplyToAddress")}>
          <input type="email" value={settings.emailReplyToAddress ?? ""}
            aria-label="Reply-to address"
            onChange={(e) => setSettings({ ...settings, emailReplyToAddress: e.target.value || null })}
            className="input" />
        </Field>
      </Section>

      {isSendGrid && (
        <Section number="03" title="SendGrid">
          <Field label="API key" required error={fe("SendGridApiKey")}
            hint="Stored masked in the UI; the saved value is sent on every save regardless.">
            <input type="password" value={settings.sendGridApiKey ?? ""}
              onChange={(e) => setSettings({ ...settings, sendGridApiKey: e.target.value || null })}
              className="input" autoComplete="off" />
          </Field>
          <Field label="Webhook secret" hint="Used to verify SendGrid event webhook signatures. Optional but recommended for production." error={fe("SendGridWebhookSecret")}>
            <input type="password" value={settings.sendGridWebhookSecret ?? ""}
              onChange={(e) => setSettings({ ...settings, sendGridWebhookSecret: e.target.value || null })}
              className="input" autoComplete="off" />
          </Field>
        </Section>
      )}

      {isSmtp && (
        <Section number="03" title="SMTP">
          <Field label="Host" required error={fe("SmtpHost")}>
            <input value={settings.smtpHost ?? ""} required
              onChange={(e) => setSettings({ ...settings, smtpHost: e.target.value || null })}
              className="input" placeholder="smtp.example.com" />
          </Field>
          <Field label="Port" required error={fe("SmtpPort")}>
            <input type="number" min={1} max={65535} value={settings.smtpPort}
              aria-label="SMTP port"
              onChange={(e) => setSettings({ ...settings, smtpPort: Number(e.target.value) })}
              className="input" placeholder="587" />
          </Field>
          <Field label="Username" error={fe("SmtpUsername")}>
            <input value={settings.smtpUsername ?? ""}
              onChange={(e) => setSettings({ ...settings, smtpUsername: e.target.value || null })}
              className="input" autoComplete="off" />
          </Field>
          <Field label="Password" error={fe("SmtpPassword")}>
            <input type="password" value={settings.smtpPassword ?? ""}
              onChange={(e) => setSettings({ ...settings, smtpPassword: e.target.value || null })}
              className="input" autoComplete="off" />
          </Field>
          <Field label="Use SSL/TLS">
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={settings.smtpUseSsl}
                onChange={(e) => setSettings({ ...settings, smtpUseSsl: e.target.checked })} />
              Encrypt the connection (typically on for port 587/465)
            </label>
          </Field>
        </Section>
      )}

      <Section number={isSendGrid || isSmtp ? "04" : "03"} title="Subject prefixes">
        <Field label="News broadcasts" hint='Prepended to "News" auto-broadcast subjects, e.g. "[News] Sunday recap".' error={fe("EmailSubjectPrefixNews")}>
          <input value={settings.emailSubjectPrefixNews}
            aria-label="News email subject prefix"
            onChange={(e) => setSettings({ ...settings, emailSubjectPrefixNews: e.target.value })}
            className="input" />
        </Field>
        <Field label="Blog broadcasts" hint='Prepended to "Blog" auto-broadcast subjects.' error={fe("EmailSubjectPrefixBlog")}>
          <input value={settings.emailSubjectPrefixBlog}
            aria-label="Blog email subject prefix"
            onChange={(e) => setSettings({ ...settings, emailSubjectPrefixBlog: e.target.value })}
            className="input" />
        </Field>
      </Section>

      <Section number={isSendGrid || isSmtp ? "05" : "04"} title="Admin notifications">
        <Field label="Digest frequency" hint="How often the API's digest job emails admins about new connect cards, prayer requests, etc." error={fe("AdminNotificationFrequency")}>
          <select
            aria-label="Admin notification frequency"
            value={settings.adminNotificationFrequency}
            onChange={(e) => setSettings({ ...settings, adminNotificationFrequency: e.target.value as AdminNotificationFrequency })}
            className="input"
          >
            {ADMIN_NOTIFICATION_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </Field>
      </Section>

      <Section number={isSendGrid || isSmtp ? "06" : "05"} title="Test send">
        <Field label="Default test recipient" hint="Optional. Used when no override is supplied below." error={fe("TestEmailRecipient")}>
          <input type="email" value={settings.testEmailRecipient ?? ""}
            onChange={(e) => setSettings({ ...settings, testEmailRecipient: e.target.value || null })}
            className="input" placeholder="you@example.org" />
        </Field>
        <Field label="Send a one-off test"
          hint="Uses the values on this form (even if you haven't saved). Sends to the override address below, or to your admin email if blank.">
          <div className="flex flex-wrap items-center gap-2">
            <input type="email" value={overrideTo}
              onChange={(e) => setOverrideTo(e.target.value)}
              className="input flex-1" placeholder="override@example.org (optional)" />
            <button type="button" disabled={testing} onClick={handleTest}
              className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-card px-4 text-sm font-medium hover:bg-panel-alt disabled:opacity-50">
              {testing ? "Sending…" : "Send test"}
            </button>
          </div>
        </Field>
      </Section>

      <SubmitButton submitting={submitting} />
      <Styles />
    </form>
  );
}

function IntegrationsTab({ onErrorTabs }: { onErrorTabs: (s: Set<TabId>) => void }) {
  const { settings, setSettings, loading, errors, fieldErrors, submitting, submit } = useSettingsForm();
  useEffect(() => { onErrorTabs(tabsWithErrors(fieldErrors)); }, [fieldErrors, onErrorTabs]);

  if (loading) return <p className="text-muted">Loading…</p>;
  if (!settings) return <p className="text-danger">Could not load settings.</p>;

  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); void submit(); };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} fieldErrors={fieldErrors} />

      <Section number="01" title="Cloudflare Turnstile">
        <Field label="Site key (public)" hint="Rendered into the connect-card page's Turnstile widget.">
          <input
            value={settings.cloudflareTurnstileSiteKey ?? ""}
            maxLength={200}
            onChange={(e) => setSettings({ ...settings, cloudflareTurnstileSiteKey: e.target.value || null })}
            className="input"
          />
        </Field>
        <Field label="Secret key" hint="Used server-side for siteverify. Treated as a secret in transit.">
          <input
            type="password"
            value={settings.cloudflareTurnstileSecretKey ?? ""}
            maxLength={200}
            onChange={(e) => setSettings({ ...settings, cloudflareTurnstileSecretKey: e.target.value || null })}
            className="input"
          />
        </Field>
      </Section>

      <Section number="02" title="Facebook OAuth">
        <Field label="App ID">
          <input
            value={settings.facebookOAuthAppId ?? ""}
            maxLength={200}
            onChange={(e) => setSettings({ ...settings, facebookOAuthAppId: e.target.value || null })}
            className="input"
          />
        </Field>
        <Field label="App secret">
          <input
            type="password"
            value={settings.facebookOAuthAppSecret ?? ""}
            maxLength={200}
            onChange={(e) => setSettings({ ...settings, facebookOAuthAppSecret: e.target.value || null })}
            className="input"
          />
        </Field>
        <Field label="Enable Facebook sign-in" hint="When on, the /login page shows a Continue with Facebook button.">
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={settings.facebookLoginEnabled}
              onChange={(e) => setSettings({ ...settings, facebookLoginEnabled: e.target.checked })}
            />
            Enabled
          </label>
        </Field>
      </Section>

      <SubmitButton submitting={submitting} />
      <Styles />
    </form>
  );
}

function AdvancedTab({ onErrorTabs }: { onErrorTabs: (s: Set<TabId>) => void }) {
  const { settings, setSettings, loading, errors, fieldErrors, submitting, submit } = useSettingsForm();
  useEffect(() => { onErrorTabs(tabsWithErrors(fieldErrors)); }, [fieldErrors, onErrorTabs]);
  const [rebuildState, setRebuildState] = useState<"idle" | "queued">("idle");

  if (loading) return <p className="text-muted">Loading…</p>;
  if (!settings) return <p className="text-danger">Could not load settings.</p>;

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
      <FormBanner errors={errors} fieldErrors={fieldErrors} />

      <Section number="01" title="Image uploads">
        <Field label="Max width (px)" hint="Wider images are resized down on upload. Range: 800–5000.">
          <input
            type="number"
            min={800}
            max={5000}
            value={settings.imageMaxWidth}
            aria-label="Max image width in pixels"
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
            aria-label="JPEG and WebP quality"
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
            aria-label="Max image size in megabytes"
            onChange={(e) =>
              setSettings({ ...settings, maxImageSizeBytes: Number(e.target.value) * 1024 * 1024 })
            }
            className="input"
          />
        </Field>
      </Section>

      <Section number="02" title="Document uploads">
        <Field label="Max document size (MB)" hint="1–200 MB. Limits PDF uploads under /admin/documents.">
          <input
            type="number"
            min={1}
            max={200}
            value={Math.round(settings.maxDocumentSizeBytes / (1024 * 1024))}
            aria-label="Max document size in megabytes"
            onChange={(e) =>
              setSettings({ ...settings, maxDocumentSizeBytes: Number(e.target.value) * 1024 * 1024 })
            }
            className="input"
          />
        </Field>
      </Section>

      <Section number="03" title="SEO">
        <Field label="Default meta description" hint="Used when an entity has no description and no excerpt to fall back on. Up to 300 chars.">
          <textarea
            value={settings.defaultMetaDescription ?? ""}
            maxLength={300}
            aria-label="Default meta description"
            onChange={(e) => setSettings({ ...settings, defaultMetaDescription: e.target.value })}
            className="input min-h-20 py-2"
          />
        </Field>
      </Section>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Search</legend>
        <p className="text-xs text-muted">
          Re-indexes all published Pages, News items, Leaders, and Documents. Safe to run; triggers a
          background job.
        </p>
        <button
          type="button"
          onClick={handleRebuild}
          disabled={rebuildState === "queued"}
          className="inline-flex h-9 items-center justify-center rounded-md border bg-card px-3 text-sm hover:bg-panel-alt disabled:opacity-50"
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
          <button type="button" onClick={() => moveUp(i)} aria-label="Move up" className="iconbtn inline-flex items-center justify-center">
            <ChevronUp aria-hidden="true" strokeWidth={1.75} className="h-4 w-4" />
          </button>
          <button type="button" onClick={() => moveDown(i)} aria-label="Move down" className="iconbtn inline-flex items-center justify-center">
            <ChevronDown aria-hidden="true" strokeWidth={1.75} className="h-4 w-4" />
          </button>
          <button type="button" onClick={() => remove(i)} aria-label="Remove" className="iconbtn inline-flex items-center justify-center text-danger">
            <X aria-hidden="true" strokeWidth={1.75} className="h-4 w-4" />
          </button>
        </div>
      ))}
      <button
        type="button"
        onClick={add}
        className="inline-flex h-8 items-center justify-center rounded-md border px-3 text-xs hover:bg-panel-alt"
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

function Section({
  number,
  title,
  children,
}: {
  number: string;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section className="space-y-4 pt-2">
      <SectionHead number={number} title={title} />
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">{children}</div>
    </section>
  );
}

function Field({ label, hint, required, error, children }: { label: string; hint?: string; required?: boolean; error?: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className={cn("mb-1 block text-sm font-medium", error && "text-danger")}>
        {label}{required && <span className="text-danger"> *</span>}
      </span>
      <div className={error ? "[&_.input]:border-danger [&_.input]:ring-1 [&_.input]:ring-danger/30" : ""}>
        {children}
      </div>
      {error && <span className="mt-1 block text-xs text-danger">{error}</span>}
      {!error && hint && <span className="mt-1 block text-xs text-muted">{hint}</span>}
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
