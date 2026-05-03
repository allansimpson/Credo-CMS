import { useState } from "react";
import { Link, NavLink } from "react-router-dom";
import { Menu, X } from "lucide-react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { useAuth } from "@/hooks/useAuth";
import { cn } from "@/lib/utils";

const NAV_ITEMS: { to: string; label: string }[] = [
  { to: "/", label: "Home" },
  { to: "/about", label: "About" },
  { to: "/services", label: "Services" },
];

export function PublicNavBar() {
  const { settings } = useSiteSettings();
  const { isAuthenticated, user } = useAuth();
  const [open, setOpen] = useState(false);

  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4">
        <Link to="/" className="flex items-center gap-3">
          {settings?.logoUrl ? (
            <img
              src={settings.logoUrl}
              alt=""
              className="h-9 w-9 rounded-md object-cover"
            />
          ) : (
            <span aria-hidden className="grid h-9 w-9 place-items-center rounded-md bg-primary text-primary-foreground font-bold">
              {(settings?.churchName ?? "CC").slice(0, 2).toUpperCase()}
            </span>
          )}
          <span className="text-lg font-semibold">{settings?.churchName ?? "Credo CMS"}</span>
        </Link>

        <nav className="hidden md:flex md:items-center md:gap-6 text-sm">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                cn("hover:text-primary", isActive && "text-primary font-medium")
              }
            >
              {item.label}
            </NavLink>
          ))}
          {isAuthenticated ? (
            <Link
              to="/profile"
              className="rounded-md bg-primary px-3 py-2 text-primary-foreground hover:bg-primary/90"
            >
              {user?.firstName ?? "Profile"}
            </Link>
          ) : (
            <Link
              to="/login"
              className="rounded-md bg-primary px-3 py-2 text-primary-foreground hover:bg-primary/90"
            >
              Member login
            </Link>
          )}
        </nav>

        <button
          type="button"
          className="md:hidden inline-flex h-10 w-10 items-center justify-center rounded-md border bg-background"
          aria-label={open ? "Close menu" : "Open menu"}
          onClick={() => setOpen((v) => !v)}
        >
          {open ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </button>
      </div>

      {open && (
        <nav className="md:hidden border-t bg-background">
          <ul className="flex flex-col p-4">
            {NAV_ITEMS.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  className={({ isActive }) =>
                    cn("block py-3 text-sm", isActive && "font-medium text-primary")
                  }
                  onClick={() => setOpen(false)}
                >
                  {item.label}
                </NavLink>
              </li>
            ))}
            <li>
              <Link
                to={isAuthenticated ? "/profile" : "/login"}
                className="block py-3 text-sm font-medium text-primary"
                onClick={() => setOpen(false)}
              >
                {isAuthenticated ? "Profile" : "Member login"}
              </Link>
            </li>
          </ul>
        </nav>
      )}
    </header>
  );
}
