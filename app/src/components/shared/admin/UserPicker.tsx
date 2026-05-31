import { useEffect, useRef, useState } from "react";
import { Search, X } from "lucide-react";
import { usersApi } from "@/lib/api/users";
import type { UserListItem } from "@/types/api";

export interface PickedUser {
  id: string;
  displayName: string;
  email: string;
}

interface Props {
  /** Current selection. Null = nothing linked. */
  value: PickedUser | null;
  /** Fired on pick / clear. `null` payload means the user cleared the link. */
  onChange: (next: PickedUser | null) => void;
  /** Optional placeholder inside the search input. */
  placeholder?: string;
  /** Label for the "clear" button. Defaults to "Clear". */
  clearLabel?: string;
  /** Disabled state for the whole control. */
  disabled?: boolean;
}

/**
 * Typeahead picker over the admin user list. Used wherever a record links
 * to an ApplicationUser (Leader → user link, News author override). Shows a
 * selected-user "pill" when a value is set; clicking ✕ clears the link.
 *
 * Search hits `GET /api/admin/users?search=…` with a 250ms debounce so each
 * keystroke doesn't refetch.
 */
export function UserPicker({ value, onChange, placeholder, clearLabel = "Clear", disabled }: Props) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<UserListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // Debounced fetch — skip when too short to be useful or when an item is
  // already selected (typing into the search is the way to switch).
  useEffect(() => {
    if (query.trim().length < 2) {
      setResults([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    const handle = window.setTimeout(async () => {
      try {
        const page = await usersApi.list({ search: query.trim(), pageSize: 8 });
        setResults(page.items);
      } catch {
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, 250);
    return () => window.clearTimeout(handle);
  }, [query]);

  // Close the dropdown on outside-click so it doesn't linger.
  useEffect(() => {
    if (!open) return;
    const onClick = (e: MouseEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, [open]);

  if (value) {
    return (
      <div className="flex items-center gap-2 border border-border bg-panel px-3 py-2 text-sm">
        <div className="min-w-0 flex-1">
          <div className="truncate font-medium text-foreground">{value.displayName || value.email}</div>
          {value.displayName && value.email && (
            <div className="truncate text-xs text-muted">{value.email}</div>
          )}
        </div>
        <button
          type="button"
          onClick={() => onChange(null)}
          disabled={disabled}
          className="inline-flex items-center gap-1 text-xs text-muted hover:text-danger focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-accent disabled:cursor-not-allowed disabled:opacity-50"
        >
          <X strokeWidth={1.75} className="h-3.5 w-3.5" />
          {clearLabel}
        </button>
      </div>
    );
  }

  return (
    <div ref={containerRef} className="relative">
      <div className="relative flex items-center">
        <Search aria-hidden strokeWidth={1.5} className="pointer-events-none absolute left-3 h-4 w-4 text-muted" />
        <input
          type="search"
          value={query}
          onChange={(e) => { setQuery(e.target.value); setOpen(true); }}
          onFocus={() => { if (query.trim().length >= 2) setOpen(true); }}
          placeholder={placeholder ?? "Search by name or email…"}
          disabled={disabled}
          autoComplete="off"
          className="h-10 w-full border border-border bg-panel pl-9 pr-3 text-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-accent disabled:cursor-not-allowed disabled:opacity-50"
        />
      </div>

      {open && query.trim().length >= 2 && (
        <div className="absolute left-0 right-0 top-full z-30 mt-1 max-h-72 overflow-y-auto border border-border bg-popover text-foreground shadow-lg">
          {loading && results.length === 0 && (
            <div className="px-3 py-2 text-xs text-muted">Searching…</div>
          )}
          {!loading && results.length === 0 && (
            <div className="px-3 py-2 text-xs text-muted">No matches.</div>
          )}
          {results.map((u) => (
            <button
              key={u.id}
              type="button"
              onClick={() => {
                onChange({ id: u.id, displayName: u.displayName, email: u.email });
                setQuery("");
                setOpen(false);
              }}
              className="flex w-full flex-col items-start gap-0.5 border-b border-border-soft px-3 py-2 text-left text-sm hover:bg-panel-alt focus-visible:bg-panel-alt focus-visible:outline-none"
            >
              <span className="font-medium text-foreground">{u.displayName || u.email}</span>
              <span className="text-xs text-muted">{u.email}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
