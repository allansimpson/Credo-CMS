import { useState } from "react";
import { Link } from "react-router-dom";
import { Facebook, Instagram, Youtube, Twitter, Music2, Link as LinkIcon } from "lucide-react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { clearConsent, readConsent, type ConsentValue } from "@/components/shared/CookieConsentBanner";
import type { PublicTemplate } from "@/types/api";
import { Eyebrow } from "./Eyebrow";

export interface PublicFooterProps {
  template: PublicTemplate;
}

interface SocialLink {
  url: string | null | undefined;
  icon: typeof Facebook;
  label: string;
}

interface NavGroup {
  heading: string;
  items: { to: string; label: string }[];
}

const FOOTER_NAV: NavGroup[] = [
  {
    heading: "Visit",
    items: [
      { to: "/im-new", label: "I'm new" },
      { to: "/about", label: "About" },
      { to: "/service-times", label: "Service times" },
      { to: "/contact", label: "Contact" },
    ],
  },
  {
    heading: "Connect",
    items: [
      { to: "/sermons", label: "Sermons" },
      { to: "/events", label: "Events" },
      { to: "/news", label: "News" },
      { to: "/leaders", label: "Leaders" },
    ],
  },
  {
    heading: "Give & serve",
    items: [
      { to: "/give", label: "Give" },
      { to: "/im-new", label: "Serve" },
      { to: "/beliefs", label: "What we believe" },
    ],
  },
];

/**
 * Public-site footer. Two visual treatments selected by `template`:
 *   - Editorial: dark inset, 4-column (logo block + 3 nav columns),
 *     accent strip, "Built on Credo CMS" credit.
 *   - Quiet: light panel-alt background, 3-column (tagline + 2 nav
 *     columns), no dark inset.
 *
 * Content (nav links, social links, policy links) is identical across
 * templates.
 */
export function PublicFooter({ template }: PublicFooterProps) {
  const { settings } = useSiteSettings();
  const [showCookieModal, setShowCookieModal] = useState(false);
  if (!settings) return null;

  const isQuiet = template === 1;
  const churchName = settings.churchName;
  const showCookiePreferences = settings.analyticsProvider === "Ga4";

  const socialLinks: SocialLink[] = [
    { url: settings.facebookUrl, icon: Facebook, label: "Facebook" },
    { url: settings.instagramUrl, icon: Instagram, label: "Instagram" },
    { url: settings.youTubeUrl, icon: Youtube, label: "YouTube" },
    { url: settings.xUrl, icon: Twitter, label: "X" },
    { url: settings.tikTokUrl, icon: Music2, label: "TikTok" },
  ];

  return (
    <>
      {isQuiet ? (
        <QuietFooter
          churchName={churchName}
          tagline={settings.tagline}
          contactAddress={settings.contactAddress}
          contactEmail={settings.contactEmail}
          contactPhone={settings.contactPhone}
          socialLinks={socialLinks}
          otherSocialUrl={settings.otherSocialUrl}
          otherSocialLabel={settings.otherSocialLabel}
          footerText={settings.footerText}
          showCookiePreferences={showCookiePreferences}
          onCookiePreferences={() => setShowCookieModal(true)}
        />
      ) : (
        <EditorialFooter
          churchName={churchName}
          tagline={settings.tagline}
          contactAddress={settings.contactAddress}
          contactEmail={settings.contactEmail}
          contactPhone={settings.contactPhone}
          socialLinks={socialLinks}
          otherSocialUrl={settings.otherSocialUrl}
          otherSocialLabel={settings.otherSocialLabel}
          footerText={settings.footerText}
          showCookiePreferences={showCookiePreferences}
          onCookiePreferences={() => setShowCookieModal(true)}
        />
      )}

      {showCookieModal ? (
        <CookiePreferencesModal onClose={() => setShowCookieModal(false)} />
      ) : null}
    </>
  );
}

