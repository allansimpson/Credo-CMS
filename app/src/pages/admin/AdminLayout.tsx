import { useEffect, useRef, useState } from "react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { searchApi, type SearchResultItem } from "@/lib/api/search";
import {
  LayoutDashboard,
  Users,
  ScrollText,
  Settings,
  Menu,
  X,
  LogOut,
  ExternalLink,
  HelpCircle,
  FileText,
  Newspaper,
  Clock,
  UserCircle2,
  File,
  Megaphone,
  Video,
  Calendar,
  Users as UsersIcon,
  GraduationCap,
  HandHelping,
  Mail as MailIcon,
  IdCard,
  Pencil,
  Search,
  ChevronRight,
} from "lucide-react";
import { SystemThemeLayout } from "@/themes/SystemThemeLayout";
import { useAuth } from "@/hooks/useAuth";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { cn } from "@/lib/utils";
import { ToastProvider } from "@/components/shared/admin/Toast";
import type { Role } from "@/types/api";

interface NavItem {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  /** Roles that can see this nav item. Empty = any admin-shell role. */
  requiredRoles?: Role[];
  end?: boolean;
}

interface NavSection {
  heading: string;
  items: NavItem[];
}

const NAV_SECTIONS: NavSection[] = [
  {
    heading: "Overview",
    items: [
      { to: "/admin", label: "Dashboard", icon: LayoutDashboard, end: true },
    ],
  },
  {
    heading: "Content",
    items: [
      { to: "/admin/pages", label: "Pages", icon: FileText },
      { to: "/admin/news", label: "News", icon: Newspaper },
      { to: "/admin/blog", label: "Blog", icon: Pencil },
      { to: "/admin/sermons", label: "Sermons", icon: Video },
      { to: "/admin/sermon-series", label: "Sermon Series", icon: Video },
      { to: "/admin/events", label: "Events", icon: Calendar },
      { to: "/admin/leaders", label: "Leaders", icon: UserCircle2 },
      { to: "/admin/documents", label: "Documents", icon: File },
      { to: "/admin/announcement", label: "Announcement", icon: Megaphone },
      { to: "/admin/service-times", label: "Service Times", icon: Clock },
    ],
  },
  {
    heading: "Engagement",
    items: [
      { to: "/admin/groups", label: "Groups", icon: UsersIcon },
      { to: "/admin/class-slots", label: "Classes", icon: GraduationCap, requiredRoles: ["Administrator"] },
      { to: "/admin/prayer-requests", label: "Prayer", icon: HandHelping },
      { to: "/admin/connect-cards", label: "Connect Cards", icon: IdCard },
      { to: "/admin/broadcasts", label: "Broadcasts", icon: MailIcon },
    ],
  },
  {
    heading: "Administration",
    items: [
      { to: "/admin/users", label: "Users", icon: Users, requiredRoles: ["Administrator"] },
      { to: "/admin/audit-log", label: "Audit Log", icon: ScrollText, requiredRoles: ["Administrator"] },
      { to: "/admin/settings", label: "Settings", icon: Settings, requiredRoles: ["Administrator"] },
    ],
  },
];

