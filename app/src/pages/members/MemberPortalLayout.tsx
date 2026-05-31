import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import {
  Home,
  Users,
  HandHeart,
  UsersRound,
  GraduationCap,
  User,
  MoreHorizontal,
  ExternalLink,
  LogOut,
  ChevronRight,
} from "lucide-react";
import { useAuth } from "@/hooks/useAuth";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { useChurchColorTokens } from "@/themes/useChurchColorTokens";
import { Avatar } from "@/components/members/portal-primitives";

interface NavEntry {
  to: string;
  label: string;
  icon: typeof Home;
  /** Matcher for "active" — if URL starts with this path, treat as active. */
  match: string;
}

const NAV: NavEntry[] = [
  { to: "/members",            label: "Home",            icon: Home,           match: "/members" },
  { to: "/members/directory",  label: "Directory",       icon: Users,          match: "/members/directory" },
  { to: "/members/prayer",     label: "Prayer Requests", icon: HandHeart,      match: "/members/prayer" },
  { to: "/members/groups",     label: "Groups",          icon: UsersRound,     match: "/members/groups" },
  { to: "/members/classes",    label: "Classes",         icon: GraduationCap,  match: "/members/classes" },
  { to: "/members/profile",    label: "My Profile",      icon: User,           match: "/members/profile" },
];

const MOBILE_TABS = NAV.filter((n) =>
  ["Home", "Directory", "Prayer Requests", "Classes"].includes(n.label),
).map((n) => ({ ...n, label: n.label === "Prayer Requests" ? "Prayer" : n.label }));

const MORE_ITEMS = NAV.filter((n) => ["Groups", "My Profile"].includes(n.label));

/**
 * Persistent shell for /members/*. Sidebar + top bar on desktop, top bar +
 * bottom tabs on mobile. Renders <Outlet/> in the content panel so nested
 * routes swap WITHOUT remounting the shell. Resets content scroll to 0 on
 * each navigation.
 *
 * NOTE: no notification bell anywhere — descoped to v1.1 per resolution.
 */
