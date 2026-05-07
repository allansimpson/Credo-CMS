import { useState } from "react";
import { Link } from "react-router-dom";
import { Facebook, Instagram, Youtube, Twitter, Music2, Link as LinkIcon } from "lucide-react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { clearConsent, readConsent, type ConsentValue } from "@/components/shared/CookieConsentBanner";

export function PublicFooter() {
  const { settings } = useSiteSettings();
  const [showCookieModal, setShowCookieModal] = useState(false);
  if (!settings) return null;

  const showCookiePreferences = settings.analyticsProvider === 1;

  // Phase 6 S4: every configured social URL renders a Lucide brand icon.
  const socialLinks: { url: string | null; icon: typeof Facebook; label: string }[] = [
    { url: settings.facebookUrl, icon: Facebook, label: "Facebook" },
    { url: settings.instagramUrl, icon: Instagram, label: "Instagram" },
    { url: settings.youTubeUrl, icon: Youtube, label: "YouTube" },
    { url: settings.xUrl, icon: Twitter, label: "X" },
    { url: settings.tikTokUrl, icon: Music2, label: "TikTok" },
  ];

  return (
    <footer className="mt-16 border-t bg-panel-alt/30">
      <div className="mx-auto grid max-w-6xl gap-8 px-4 py-10 md:grid-cols-3">
        <div>
          <h3 className="font-semibold">{settings.churchName}</h3>
          {settings.tagline && (
            <p className="mt-1 text-sm text-muted">{settings.tagline}</p>
          )}
        </div>

        <div>
          <h4 className="text-sm font-semibold uppercase tracking-wide text-muted">
            Contact
          </h4>
          <address className="mt-2 not-italic text-sm leading-6">
            {settings.contactAddress && <div>{settings.contactAddress}</div>}
            {settings.contactPhone && <div>{settings.contactPhone}</div>}
            {settings.contactEmail && (
              <a href={`mailto:${settings.contactEmail}`} className="text-primary hover:underline">
                {settings.contactEmail}
              </a>
            )}
          </address>
        </div>

        <div>
          <h4 className="text-sm font-semibold uppercase tracking-wide text-muted">
            Connect
          </h4>
          <ul className="mt-2 flex gap-4">
            {socialLinks.map(({ url, icon: Icon, label }) => url ? (
              <li key={label}>
                <a
                  href={url}
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label={`${settings.churchName} on ${label}`}
                  className="text-muted hover:text-primary focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2"
                >
                  <Icon className="h-5 w-5" aria-hidden="true" />
                </a>
              </li>
            ) : null)}
            {settings.otherSocialUrl && settings.otherSocialLabel && (
              <li>
                <a
                  href={settings.otherSocialUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label={`${settings.churchName} on ${settings.otherSocialLabel}`}
                  className="text-muted hover:text-primary focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2"
                >
                  <LinkIcon className="h-5 w-5" aria-hidden="true" />
                </a>
              </li>
            )}
          </ul>
        </div>
      </div>

      <div className="border-t bg-background">
        <div className="mx-auto flex max-w-6xl flex-col gap-2 px-4 py-4 text-sm text-muted sm:flex-row sm:justify-between">
          <span>{settings.footerText ?? `© ${new Date().getFullYear()} ${settings.churchName}`}</span>
          <div className="flex gap-4">
            <Link to="/privacy-policy" className="hover:text-primary">Privacy</Link>
            <Link to="/terms-of-service" className="hover:text-primary">Terms</Link>
            {showCookiePreferences && (
              <button
                type="button"
                onClick={() => setShowCookieModal(true)}
                className="hover:text-primary focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2"
              >
                Cookie Preferences
              </button>
            )}
          </div>
        </div>
      </div>

      {showCookieModal && (
        <CookiePreferencesModal onClose={() => setShowCookieModal(false)} />
      )}
    </footer>
  );
}

function CookiePreferencesModal({ onClose }: { onClose: () => void }) {
  const current: ConsentValue | null = readConsent();
  const handleRevoke = () => {
    clearConsent();
    onClose();
    // Reload so the banner re-renders cleanly. Per spec, revocation is "as
    // easy as opt-in" and the user is taken back to a no-tracking baseline.
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
        className="w-full max-w-md border border-gray-200 bg-white p-6 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 id="cookie-prefs-title" className="text-base font-semibold">Cookie Preferences</h2>
        <p className="mt-2 text-sm text-gray-600">
          Current status: <strong>{current ?? "no decision yet"}</strong>
        </p>
        <p className="mt-2 text-sm text-gray-600">
          Revoking your consent will clear the cookie and reload the page.
          You'll be asked again on your next visit.
        </p>
        <div className="mt-4 flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="border border-gray-300 px-4 py-2 text-sm hover:bg-gray-50"
          >
            Close
          </button>
          <button
            type="button"
            onClick={handleRevoke}
            className="bg-gray-900 px-4 py-2 text-sm text-white hover:bg-gray-800"
          >
            Revoke consent
          </button>
        </div>
      </div>
    </div>
  );
}
