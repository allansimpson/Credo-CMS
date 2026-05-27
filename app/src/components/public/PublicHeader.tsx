import { useState } from "react";
import { Link, NavLink } from "react-router-dom";
import { Menu, X } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { PublicTemplate } from "@/types/api";
import { AnnouncementBar } from "@/components/shared/AnnouncementBar";
import { BtnLink } from "./Btn";
import { Eyebrow } from "./Eyebrow";
import { PIcon } from "./PIcon";

/** Page identifiers used for active-nav styling. */
export type PublicActivePage =
  | "home"
  | "about"
  | "im-new"
  | "beliefs"
  | "sermons"
  | "events"
  | "news"
  | "leaders"
  | "contact"
  | "members"
  | null;

export interface PublicHeaderProps {
  template: PublicTemplate;
  activePage: PublicActivePage;
}

interface NavItem {
  to: string;
  label: string;
  page: Exclude<PublicActivePage, null>;
}

const NAV_ITEMS: NavItem[] = [
  { to: "/about", label: "About", page: "about" },
  { to: "/im-new", label: "I'm New", page: "im-new" },
  { to: "/sermons", label: "Sermons", page: "sermons" },
  { to: "/events", label: "Events", page: "events" },
  { to: "/news", label: "News", page: "news" },
  { to: "/leaders", label: "Leaders", page: "leaders" },
  { to: "/contact", label: "Contact", page: "contact" },
];

/**
 * Public-site header. Two visual treatments selected by `template`:
 *   - Editorial: includes the announcement bar, three-column row with
 *     primary CTA "Plan a visit", dark inset announcement strip.
 *   - Quiet: no announcement bar by default, single-row header, lighter
 *     chrome, primary CTA "Visit Sunday".
 *
 * Content (nav links, sign-in target, CTA target) is identical across
 * templates per the handoff's "match content shape" rule.
 */
export function PublicHeader({ template, activePage }: PublicHeaderProps) {
  const { user } = useAuth();
  const { settings } = useSiteSettings();
  const [mobileOpen, setMobileOpen] = useState(false);

  const churchName = settings?.churchName ?? "Credo CMS";
  const isQuiet = template === 1;
  const ctaLabel = isQuiet ? "Visit Sunday" : "Plan a visit";
  const ctaHref = "/im-new";

  return (
    <header className="bg-background">
      {/* Editorial keeps the announcement bar; Quiet omits by default. */}
      {!isQuiet ? <AnnouncementBar /> : null}

      <div className="border-b border-border-soft">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-6 px-4 py-4 sm:px-6 lg:px-8">
          <Link to="/" className="inline-flex items-baseline gap-1" aria-label={`${churchName} home`}>
            {isQuiet ? (
              <span className="font-heading text-xl font-semibold tracking-[-0.022em]">
                {churchName}
                <span className="text-muted">.church</span>
              </span>
            ) : (
              <>
                <Eyebrow accent>HC</Eyebrow>
                <span className="font-heading text-base font-semibold tracking-[-0.018em]">
                  {churchName}
                </span>
              </>
            )}
          </Link>

          {/* Desktop nav */}
          <nav className="hidden md:flex md:items-center md:gap-6" aria-label="Primary">
            {NAV_ITEMS.map((item) => (
              <NavLink
                key={item.page}
                to={item.to}
                className={({ isActive }) =>
                  [
                    "text-sm transition-colors",
                    isActive || activePage === item.page
                      ? "text-foreground font-medium"
                      : "text-fg-soft hover:text-foreground",
                  ].join(" ")
                }
                aria-current={activePage === item.page ? "page" : undefined}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="flex items-center gap-3">
            {user ? (
              <Link
                to="/members/home"
                className="hidden sm:inline-flex text-sm text-fg-soft hover:text-foreground"
              >
                Members
              </Link>
            ) : (
              <Link
                to="/login"
                className="hidden sm:inline-flex text-sm text-fg-soft hover:text-foreground"
              >
                Sign in
              </Link>
            )}
            <BtnLink to={ctaHref} variant="primary" size="sm" className="hidden sm:inline-flex">
              {ctaLabel}
            </BtnLink>
            {/* Mobile menu trigger */}
            <button
              type="button"
              onClick={() => setMobileOpen((v) => !v)}
              aria-expanded={mobileOpen}
              aria-controls="public-mobile-nav"
              aria-label={mobileOpen ? "Close menu" : "Open menu"}
              className="md:hidden inline-flex items-center justify-center w-10 h-10 border border-border focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-foreground focus-visible:ring-offset-2"
            >
              <PIcon icon={mobileOpen ? X : Menu} size="md" />
            </button>
          </div>
        </div>

        {/* Mobile nav */}
        {mobileOpen ? (
          <nav
            id="public-mobile-nav"
            aria-label="Primary mobile"
            className="md:hidden border-t border-border-soft"
          >
            <ul className="px-4 py-3 sm:px-6">
              {NAV_ITEMS.map((item) => (
                <li key={item.page}>
                  <Link
                    to={item.to}
                    onClick={() => setMobileOpen(false)}
                    className={[
                      "block py-2 text-base",
                      activePage === item.page ? "text-foreground font-medium" : "text-fg-soft",
                    ].join(" ")}
                  >
                    {item.label}
                  </Link>
                </li>
              ))}
              <li className="pt-3 mt-2 border-t border-border-soft">
                {user ? (
                  <Link to="/members/home" className="block py-2 text-base text-fg-soft">
                    Members
                  </Link>
                ) : (
                  <Link to="/login" className="block py-2 text-base text-fg-soft">
                    Sign in
                  </Link>
                )}
                <BtnLink
                  to={ctaHref}
                  variant="primary"
                  size="md"
                  className="mt-2 w-full"
                  onClick={() => setMobileOpen(false)}
                >
                  {ctaLabel}
                </BtnLink>
              </li>
            </ul>
          </nav>
        ) : null}
      </div>
    </header>
  );
}