export function MemberPortalLayout() {
  const { user, logout } = useAuth();
  const { settings } = useSiteSettings();
  useChurchColorTokens();
  const navigate = useNavigate();
  const location = useLocation();
  const mainRef = useRef<HTMLElement>(null);
  const [identitySheetOpen, setIdentitySheetOpen] = useState(false);
  const [moreSheetOpen, setMoreSheetOpen] = useState(false);

  // Active section: longest matching prefix wins. Used for both the
  // desktop sidebar (left-bar highlight) and the mobile More-tab active
  // detection (More is active when on Groups or Profile).
  const activeNav =
    [...NAV]
      .filter((n) => location.pathname.startsWith(n.match))
      .sort((a, b) => b.match.length - a.match.length)[0] ?? NAV[0];
  const moreActive = ["Groups", "My Profile"].includes(activeNav.label);

  // Reset content scroll on every navigation so the new section starts at
  // the top, even when the shell itself doesn't unmount.
  useEffect(() => {
    mainRef.current?.scrollTo({ top: 0, left: 0, behavior: "instant" as ScrollBehavior });
  }, [location.pathname]);

  // Close any open sheet on navigation.
  useEffect(() => {
    setIdentitySheetOpen(false);
    setMoreSheetOpen(false);
  }, [location.pathname]);

  const displayName = user
    ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email
    : "Member";
  const topRole = user?.roles?.[0] ?? "Member";
  const churchName = settings?.churchName ?? "Credo CMS";
  const churchInitials = churchName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? "")
    .join("") || "C";

  const handleSignOut = async () => {
    setIdentitySheetOpen(false);
    await logout();
    navigate("/login");
  };

  return (
    // The portal lives outside PublicLayout/ChurchThemeLayout, so it
    // needs its own theme root to resolve the CSS custom properties
    // the editorial design system depends on (--sidebar, --inset,
    // --accent, etc.). One consistent treatment — not per-template —
    // per the handoff, so we pin data-template="editorial".
    <div
      data-theme="church"
      data-template="editorial"
      className="flex h-screen overflow-hidden bg-background text-foreground"
    >
      {/* ── Desktop sidebar ── */}
      <aside className="hidden h-full w-[248px] shrink-0 flex-col bg-sidebar text-sidebar-foreground lg:flex">
        {/* Identity */}
        <div className="relative flex items-center gap-3 border-b border-white/[0.06] px-4 py-4">
          <Avatar
            name={displayName}
            size={36}
            src={user?.photoBlobUrl ?? null}
            webpSrc={user?.photoWebpBlobUrl ?? null}
            alt={user?.photoAltText ?? displayName}
            className="bg-white/10 text-sidebar-foreground"
          />
          <div className="min-w-0 flex-1">
            <div className="truncate font-heading text-[13.5px] font-semibold">
              {displayName}
            </div>
            <div className="mt-0.5 text-[9.5px] uppercase tracking-[0.14em] text-white/50">
              {topRole}
            </div>
          </div>
          <button
            type="button"
            aria-label="Identity menu"
            aria-haspopup="menu"
            aria-expanded={identitySheetOpen}
            onClick={() => setIdentitySheetOpen((v) => !v)}
            className="rounded-none p-1 text-white/60 hover:text-white"
          >
            <MoreHorizontal strokeWidth={1.75} className="h-4 w-4" />
          </button>
          {identitySheetOpen && (
            <div className="absolute right-3 top-full z-30 mt-1 w-[150px] border border-border bg-panel text-foreground shadow-lg">
              <button
                type="button"
                onClick={() => {
                  setIdentitySheetOpen(false);
                  navigate("/members/profile");
                }}
                className="flex w-full items-center gap-2.5 border-b border-border-soft px-3.5 py-2.5 text-left text-sm hover:bg-panel-alt"
              >
                <User strokeWidth={1.5} className="h-[15px] w-[15px] text-muted" />
                My Profile
              </button>
              <button
                type="button"
                onClick={handleSignOut}
                className="flex w-full items-center gap-2.5 px-3.5 py-2.5 text-left text-sm text-danger hover:bg-panel-alt"
              >
                <LogOut strokeWidth={1.5} className="h-[15px] w-[15px]" />
                Sign out
              </button>
            </div>
          )}
        </div>

        {/* Nav */}
        <nav className="flex-1 overflow-y-auto px-2.5 py-3">
          <div className="px-3 pb-2 pt-1.5 text-[9.5px] font-semibold uppercase tracking-[0.16em] text-white/40">
            Member
          </div>
          {NAV.map((n) => {
            const isActive = activeNav.to === n.to;
            const Icon = n.icon;
            return (
              <NavLink
                key={n.to}
                to={n.to}
                end={n.to === "/members"}
                className={`relative flex items-center gap-3 px-3.5 py-2.5 text-[12.5px] transition-colors ${
                  isActive
                    ? "bg-[hsl(22_75%_41%/0.14)] font-semibold text-[hsl(43_29%_95%)]"
                    : "text-white/70 hover:text-white"
                }`}
              >
                {isActive && (
                  <span
                    aria-hidden="true"
                    className="absolute bottom-1 left-0 top-1 w-[2px] bg-accent"
                  />
                )}
                <Icon
                  strokeWidth={1.6}
                  className={`h-4 w-4 ${isActive ? "text-accent" : "text-white/55"}`}
                />
                <span>{n.label}</span>
              </NavLink>
            );
          })}
        </nav>

        {/* Footer: return to public site */}
        <div className="border-t border-white/[0.06] p-2.5">
          <a
            href="/"
            className="flex items-center gap-3 px-3.5 py-2.5 text-[11.5px] text-white/50 transition-colors hover:text-white/80"
          >
            <ExternalLink strokeWidth={1.5} className="h-3.5 w-3.5 text-white/40" />
            <span>Return to {churchName}</span>
          </a>
        </div>
      </aside>

      {/* ── Right side: top bar + content ── */}
      <div className="flex h-full min-w-0 flex-1 flex-col">
        {/* Desktop top bar */}
        <header className="hidden h-14 shrink-0 items-center gap-4 border-b border-border bg-panel px-6 lg:flex">
          <div className="flex flex-1 items-baseline gap-2.5">
            <span className="text-[9.5px] font-semibold uppercase tracking-[0.16em] text-muted">
              Members
            </span>
            <span className="text-border">/</span>
            <span className="font-heading text-base font-semibold tracking-tight">
              {activeNav.label}
            </span>
          </div>
        </header>

        {/* Mobile top bar */}
        <header className="relative flex h-13 shrink-0 items-center gap-2.5 border-b border-border bg-panel px-3.5 lg:hidden">
          <div className="flex h-6 w-6 items-center justify-center bg-accent font-heading text-[11px] font-bold text-accent-foreground">
            {churchInitials}
          </div>
          <span className="flex-1 truncate font-heading text-[14.5px] font-semibold tracking-tight">
            {churchName}
          </span>
          <button
            type="button"
            aria-label="Identity menu"
            onClick={() => setIdentitySheetOpen(true)}
            className="rounded-none p-0.5"
          >
            <Avatar
              name={displayName}
              size={28}
              src={user?.photoBlobUrl ?? null}
              webpSrc={user?.photoWebpBlobUrl ?? null}
              alt={user?.photoAltText ?? displayName}
              className="bg-accent text-accent-foreground"
            />
          </button>
        </header>

        {/* Content */}
        <main
          ref={mainRef}
          className="flex-1 overflow-y-auto bg-background"
        >
          <Outlet />
        </main>

        {/* Mobile bottom tabs */}
        <nav className="grid h-[58px] shrink-0 grid-cols-5 border-t border-border bg-panel lg:hidden">
          {MOBILE_TABS.map((t) => {
            const isActive = activeNav.to === t.to;
            const Icon = t.icon;
            return (
              <NavLink
                key={t.to}
                to={t.to}
                end={t.to === "/members"}
                className={`flex flex-col items-center justify-center gap-1 ${
                  isActive ? "text-accent" : "text-muted"
                }`}
              >
                <Icon strokeWidth={1.6} className="h-5 w-5" />
                <span
                  className={`text-[10px] tracking-[0.01em] ${
                    isActive ? "font-semibold" : "font-medium"
                  }`}
                >
                  {t.label}
                </span>
              </NavLink>
            );
          })}
          <button
            type="button"
            onClick={() => setMoreSheetOpen(true)}
            className={`flex flex-col items-center justify-center gap-1 ${
              moreActive ? "text-accent" : "text-muted"
            }`}
          >
            <MoreHorizontal strokeWidth={1.6} className="h-5 w-5" />
            <span
              className={`text-[10px] tracking-[0.01em] ${
                moreActive ? "font-semibold" : "font-medium"
              }`}
            >
              More
            </span>
          </button>
        </nav>

        {/* Mobile sheets */}
        {(identitySheetOpen || moreSheetOpen) && (
          <button
            type="button"
            aria-label="Close menu"
            onClick={() => {
              setIdentitySheetOpen(false);
              setMoreSheetOpen(false);
            }}
            className="absolute inset-0 z-40 bg-black/40 lg:hidden"
          />
        )}
        {identitySheetOpen && (
          <div className="absolute inset-x-0 bottom-0 z-50 border-t border-border bg-panel lg:hidden">
            <div className="flex items-center gap-3 border-b border-border-soft px-4 py-3.5">
              <Avatar
                name={displayName}
                size={40}
                src={user?.photoBlobUrl ?? null}
                webpSrc={user?.photoWebpBlobUrl ?? null}
                alt={user?.photoAltText ?? displayName}
                className="bg-accent text-accent-foreground"
              />
              <div>
                <div className="font-heading text-[15px] font-semibold">{displayName}</div>
                <div className="mt-0.5 text-[11px] uppercase tracking-[0.12em] text-muted">
                  {topRole}
                </div>
              </div>
            </div>
            <button
              type="button"
              onClick={() => {
                setIdentitySheetOpen(false);
                navigate("/members/profile");
              }}
              className="flex w-full items-center gap-3 border-b border-border-soft px-4 py-3.5 text-left text-sm"
            >
              <User strokeWidth={1.5} className="h-[15px] w-[15px] text-muted" />
              My Profile
            </button>
            <button
              type="button"
              onClick={handleSignOut}
              className="flex w-full items-center gap-3 px-4 py-3.5 text-left text-sm text-danger"
            >
              <LogOut strokeWidth={1.5} className="h-[15px] w-[15px]" />
              Sign out
            </button>
          </div>
        )}
        {moreSheetOpen && (
          <div className="absolute inset-x-0 bottom-0 z-50 border-t border-border bg-panel lg:hidden">
            <div className="border-b border-border-soft px-4 py-3 text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
              More
            </div>
            {MORE_ITEMS.map((m) => {
              const Icon = m.icon;
              return (
                <button
                  key={m.to}
                  type="button"
                  onClick={() => {
                    setMoreSheetOpen(false);
                    navigate(m.to);
                  }}
                  className="flex w-full items-center gap-3 border-b border-border-soft px-4 py-3.5 text-left text-[13.5px]"
                >
                  <Icon strokeWidth={1.5} className="h-[17px] w-[17px] text-muted" />
                  <span className="flex-1">{m.label}</span>
                  <ChevronRight strokeWidth={1.5} className="h-[15px] w-[15px] text-border" />
                </button>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