export function AdminLayout() {
  const { user, logout, hasAnyRole } = useAuth();
  const { settings } = useSiteSettings();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  const visibleSections = NAV_SECTIONS
    .map((section) => ({
      heading: section.heading,
      items: section.items.filter(
        (item) => !item.requiredRoles || hasAnyRole(item.requiredRoles),
      ),
    }))
    .filter((section) => section.items.length > 0);

  // Pick the highest-privilege role for the user badge.
  const primaryRole: Role | null = user?.roles.includes("Administrator")
    ? "Administrator"
    : user?.roles.includes("Editor")
      ? "Editor"
      : user?.roles[0] ?? null;

  // Tab title: "{Role} · {Church Name}" — replaces the static "spa" fallback
  // from index.html. Role values from the API ("Administrator" / "Editor" /
  // "Member") are already title-cased, so no transform is needed.
  useEffect(() => {
    const churchName = settings?.churchName ?? "Credo CMS";
    const rolePart = primaryRole ?? "Admin";
    document.title = `${rolePart} · ${churchName}`;
  }, [primaryRole, settings?.churchName]);

  return (
    <SystemThemeLayout>
    <ToastProvider>
      <a
        href="#admin-main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50 focus:bg-primary focus:text-primary-foreground focus:px-3 focus:py-2"
      >
        Skip to main content
      </a>
      <div className="flex h-screen overflow-hidden bg-background">
        {/* Sidebar (desktop) */}
        <aside className="hidden w-60 shrink-0 bg-sidebar text-[hsl(var(--card))] lg:flex lg:flex-col">
          <SidebarBrand
            churchName={settings?.churchName ?? "Credo CMS"}
            logoUrl={settings?.logoUrl ?? null}
          />
          <div className="flex-1 overflow-y-auto py-2">
            <SidebarNav sections={visibleSections} />
          </div>
          <SidebarUserBlock
            displayName={user?.displayName ?? ""}
            firstName={user?.firstName ?? "?"}
            lastName={user?.lastName ?? ""}
            role={primaryRole}
            open={userMenuOpen}
            onToggle={() => setUserMenuOpen((v) => !v)}
            onClose={() => setUserMenuOpen(false)}
            onLogout={logout}
          />
        </aside>

        {/* Sidebar drawer (mobile/tablet) */}
        {drawerOpen && (
          <div className="fixed inset-0 z-30 lg:hidden" role="dialog" aria-modal="true">
            <div className="absolute inset-0 bg-foreground/40" onClick={() => setDrawerOpen(false)} />
            <div className="absolute left-0 top-0 flex h-full w-64 flex-col bg-sidebar text-[hsl(var(--card))] shadow-xl">
              <div className="flex h-14 items-center justify-between border-b border-white/10 px-4">
                <span className="font-semibold">Navigation</span>
                <button type="button" onClick={() => setDrawerOpen(false)} aria-label="Close navigation" className="rounded-md border border-white/20 p-1.5">
                  <X className="h-4 w-4" />
                </button>
              </div>
              <div className="flex-1 overflow-y-auto py-2">
                <SidebarNav sections={visibleSections} onNavigate={() => setDrawerOpen(false)} />
              </div>
              <SidebarUserBlock
                displayName={user?.displayName ?? ""}
                firstName={user?.firstName ?? "?"}
                lastName={user?.lastName ?? ""}
                role={primaryRole}
                open={userMenuOpen}
                onToggle={() => setUserMenuOpen((v) => !v)}
                onClose={() => setUserMenuOpen(false)}
                onLogout={logout}
              />
            </div>
          </div>
        )}

        {/* Right column: top bar + main */}
        <div className="flex min-w-0 flex-1 flex-col">
          <AdminTopBar
            churchName={settings?.churchName ?? "Credo CMS"}
            onOpenDrawer={() => setDrawerOpen(true)}
          />

          <main id="admin-main-content" className="flex-1 overflow-y-auto p-4 lg:p-8" tabIndex={-1}>
            <Outlet />
          </main>
        </div>
      </div>
    </ToastProvider>
    </SystemThemeLayout>
  );
}

function SidebarBrand({ churchName, logoUrl }: { churchName: string; logoUrl: string | null }) {
  return (
    <Link to="/admin" className="flex items-center gap-3 px-5 py-5 border-b border-white/10">
      {logoUrl ? (
        <img src={logoUrl} alt="" className="h-10 w-10 rounded object-cover" />
      ) : (
        <span
          aria-hidden
          className="grid h-10 w-10 place-items-center bg-accent text-[hsl(var(--accent-foreground))] text-sm font-bold"
        >
          {churchName.slice(0, 2).toUpperCase()}
        </span>
      )}
      <div className="leading-tight min-w-0">
        <div className="truncate text-sm font-semibold text-white">{churchName}</div>
        <div className="text-[10px] uppercase tracking-[0.18em] text-white/50">Credo · Workbench</div>
      </div>
    </Link>
  );
}