interface FooterContentProps {
  churchName: string;
  tagline: string | null;
  contactAddress: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  socialLinks: SocialLink[];
  otherSocialUrl: string | null;
  otherSocialLabel: string | null;
  footerText: string | null;
  showCookiePreferences: boolean;
  onCookiePreferences: () => void;
}

function EditorialFooter({
  churchName,
  tagline,
  contactAddress,
  contactEmail,
  contactPhone,
  socialLinks,
  otherSocialUrl,
  otherSocialLabel,
  footerText,
  showCookiePreferences,
  onCookiePreferences,
}: FooterContentProps) {
  return (
    <footer className="mt-20 bg-inset text-inset-foreground">
      <div className="mx-auto grid max-w-7xl gap-10 px-4 py-14 sm:px-6 md:grid-cols-4 lg:px-8">
        <div>
          <div className="flex items-center gap-2.5">
            <span className="flex h-8 w-8 items-center justify-center bg-accent text-xs font-bold text-accent-foreground">
              H
            </span>
            <span className="text-[11px] font-medium uppercase tracking-[0.18em] text-inset-foreground">
              {churchName}
            </span>
          </div>
          {tagline ? (
            <p className="mt-3 text-sm leading-6 text-inset-foreground/80">{tagline}</p>
          ) : null}
          <address className="mt-4 not-italic text-sm leading-6 text-inset-foreground/80">
            {contactAddress ? <div>{contactAddress}</div> : null}
            {contactPhone ? <div className="font-mono mt-1">{contactPhone}</div> : null}
            {contactEmail ? (
              <a
                href={`mailto:${contactEmail}`}
                className="font-mono mt-1 inline-block hover:text-inset-foreground"
              >
                {contactEmail}
              </a>
            ) : null}
          </address>
        </div>

        {FOOTER_NAV.map((group) => (
          <div key={group.heading}>
            <Eyebrow tone="inverse">{group.heading}</Eyebrow>
            <ul className="mt-3 space-y-2">
              {group.items.map((item) => (
                <li key={item.to}>
                  <Link
                    to={item.to}
                    className="text-sm text-inset-foreground/80 hover:text-inset-foreground"
                  >
                    {item.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>

      <div className="border-t border-inset-foreground/20">
        <div className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-6 sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
          <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-xs text-inset-foreground/70">
            <span>{footerText ?? `© ${new Date().getFullYear()} ${churchName}`}</span>
            <Link to="/privacy-policy" className="hover:text-inset-foreground">Privacy</Link>
            <Link to="/terms-of-service" className="hover:text-inset-foreground">Terms</Link>
            {showCookiePreferences ? (
              <button
                type="button"
                onClick={onCookiePreferences}
                className="hover:text-inset-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-inset-foreground focus-visible:ring-offset-2 focus-visible:ring-offset-inset"
              >
                Cookie preferences
              </button>
            ) : null}
          </div>
          <SocialIcons
            churchName={churchName}
            socialLinks={socialLinks}
            otherSocialUrl={otherSocialUrl}
            otherSocialLabel={otherSocialLabel}
            tone="inverse"
          />
        </div>
      </div>
    </footer>
  );
}

function QuietFooter({
  churchName,
  tagline,
  contactAddress,
  contactEmail,
  contactPhone,
  socialLinks,
  otherSocialUrl,
  otherSocialLabel,
  footerText,
  showCookiePreferences,
  onCookiePreferences,
}: FooterContentProps) {
  return (
    <footer className="mt-24 bg-panel-alt text-foreground">
      <div className="mx-auto grid max-w-7xl gap-12 px-4 py-20 sm:px-6 md:grid-cols-3 lg:px-8">
        <div className="md:col-span-1">
          <p className="font-heading text-2xl tracking-[-0.022em]">{churchName}</p>
          {tagline ? <p className="mt-3 text-sm leading-6 text-fg-soft">{tagline}</p> : null}
          <address className="mt-6 not-italic text-sm leading-6 text-fg-soft">
            {contactAddress ? <div>{contactAddress}</div> : null}
            {contactPhone ? <div className="font-mono mt-1">{contactPhone}</div> : null}
            {contactEmail ? (
              <a
                href={`mailto:${contactEmail}`}
                className="font-mono mt-1 inline-block hover:text-foreground"
              >
                {contactEmail}
              </a>
            ) : null}
          </address>
        </div>

        <div className="grid grid-cols-2 gap-8 md:col-span-2">
          {FOOTER_NAV.slice(0, 2).map((group) => (
            <div key={group.heading}>
              <Eyebrow>{group.heading}</Eyebrow>
              <ul className="mt-4 space-y-3">
                {group.items.map((item) => (
                  <li key={item.to}>
                    <Link to={item.to} className="text-sm text-foreground hover:text-accent">
                      {item.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>

      <div className="border-t border-border">
        <div className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-6 sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
          <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-xs text-muted">
            <span>{footerText ?? `© ${new Date().getFullYear()} ${churchName}`}</span>
            <Link to="/privacy-policy" className="hover:text-foreground">Privacy</Link>
            <Link to="/terms-of-service" className="hover:text-foreground">Terms</Link>
            {showCookiePreferences ? (
              <button
                type="button"
                onClick={onCookiePreferences}
                className="hover:text-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-foreground focus-visible:ring-offset-2"
              >
                Cookie preferences
              </button>
            ) : null}
          </div>
          <SocialIcons
            churchName={churchName}
            socialLinks={socialLinks}
            otherSocialUrl={otherSocialUrl}
            otherSocialLabel={otherSocialLabel}
            tone="default"
          />
        </div>
      </div>
    </footer>
  );
}

interface SocialIconsProps {
  churchName: string;
  socialLinks: SocialLink[];
  otherSocialUrl: string | null;
  otherSocialLabel: string | null;
  tone: "default" | "inverse";
}

function SocialIcons({ churchName, socialLinks, otherSocialUrl, otherSocialLabel, tone }: SocialIconsProps) {
  const iconClass =
    tone === "inverse"
      ? "text-inset-foreground/70 hover:text-inset-foreground"
      : "text-muted hover:text-foreground";
  return (
    <ul className="flex items-center gap-4">
      {socialLinks.map(({ url, icon: Icon, label }) =>
        url ? (
          <li key={label}>
            <a
              href={url}
              target="_blank"
              rel="noopener noreferrer"
              aria-label={`${churchName} on ${label}`}
              className={`focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 ${iconClass}`}
            >
              <Icon className="h-5 w-5" aria-hidden="true" />
            </a>
          </li>
        ) : null,
      )}
      {otherSocialUrl && otherSocialLabel ? (
        <li>
          <a
            href={otherSocialUrl}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={`${churchName} on ${otherSocialLabel}`}
            className={`focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 ${iconClass}`}
          >
            <LinkIcon className="h-5 w-5" aria-hidden="true" />
          </a>
        </li>
      ) : null}
    </ul>
  );
}

function CookiePreferencesModal({ onClose }: { onClose: () => void }) {
  const current: ConsentValue | null = readConsent();
  const handleRevoke = () => {
    clearConsent();
    onClose();
    if (typeof window !== "undefined") window.location.reload();
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="cookie-prefs-title"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={onClose}
    >
      <div
        className="w-full max-w-md border border-border bg-background p-6 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 id="cookie-prefs-title" className="font-heading text-lg font-semibold">
          Cookie preferences
        </h2>
        <p className="mt-2 text-sm text-fg-soft">
          Current status: <strong>{current ?? "no decision yet"}</strong>
        </p>
        <p className="mt-2 text-sm text-fg-soft">
          Revoking your consent will clear the cookie and reload the page. You'll
          be asked again on your next visit.
        </p>
        <div className="mt-4 flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="border border-border px-4 py-2 text-sm hover:bg-panel-alt"
          >
            Close
          </button>
          <button
            type="button"
            onClick={handleRevoke}
            className="bg-foreground px-4 py-2 text-sm text-background hover:bg-fg-soft"
          >
            Revoke consent
          </button>
        </div>
      </div>
    </div>
  );
}
