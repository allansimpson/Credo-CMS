import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useSiteSettings } from "@/lib/SiteSettingsContext";

/**
 * Cookie consent banner. Renders only when:
 *   - PublicSiteSettings.analyticsProvider === Ga4 (1)
 *   - settings.ga4ConsentBannerEnabled is true
 *   - the cms_consent cookie is absent (no decision yet)
 *
 * On Accept: writes cms_consent=accepted (1y), injects gtag.js, dismisses.
 * On Decline: writes cms_consent=declined (1y), dismisses, does not load gtag.
 *
 * The body footer's permanent "Cookie Preferences" link (rendered by
 * PublicFooter) is the revoke surface — clearing the cookie re-shows
 * the banner on next page load.
 */
export const CONSENT_COOKIE_NAME = "cms_consent";
export type ConsentValue = "accepted" | "declined";

export function readConsent(): ConsentValue | null {
  if (typeof document === "undefined") return null;
  const m = document.cookie.match(new RegExp(`(?:^|; )${CONSENT_COOKIE_NAME}=([^;]+)`));
  if (!m) return null;
  const v = decodeURIComponent(m[1]);
  return v === "accepted" || v === "declined" ? v : null;
}

export function writeConsent(value: ConsentValue) {
  const oneYear = 60 * 60 * 24 * 365;
  document.cookie =
    `${CONSENT_COOKIE_NAME}=${value}; max-age=${oneYear}; path=/; SameSite=Lax`;
}

export function clearConsent() {
  document.cookie = `${CONSENT_COOKIE_NAME}=; max-age=0; path=/`;
}

export function CookieConsentBanner() {
  const { settings, reload } = useSiteSettings();
  const [consent, setConsent] = useState<ConsentValue | null>(() => readConsent());

  // Inject gtag.js when consent flips to accepted and a measurement id is set.
  useEffect(() => {
    if (consent !== "accepted") return;
    if (!settings || settings.analyticsProvider !== 1) return;
    const id = settings.ga4MeasurementId;
    if (!id || typeof document === "undefined") return;
    if (document.querySelector(`script[data-ga4="${id}"]`)) return; // already loaded

    const tag = document.createElement("script");
    tag.async = true;
    tag.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(id)}`;
    tag.dataset.ga4 = id;
    document.head.appendChild(tag);

    const init = document.createElement("script");
    init.dataset.ga4Init = "true";
    init.text = `window.dataLayer=window.dataLayer||[];function gtag(){dataLayer.push(arguments);}gtag('js',new Date());gtag('config','${id}');`;
    document.head.appendChild(init);
  }, [consent, settings]);

  if (!settings || settings.analyticsProvider !== 1) return null;
  if (!settings.ga4ConsentBannerEnabled) return null;
  if (consent !== null) return null;

  const positionClasses = settings.ga4ConsentBannerPosition === 1
    ? "left-0 right-0 bottom-0" // BottomFull
    : "right-4 bottom-4 max-w-sm w-full sm:max-w-md"; // BottomRight

  const handleAccept = () => {
    writeConsent("accepted");
    setConsent("accepted");
    // The server omits Ga4MeasurementId from the public bootstrap until the
    // consent cookie is set + accepted. Re-fetch so the id flows through and
    // the loader effect can inject gtag with the correct measurement.
    reload().catch(() => { /* loader will fall through with whatever's in context */ });
  };
  const handleDecline = () => { writeConsent("declined"); setConsent("declined"); };

  return (
    <div
      role="dialog"
      aria-live="polite"
      aria-labelledby="cookie-consent-title"
      className={`fixed z-50 ${positionClasses} border border-gray-200 bg-white p-4 shadow-lg sm:p-5`}
    >
      <h2 id="cookie-consent-title" className="text-sm font-medium text-gray-900">
        Cookies
      </h2>
      <p className="mt-2 text-sm text-gray-600">
        We use cookies to understand how visitors use our site. You can
        accept or decline.{" "}
        {settings.cookiePolicyPageSlug ? (
          <>
            See our{" "}
            <Link
              to={`/${settings.cookiePolicyPageSlug}`}
              className="underline focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2"
            >
              Cookie Policy
            </Link>{" "}
            for details.
          </>
        ) : null}
      </p>
      <div className="mt-3 flex gap-2">
        <button
          type="button"
          onClick={handleAccept}
          className="bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2"
        >
          Accept
        </button>
        <button
          type="button"
          onClick={handleDecline}
          className="border border-gray-300 px-4 py-2 text-sm font-medium hover:bg-gray-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2"
        >
          Decline
        </button>
      </div>
    </div>
  );
}