function SidebarNav({
  sections,
  onNavigate,
}: {
  sections: NavSection[];
  onNavigate?: () => void;
}) {
  return (
    <nav className="px-3">
      {sections.map((section, idx) => (
        <div key={section.heading} className={cn(idx > 0 && "mt-6")}>
          <div className="px-3 pb-2 text-[10px] font-semibold uppercase tracking-[0.18em] text-white/40">
            {section.heading}
          </div>
          <ul className="space-y-0.5">
            {section.items.map((item) => {
              const Icon = item.icon;
              return (
                <li key={item.to}>
                  <NavLink
                    to={item.to}
                    end={item.end}
                    onClick={onNavigate}
                    className={({ isActive }) =>
                      cn(
                        "relative flex items-center gap-2.5 px-3 py-2 text-sm transition-colors",
                        isActive
                          ? "bg-white/[0.06] text-white before:absolute before:left-0 before:top-1.5 before:bottom-1.5 before:w-[3px] before:bg-accent"
                          : "text-white/70 hover:bg-white/[0.04] hover:text-white",
                      )
                    }
                  >
                    <Icon className="h-4 w-4 shrink-0" />
                    {item.label}
                  </NavLink>
                </li>
              );
            })}
          </ul>
        </div>
      ))}
    </nav>
  );
}

function SidebarUserBlock({
  displayName,
  firstName,
  lastName,
  role,
  open,
  onToggle,
  onClose,
  onLogout,
}: {
  displayName: string;
  firstName: string;
  lastName: string;
  role: Role | null;
  open: boolean;
  onToggle: () => void;
  onClose: () => void;
  onLogout: () => void;
}) {
  const initials = ((firstName.slice(0, 1) + lastName.slice(0, 1)) || "?").toUpperCase();
  return (
    <div className="relative border-t border-white/10 px-3 py-3">
      <button
        type="button"
        onClick={onToggle}
        aria-haspopup="menu"
        aria-expanded={open ? "true" : "false"}
        className="flex w-full items-center gap-3 px-2 py-1.5 text-left hover:bg-white/[0.04]"
      >
        <span
          aria-hidden
          className="grid h-9 w-9 place-items-center bg-white/10 text-xs font-bold text-white"
        >
          {initials}
        </span>
        <span className="min-w-0 flex-1 leading-tight">
          <span className="block truncate text-sm font-semibold text-white">{displayName}</span>
          {role && (
            <span className="block text-[10px] uppercase tracking-[0.16em] text-white/50">
              {role}
            </span>
          )}
        </span>
      </button>
      {open && (
        <div
          role="menu"
          className="absolute bottom-full left-3 right-3 z-30 mb-2 border bg-popover p-1 text-foreground shadow-lg"
          onMouseLeave={onClose}
        >
          <Link
            to="/profile"
            className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-panel-alt"
            onClick={onClose}
            role="menuitem"
          >
            Profile
          </Link>
          <Link
            to="/"
            className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-panel-alt"
            onClick={onClose}
            role="menuitem"
          >
            <ExternalLink className="h-4 w-4" />
            View public site
          </Link>
          <Link
            to="/docs"
            className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-panel-alt"
            onClick={onClose}
            role="menuitem"
          >
            <HelpCircle className="h-4 w-4" />
            Help
          </Link>
          <button
            type="button"
            onClick={() => { onClose(); onLogout(); }}
            className="flex w-full items-center gap-2 px-3 py-2 text-sm text-danger hover:bg-panel-alt"
            role="menuitem"
          >
            <LogOut className="h-4 w-4" />
            Log out
          </button>
        </div>
      )}
    </div>
  );
}

