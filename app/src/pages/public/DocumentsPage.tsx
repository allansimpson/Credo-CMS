import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { documentsApi } from "@/lib/api/documents";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { PublicDocument } from "@/types/api";

export function PublicDocumentsListPage() {
  const { settings } = useSiteSettings();
  const [items, setItems] = useState<PublicDocument[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    documentsApi.listPublic()
      .then((d) => { if (!cancelled) setItems(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  const grouped = useMemo(() => {
    const m = new Map<string, PublicDocument[]>();
    for (const d of items) {
      const a = m.get(d.category) ?? [];
      a.push(d);
      m.set(d.category, a);
    }
    for (const a of m.values()) a.sort((x, y) => x.title.localeCompare(y.title));
    return Array.from(m.entries()).sort(([a], [b]) => a.localeCompare(b));
  }, [items]);

  return (
    <div className="mx-auto max-w-3xl px-4 py-8">
      <SeoTags title={`Documents · ${settings?.churchName ?? ""}`}
        description="Documents and resources." />
      <h1 className="text-3xl font-bold sm:text-4xl">Documents</h1>

      {loading && <p className="mt-6 text-muted-foreground">Loading…</p>}
      {!loading && items.length === 0 && (
        <p className="mt-6 text-muted-foreground">No documents have been published yet.</p>
      )}

      <div className="mt-6 space-y-6">
        {grouped.map(([category, ds]) => (
          <section key={category}>
            <h2 className="text-xl font-semibold">{category}</h2>
            <ul className="mt-2 divide-y rounded-lg border bg-card">
              {ds.map((d) => (
                <li key={d.id} className="p-4">
                  <Link to={`/documents/${d.id}`} className="block hover:underline">
                    <p className="font-semibold">{d.title}</p>
                    {d.description && <p className="mt-1 text-sm text-muted-foreground">{d.description}</p>}
                    <p className="mt-1 text-xs text-muted-foreground">
                      {Math.round(d.sizeBytes / 1024)} KB
                      {d.isMembersOnly && " · Members only"}
                    </p>
                  </Link>
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>
    </div>
  );
}
