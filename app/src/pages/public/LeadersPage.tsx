import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { leadersApi } from "@/lib/api/leaders";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import type { PublicLeader } from "@/types/api";

export function PublicLeadersPage() {
  const { settings } = useSiteSettings();
  const [items, setItems] = useState<PublicLeader[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    leadersApi.listPublic()
      .then((d) => { if (!cancelled) setItems(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  const grouped = useMemo(() => {
    const groups = new Map<string, PublicLeader[]>();
    for (const l of items) {
      const arr = groups.get(l.category) ?? [];
      arr.push(l);
      groups.set(l.category, arr);
    }
    for (const arr of groups.values()) {
      arr.sort((a, b) => a.displayOrder - b.displayOrder || a.fullName.localeCompare(b.fullName));
    }
    return Array.from(groups.entries());
  }, [items]);

  const label = settings?.leadersPageLabel ?? "Our Leaders";

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <SeoTags title={`${label} · ${settings?.churchName ?? ""}`}
        description={`Meet the leaders of ${settings?.churchName ?? "the church"}.`} />
      <h1 className="text-3xl font-bold sm:text-4xl">{label}</h1>

      {loading && <p className="mt-6 text-muted-foreground">Loading…</p>}
      {!loading && items.length === 0 && (
        <p className="mt-6 text-muted-foreground">Leaders haven't been published yet.</p>
      )}

      <div className="mt-6 space-y-8">
        {grouped.map(([category, ls]) => (
          <section key={category}>
            <h2 className="text-xl font-semibold">{category}</h2>
            <ul className="mt-3 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {ls.map((l) => (
                <li key={l.id} className="rounded-lg border bg-card p-4">
                  <Link to={`/leaders/${l.id}`} className="block">
                    {l.photoUrl ? (
                      <picture>
                        {l.photoWebpUrl && <source srcSet={l.photoWebpUrl} type="image/webp" />}
                        <img src={l.photoUrl} alt={l.photoAlt ?? l.fullName}
                          className="mx-auto h-24 w-24 rounded-full object-cover" />
                      </picture>
                    ) : (
                      <div className="mx-auto h-24 w-24 rounded-full bg-muted" />
                    )}
                    <p className="mt-3 text-center font-semibold">{l.fullName}</p>
                    {l.title && <p className="text-center text-xs text-muted-foreground">{l.title}</p>}
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