/* ── Top bar ─────────────────────────────────────────────────────────────
 * Sits to the right of the sidebar. Left: breadcrumb derived from the
 * current path. Middle: a global search input that hits the admin search
 * endpoint (drafts included). Right: a "Live site" external link. */

const ENTITY_TYPE_LABELS: Record<string, string> = {
  Page: "Page",
  NewsItem: "News",
  Sermon: "Sermon",
  SermonSeries: "Series",
  Event: "Event",
  Leader: "Leader",
  Document: "Document",
};

/** Flat searchable index of admin sections. Lets the palette double as a
 * nav shortcut: typing "Ser" jumps to Sermons even if no content matches. */
interface SectionMatch {
  label: string;
  to: string;
  icon: React.ComponentType<{ className?: string }>;
  /** Section heading the item belongs to (Overview/Content/Engagement/Administration). */
  group: string;
  /** Extra match strings (singular/plural forms, synonyms). */
  aliases: string[];
}

function buildSectionIndex(): SectionMatch[] {
  const out: SectionMatch[] = [];
  for (const section of NAV_SECTIONS) {
    for (const item of section.items) {
      out.push({
        label: item.label,
        to: item.to,
        icon: item.icon,
        group: section.heading,
        aliases: [],
      });
    }
  }
  // Hand-tuned aliases: things admins type that don't match the literal label.
  const aliasMap: Record<string, string[]> = {
    "/admin/sermons": ["sermon"],
    "/admin/sermon-series": ["series"],
    "/admin/news": ["article", "articles"],
    "/admin/events": ["event"],
    "/admin/pages": ["page"],
    "/admin/leaders": ["leader", "elder", "elders", "staff", "pastors"],
    "/admin/documents": ["document", "files", "downloads"],
    "/admin/prayer-requests": ["prayer-request", "prayer requests", "prayers"],
    "/admin/connect-cards": ["connect card"],
    "/admin/blog": ["blog post", "posts"],
    "/admin/users": ["user", "accounts", "members"],
    "/admin/audit-log": ["audit", "logs", "activity"],
    "/admin/settings": ["site settings", "config", "configuration"],
    "/admin/service-times": ["service", "schedule"],
    "/admin/class-slots": ["class", "course", "courses"],
    "/admin/groups": ["small group", "small groups", "group"],
    "/admin/broadcasts": ["broadcast", "newsletter", "email"],
    "/admin/announcement": ["announce", "banner"],
  };
  for (const s of out) {
    if (aliasMap[s.to]) s.aliases = aliasMap[s.to];
  }
  return out;
}

function adminUrlFor(item: SearchResultItem): string {
  switch (item.entityType) {
    case "Page": return `/admin/pages/${item.entityId}`;
    case "NewsItem": return `/admin/news/${item.entityId}`;
    case "Sermon": return `/admin/sermons/${item.entityId}`;
    case "SermonSeries": return `/admin/sermon-series/${item.entityId}`;
    case "Event": return `/admin/events/${item.entityId}`;
    // Leaders and Documents are managed inline on their list pages —
    // no dedicated editor route exists, so the best we can do is land
    // the admin on the right list.
    case "Leader": return `/admin/leaders`;
    case "Document": return `/admin/documents`;
    default: return "/admin";
  }
}

const SEGMENT_LABELS: Record<string, string> = {
  admin: "Dashboard",
  pages: "Pages",
  news: "News",
  blog: "Blog",
  sermons: "Sermons",
  "sermon-series": "Sermon Series",
  events: "Events",
  leaders: "Leaders",
  documents: "Documents",
  announcement: "Announcement",
  "service-times": "Service Times",
  groups: "Groups",
  "class-slots": "Classes",
  "prayer-requests": "Prayer",
  "connect-cards": "Connect Cards",
  broadcasts: "Broadcasts",
  users: "Users",
  "audit-log": "Audit Log",
  settings: "Settings",
  calendar: "Calendar",
  registrations: "Registrations",
  new: "New",
};

