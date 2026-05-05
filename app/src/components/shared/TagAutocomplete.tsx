import { useEffect, useRef, useState } from "react";
import { tagsApi, type TagDto } from "@/lib/api/tags";

export interface SelectedTag {
  /** Existing tag id, or null for a tag that hasn't been created server-side yet. */
  id: string | null;
  name: string;
}

export interface TagAutocompleteProps {
  value: SelectedTag[];
  onChange: (next: SelectedTag[]) => void;
  ariaLabel?: string;
  placeholder?: string;
  /** When true, hides the chip list (caller renders chips itself). */
  hideChips?: boolean;
}

/**
 * Chip-input + debounced suggest. Existing tags are returned by the server;
 * the user can also press Enter on a query that has no exact match to create
 * a new tag (caller-side: server normalizes via TagService.NormalizeAndUpsertAsync
 * when the parent entity saves).
 */
export function TagAutocomplete({
  value, onChange, ariaLabel, placeholder = "Add a tag…", hideChips,
}: TagAutocompleteProps) {
  const [query, setQuery] = useState("");
  const [suggestions, setSuggestions] = useState<TagDto[]>([]);
  const [open, setOpen] = useState(false);
  const debounceRef = useRef<number | undefined>(undefined);

  useEffect(() => {
    if (!query.trim()) { setSuggestions([]); return; }
    window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => {
      tagsApi.search(query.trim(), 8)
        .then((d) => setSuggestions(d))
        .catch(() => setSuggestions([]));
    }, 180);
    return () => window.clearTimeout(debounceRef.current);
  }, [query]);

  const addTag = (tag: SelectedTag) => {
    const exists = value.some(
      (t) => t.name.localeCompare(tag.name, undefined, { sensitivity: "accent" }) === 0,
    );
    if (!exists) onChange([...value, tag]);
    setQuery("");
    setOpen(false);
  };

  const removeTag = (name: string) => {
    onChange(value.filter((t) =>
      t.name.localeCompare(name, undefined, { sensitivity: "accent" }) !== 0));
  };

  const exactMatch = suggestions.find((s) =>
    s.name.localeCompare(query.trim(), undefined, { sensitivity: "accent" }) === 0);

  const handleKey = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      const q = query.trim();
      if (!q) return;
      if (exactMatch) addTag({ id: exactMatch.id, name: exactMatch.name });
      else addTag({ id: null, name: q });
    } else if (e.key === "Backspace" && !query && value.length > 0) {
      onChange(value.slice(0, -1));
    }
  };

  return (
    <div>
      {!hideChips && (
        <ul className="mb-2 flex flex-wrap gap-2">
          {value.map((t) => (
            <li key={t.name}
              className="inline-flex items-center gap-1 rounded-full border bg-card px-2 py-0.5 text-xs">
              {t.name}
              <button type="button" aria-label={`Remove ${t.name}`}
                onClick={() => removeTag(t.name)}
                className="text-muted-foreground hover:text-destructive">×</button>
            </li>
          ))}
        </ul>
      )}
      <div className="relative">
        <input
          type="text"
          value={query}
          onChange={(e) => { setQuery(e.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          onBlur={() => window.setTimeout(() => setOpen(false), 150)}
          onKeyDown={handleKey}
          placeholder={placeholder}
          aria-label={ariaLabel ?? "Tag input"}
          className="h-10 w-full rounded-md border bg-background px-3 text-sm"
        />
        {open && (suggestions.length > 0 || query.trim()) && (
          <ul className="absolute z-10 mt-1 w-full rounded-md border bg-card shadow">
            {suggestions.map((s) => (
              <li key={s.id}>
                <button type="button"
                  onClick={() => addTag({ id: s.id, name: s.name })}
                  className="flex w-full items-center justify-between px-3 py-1.5 text-left text-sm hover:bg-muted">
                  <span>{s.name}</span>
                  <span className="text-xs text-muted-foreground">
                    {s.usageCount} use{s.usageCount === 1 ? "" : "s"}
                  </span>
                </button>
              </li>
            ))}
            {query.trim() && !exactMatch && (
              <li>
                <button type="button"
                  onClick={() => addTag({ id: null, name: query.trim() })}
                  className="flex w-full items-center px-3 py-1.5 text-left text-sm hover:bg-muted">
                  Create tag: <strong className="ml-1">{query.trim()}</strong>
                </button>
              </li>
            )}
          </ul>
        )}
      </div>
    </div>
  );
}
