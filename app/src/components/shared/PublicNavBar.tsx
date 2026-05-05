import { useState } from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import { Menu, X, Search } from "lucide-react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { useAuth } from "@/hooks/useAuth";
import { cn } from "@/lib/utils";

const NAV_ITEMS: { to: string; label: string }[] = [
  { to: "/", label: "Home" },
  { to: "/about", label: "About" },
  { to: "/service-times", label: "Service Times" },
  { to: "/news", label: "News" },
];

export function PublicNavBar() {
  const { settings } = useSiteSettings();
  const { isAuthenticated, user } = useAuth();
  const [open, setOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [q, setQ] = useState("");
  const navigate = useNavigate();

  const submitSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = q.trim();
    if (!trimmed) return;
    setSearchOpen(false);
    setOpen(false);
    setQ("");
    navigate(`/search?q=${encodeURIComponent(trimmed)}`);
  };

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
          <button
            type="button"
            aria-label="Search"
            onClick={() => setSearchOpen((v) => !v)}
            className="inline-flex h-9 w-9 items-center justify-center rounded-md border bg-background hover:bg-muted"
          >
            <Search className="h-4 w-4" />
          </button>
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

      {searchOpen && (
        <div className="border-t bg-background">
          <form onSubmit={submitSearch} className="mx-auto flex max-w-3xl items-center gap-2 px-4 py-3">
            <input
              type="search"
              autoFocus
              value={q}
              onChange={(e) => setQ(e.target.value)}
              placeholder="Search the site…"
              className="h-10 flex-1 rounded-md border bg-background px-3 text-sm"
            />
            <button type="submit"
              className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
              Search
            </button>
          </form>
        </div>
      )}

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