function labelForSegment(segment: string, prevSegment: string | undefined): string {
  if (SEGMENT_LABELS[segment]) return SEGMENT_LABELS[segment];
  // Anything that looks like an ID (uuid, numeric, or :id placeholder) falls
  // back to a generic verb based on its parent collection.
  if (segment === "new" || /^[0-9a-f-]{8,}$/i.test(segment) || /^\d+$/.test(segment)) {
    return prevSegment && SEGMENT_LABELS[prevSegment] ? `Edit ${SEGMENT_LABELS[prevSegment].replace(/s$/, "")}` : "Edit";
  }
  return segment.charAt(0).toUpperCase() + segment.slice(1);
}

const SECTION_INDEX = buildSectionIndex();

function AdminTopBar({
  churchName,
  onOpenDrawer,
}: {
  churchName: string;
  onOpenDrawer: () => void;
}) {
  const location = useLocation();
  const navigate = useNavigate();
  const { hasAnyRole } = useAuth();
  const searchRef = useRef<HTMLInputElement>(null);
  const paletteRef = useRef<HTMLDivElement>(null);
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<SearchResultItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [highlighted, setHighlighted] = useState(0);

  // Sections that match the query (case-insensitive on label + aliases).
  // Filtered against visibility — admins see Audit Log, editors don't, etc.
  const sectionMatches: SectionMatch[] = (() => {
    const q = query.trim().toLowerCase();
    if (q.length < 2) return [];
    // Mirror the role gate the sidebar uses so the palette never offers a
    // section the current user can't access.
    const sectionVisibility: Record<string, ("Administrator" | "Editor")[]> = {
      "/admin/class-slots": ["Administrator"],
      "/admin/users": ["Administrator"],
      "/admin/audit-log": ["Administrator"],
      "/admin/settings": ["Administrator"],
    };
    return SECTION_INDEX.filter((s) => {
      const required = sectionVisibility[s.to];
      if (required && !hasAnyRole(required)) return false;
      if (s.label.toLowerCase().includes(q)) return true;
      return s.aliases.some((a) => a.toLowerCase().includes(q));
    }).slice(0, 5);
  })();

  // The keyboard selection runs across [sections, ...content results].
  const flatItemCount = sectionMatches.length + results.length;

  // ⌘K / Ctrl+K focuses the search input
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        searchRef.current?.focus();
      }
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, []);

  // Close palette + reset state when route changes (user navigated away).
  useEffect(() => {
    setPaletteOpen(false);
    setQuery("");
    setResults([]);
  }, [location.pathname]);

  // Click-outside closes palette.
  useEffect(() => {
    if (!paletteOpen) return;
    const handler = (e: MouseEvent) => {
      if (paletteRef.current && !paletteRef.current.contains(e.target as Node)) {
        setPaletteOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [paletteOpen]);

  // Debounced fetch: 200ms after the user stops typing.
  useEffect(() => {
    const q = query.trim();
    if (q.length < 2) {
      setResults([]);
      setLoading(false);
      setError(null);
      return;
    }
    setLoading(true);
    setError(null);
    const timer = window.setTimeout(() => {
      let cancelled = false;
      searchApi.searchAdmin(q, 1, 10)
        .then((res) => { if (!cancelled) { setResults(res.items); setHighlighted(0); } })
        .catch((err) => {
          if (cancelled) return;
          setResults([]);
          setError(err instanceof Error ? err.message : "Search failed.");
        })
        .finally(() => { if (!cancelled) setLoading(false); });
      return () => { cancelled = true; };
    }, 200);
    return () => window.clearTimeout(timer);
  }, [query]);

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Escape") {
      setPaletteOpen(false);
      searchRef.current?.blur();
      return;
    }
    if (!paletteOpen || flatItemCount === 0) return;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setHighlighted((i) => (i + 1) % flatItemCount);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setHighlighted((i) => (i - 1 + flatItemCount) % flatItemCount);
    } else if (e.key === "Enter") {
      e.preventDefault();
      if (highlighted < sectionMatches.length) {
        navigate(sectionMatches[highlighted].to);
      } else {
        const contentIdx = highlighted - sectionMatches.length;
        const target = results[contentIdx];
        if (target) navigate(adminUrlFor(target));
      }
      setPaletteOpen(false);
    }
  };

  const segments = location.pathname.split("/").filter(Boolean);
  // Build crumbs: skip the leading "admin" segment — it's represented by
  // the church name root crumb — and label each remaining segment.
  const crumbs: { label: string; to: string }[] = [];
  let acc = "";
  for (let i = 0; i < segments.length; i++) {
    const seg = segments[i];
    acc += `/${seg}`;
    if (seg === "admin" && i === 0) {
      // Root crumb is "Dashboard" only when we're exactly at /admin
      if (segments.length === 1) crumbs.push({ label: "Dashboard", to: "/admin" });
      continue;
    }
    crumbs.push({ label: labelForSegment(seg, segments[i - 1]), to: acc });
  }

  return (
    <header className="flex h-14 shrink-0 items-center gap-3 border-b bg-background px-4 lg:px-6">
      <button
        type="button"
        onClick={onOpenDrawer}
        className="lg:hidden inline-flex h-9 w-9 items-center justify-center border"
        aria-label="Open navigation"
      >
        <Menu className="h-5 w-5" />
      </button>

      {/* Breadcrumb */}
      <nav aria-label="Breadcrumb" className="flex min-w-0 items-center gap-1.5 text-sm">
        <Link to="/admin" className="truncate font-medium text-muted hover:text-foreground">
          {churchName}
        </Link>
        {crumbs.map((c, i) => (
          <span key={c.to} className="flex items-center gap-1.5 min-w-0">
            <ChevronRight className="h-3.5 w-3.5 shrink-0 text-muted" />
            {i === crumbs.length - 1 ? (
              <span className="truncate font-semibold text-foreground">{c.label}</span>
            ) : (
              <Link to={c.to} className="truncate text-muted hover:text-foreground">
                {c.label}
              </Link>
            )}
          </span>
        ))}
      </nav>

      <div className="ml-auto flex items-center gap-2">
        <div ref={paletteRef} className="relative hidden sm:block">
          <label>
            <span className="sr-only">Search</span>
            <Search
              aria-hidden
              className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted"
            />
            <input
              ref={searchRef}
              type="search"
              value={query}
              onChange={(e) => { setQuery(e.target.value); setPaletteOpen(true); }}
              onFocus={() => { if (query.trim().length >= 2) setPaletteOpen(true); }}
              onKeyDown={handleKeyDown}
              placeholder="Find anything…"
              autoComplete="off"
              spellCheck={false}
              role="combobox"
              aria-expanded={paletteOpen ? "true" : "false"}
              aria-controls="admin-search-palette"
              aria-autocomplete="list"
              className="h-9 w-64 border bg-background pl-8 pr-14 text-sm placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent md:w-80"
            />
            <kbd
              aria-hidden
              className="pointer-events-none absolute right-2 top-1/2 -translate-y-1/2 border bg-panel-alt px-1.5 py-0.5 font-mono text-[10px] text-muted"
            >
              ⌘K
            </kbd>
          </label>

          {paletteOpen && query.trim().length >= 2 && (
            <div
              id="admin-search-palette"
              className="absolute right-0 top-full z-50 mt-1 w-[28rem] max-w-[80vw] border bg-popover text-foreground shadow-lg"
            >
              {loading && results.length === 0 && sectionMatches.length === 0 && (
                <div className="flex items-center gap-2 px-3 py-3 text-xs text-muted">
                  <span aria-hidden="true" className="flex gap-1">
                    <span className="h-1.5 w-1.5 animate-[pulse_1.2s_ease-in-out_infinite] rounded-full bg-accent" />
                    <span className="h-1.5 w-1.5 animate-[pulse_1.2s_ease-in-out_infinite] rounded-full bg-accent [animation-delay:200ms]" />
                    <span className="h-1.5 w-1.5 animate-[pulse_1.2s_ease-in-out_infinite] rounded-full bg-accent [animation-delay:400ms]" />
                  </span>
                  <span className="font-mono uppercase tracking-[0.14em]">Searching</span>
                </div>
              )}

              {error && (
                <div className="border-b border-border-soft px-3 py-2 text-xs text-danger">
                  {error}
                </div>
              )}

              {!loading && flatItemCount === 0 && !error && (
                <div className="px-3 py-3 text-xs text-muted">
                  No matches for <span className="font-semibold text-foreground">{query.trim()}</span>.
                </div>
              )}

              {flatItemCount > 0 && (
                <div role="listbox" aria-label="Search results" className="max-h-80 overflow-y-auto py-1">
                  {sectionMatches.length > 0 && (
                    <>
                      <div className="px-3 pb-1 pt-2 text-[10px] font-semibold uppercase tracking-[0.18em] text-muted">
                        Jump to
                      </div>
                      {sectionMatches.map((section, idx) => {
                        const Icon = section.icon;
                        const selected = idx === highlighted;
                        return (
                          <a
                            key={section.to}
                            role="option"
                            aria-selected={selected}
                            href={section.to}
                            onMouseEnter={() => setHighlighted(idx)}
                            onClick={(e) => {
                              e.preventDefault();
                              setPaletteOpen(false);
                              navigate(section.to);
                            }}
                            className={cn(
                              "flex items-center gap-3 px-3 py-2 text-sm",
                              idx === highlighted ? "bg-panel-alt" : "hover:bg-panel-alt",
                            )}
                          >
                            <Icon className="h-4 w-4 shrink-0 text-muted" />
                            <span className="flex-1 font-medium">{section.label}</span>
                            <span className="text-[10px] uppercase tracking-[0.14em] text-muted">{section.group}</span>
                          </a>
                        );
                      })}
                    </>
                  )}

                  {results.length > 0 && (
                    <>
                      {sectionMatches.length > 0 && (
                        <div className="mt-1 border-t border-border-soft px-3 pb-1 pt-2 text-[10px] font-semibold uppercase tracking-[0.18em] text-muted">
                          Matches
                        </div>
                      )}
                      {results.map((item, i) => {
                        const idx = sectionMatches.length + i;
                        const url = adminUrlFor(item);
                        const selected = idx === highlighted;
                        return (
                          <a
                            key={`${item.entityType}-${item.entityId}`}
                            role="option"
                            aria-selected={selected}
                            href={url}
                            onMouseEnter={() => setHighlighted(idx)}
                            onClick={(e) => {
                              e.preventDefault();
                              setPaletteOpen(false);
                              navigate(url);
                            }}
                            className={cn(
                              "flex items-start gap-3 px-3 py-2 text-sm",
                              idx === highlighted ? "bg-panel-alt" : "hover:bg-panel-alt",
                            )}
                          >
                            <span className="mt-0.5 inline-flex h-6 w-14 shrink-0 items-center justify-center border border-border-soft bg-background text-[9px] font-semibold uppercase tracking-[0.12em] text-muted">
                              {ENTITY_TYPE_LABELS[item.entityType] ?? item.entityType}
                            </span>
                            <span className="min-w-0 flex-1">
                              <span className="block truncate font-medium">{item.title}</span>
                              {item.snippet && (
                                <span className="block truncate text-xs text-muted">{item.snippet}</span>
                              )}
                            </span>
                          </a>
                        );
                      })}
                    </>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
        <Link
          to="/"
          className="inline-flex h-9 items-center gap-1.5 border px-3 text-xs font-medium hover:bg-panel-alt"
        >
          <ExternalLink className="h-3.5 w-3.5" /> Live site
        </Link>
      </div>
    </header>
  );
}
