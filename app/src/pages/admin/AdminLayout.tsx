import { useState } from "react";
import { Link, NavLink, Outlet } from "react-router-dom";
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
  Mic,
  Calendar,
  Users as UsersIcon,
  GraduationCap,
  HandHelping,
  Mail as MailIcon,
  Pencil,
} from "lucide-react";
import { SystemThemeLayout } from "@/themes/SystemThemeLayout";
import { useAuth } from "@/hooks/useAuth";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { cn } from "@/lib/utils";
import type { Role } from "@/types/api";

interface NavItem {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  /** Roles that can see this nav item. Empty = any admin-shell role. */
  requiredRoles?: Role[];
  end?: boolean;
}

const NAV_ITEMS: NavItem[] = [
  { to: "/admin", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/admin/pages", label: "Pages", icon: FileText },
  { to: "/admin/news", label: "News", icon: Newspaper },
  { to: "/admin/service-times", label: "Service Times", icon: Clock },
  { to: "/admin/leaders", label: "Leaders", icon: UserCircle2 },
  { to: "/admin/documents", label: "Documents", icon: File },
  { to: "/admin/announcement", label: "Announcement", icon: Megaphone },
  { to: "/admin/sermons", label: "Sermons", icon: Mic },
  { to: "/admin/sermon-series", label: "Sermon Series", icon: Mic },
  { to: "/admin/events", label: "Events", icon: Calendar },
  { to: "/admin/groups", label: "Groups", icon: UsersIcon },
  { to: "/admin/class-slots", label: "Classes", icon: GraduationCap, requiredRoles: ["Administrator"] },
  { to: "/admin/prayer-requests", label: "Prayer", icon: HandHelping },
  { to: "/admin/connect-cards", label: "Connect cards", icon: MailIcon },
  { to: "/admin/blog", label: "Blog", icon: Pencil },
  { to: "/admin/users", label: "Users", icon: Users, requiredRoles: ["Administrator"] },
  { to: "/admin/audit-log", label: "Audit Log", icon: ScrollText, requiredRoles: ["Administrator"] },
  { to: "/admin/settings", label: "Site Settings", icon: Settings, requiredRoles: ["Administrator"] },
];

export function AdminLayout() {
  const { user, logout, hasAnyRole } = useAuth();
  const { settings } = useSiteSettings();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);

  const visibleNav = NAV_ITEMS.filter(
    (item) => !item.requiredRoles || hasAnyRole(item.requiredRoles),
  );

  return (
    <SystemThemeLayout>
      <div className="flex min-h-screen flex-col bg-background">
        {/* Top bar */}
        <header className="flex h-14 items-center justify-between border-b bg-background px-4">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setDrawerOpen(true)}
              className="lg:hidden inline-flex h-9 w-9 items-center justify-center rounded-md border"
              aria-label="Open navigation"
            >
              <Menu className="h-5 w-5" />
            </button>

            <Link to="/admin" className="flex items-center gap-2">
              {settings?.logoUrl ? (
                <img src={settings.logoUrl} alt="" className="h-7 w-7 rounded object-cover" />
              ) : (
                <span aria-hidden className="grid h-7 w-7 place-items-center rounded bg-foreground text-background text-xs font-bold">
                  {(settings?.churchName ?? "CC").slice(0, 2).toUpperCase()}
                </span>
              )}
              <div className="leading-tight">
                <div className="text-sm font-semibold">{settings?.churchName ?? "Credo CMS"}</div>
                <div className="text-[11px] uppercase tracking-wide text-muted">Credo CMS — Admin</div>
              </div>
            </Link>
          </div>

          <div className="relative">
            <button
              type="button"
              onClick={() => setProfileOpen((v) => !v)}
              className="flex items-center gap-2 rounded-md border px-3 py-1.5 text-sm hover:bg-panel-alt"
            >
              <span aria-hidden className="grid h-7 w-7 place-items-center rounded-full bg-accent text-accent-foreground text-xs font-bold">
                {(user?.firstName ?? "?").slice(0, 1).toUpperCase()}
              </span>
              <span className="hidden sm:block">{user?.displayName}</span>
            </button>
            {profileOpen && (
              <div
                role="menu"
                className="absolute right-0 z-30 mt-2 w-56 rounded-md border bg-popover p-1 shadow-lg"
                onMouseLeave={() => setProfileOpen(false)}
              >
                <Link
                  to="/profile"
                  className="flex items-center gap-2 rounded-md px-3 py-2 text-sm hover:bg-panel-alt"
                  onClick={() => setProfileOpen(false)}
                >
                  Profile
                </Link>
                <Link
                  to="/"
                  className="flex items-center gap-2 rounded-md px-3 py-2 text-sm hover:bg-panel-alt"
                  onClick={() => setProfileOpen(false)}
                >
                  <ExternalLink className="h-4 w-4" />
                  View public site
                </Link>
                <Link
                  to="/docs"
                  className="flex items-center gap-2 rounded-md px-3 py-2 text-sm hover:bg-panel-alt"
                  onClick={() => setProfileOpen(false)}
                >
                  <HelpCircle className="h-4 w-4" />
                  Help
                </Link>
                <button
                  type="button"
                  onClick={() => { setProfileOpen(false); logout(); }}
                  className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm text-danger hover:bg-panel-alt"
                >
                  <LogOut className="h-4 w-4" />
                  Log out
                </button>
              </div>
            )}
          </div>
        </header>

        <div className="flex flex-1">
          {/* Sidebar (desktop) */}
          <aside className="hidden w-60 shrink-0 border-r bg-card lg:block">
            <SidebarNav items={visibleNav} />
          </aside>

          {/* Sidebar drawer (mobile/tablet) */}
          {drawerOpen && (
            <div className="fixed inset-0 z-30 lg:hidden" role="dialog" aria-modal="true">
              <div className="absolute inset-0 bg-foreground/40" onClick={() => setDrawerOpen(false)} />
              <div className="absolute left-0 top-0 h-full w-64 bg-card shadow-xl">
                <div className="flex h-14 items-center justify-between border-b px-4">
                  <span className="font-semibold">Navigation</span>
                  <button type="button" onClick={() => setDrawerOpen(false)} className="rounded-md border p-1.5">
                    <X className="h-4 w-4" />
                  </button>
                </div>
                <SidebarNav items={visibleNav} onNavigate={() => setDrawerOpen(false)} />
              </div>
            </div>
          )}

          <main className="flex-1 p-4 lg:p-8">
            <Outlet />
          </main>
        </div>
      </div>
    </SystemThemeLayout>
  );
}

function SidebarNav({
  items,
  onNavigate,
}: {
  items: NavItem[];
  onNavigate?: () => void;
}) {
  return (
    <nav className="p-3">
      <ul className="space-y-1">
        {items.map((item) => {
          const Icon = item.icon;
          return (
            <li key={item.to}>
              <NavLink
                to={item.to}
                end={item.end}
                onClick={onNavigate}
                className={({ isActive }) =>
                  cn(
                    "flex items-center gap-2 rounded-md px-3 py-2 text-sm",
                    isActive ? "bg-accent text-accent-foreground" : "hover:bg-panel-alt",
                  )
                }
              >
                <Icon className="h-4 w-4" />
                {item.label}
              </NavLink>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
