import { Link } from "react-router-dom";
import { Facebook, Instagram, Youtube } from "lucide-react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";

export function PublicFooter() {
  const { settings } = useSiteSettings();
  if (!settings) return null;

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
            {settings.facebookUrl && (
              <li>
                <a href={settings.facebookUrl} aria-label="Facebook" className="text-muted hover:text-primary">
                  <Facebook className="h-5 w-5" />
                </a>
              </li>
            )}
            {settings.instagramUrl && (
              <li>
                <a href={settings.instagramUrl} aria-label="Instagram" className="text-muted hover:text-primary">
                  <Instagram className="h-5 w-5" />
                </a>
              </li>
            )}
            {settings.youTubeUrl && (
              <li>
                <a href={settings.youTubeUrl} aria-label="YouTube" className="text-muted hover:text-primary">
                  <Youtube className="h-5 w-5" />
                </a>
              </li>
            )}
            {settings.otherSocialUrl && settings.otherSocialLabel && (
              <li>
                <a href={settings.otherSocialUrl} className="text-sm text-muted hover:text-primary">
                  {settings.otherSocialLabel}
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
          </div>
        </div>
      </div>
    </footer>
  );
}
