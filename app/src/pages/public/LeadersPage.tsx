import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { leadersApi } from "@/lib/api/leaders";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { Eyebrow, Headline, ImageSlot } from "@/components/public";
import { Mail } from "lucide-react";
import { extractText } from "@/lib/parsePageSections";
import type { PublicLeader } from "@/types/api";

function bioExcerpt(bioJson: string | null): string {
  if (!bioJson) return "";
  try {
    const doc = JSON.parse(bioJson);
    const texts: string[] = [];
    function walk(node: { text?: string; content?: unknown[] }) {
      if (node.text) texts.push(node.text);
      if (Array.isArray(node.content)) node.content.forEach((c) => walk(c as typeof node));
    }
    walk(doc);
    return texts.join("");
  } catch {
    return "";
  }
}

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
    const order = ["Ministers", "Staff", "Elders", "Deacons"];
    const groups = new Map<string, typeof items>();
    for (const l of items) {
      const arr = groups.get(l.category) ?? [];
      arr.push(l);
      groups.set(l.category, arr);
    }
    for (const arr of groups.values()) arr.sort((a, b) => a.displayOrder - b.displayOrder);
    return order.filter((c) => groups.has(c)).map((c) => ({ category: c, leaders: groups.get(c)! }));
  }, [items]);

  return (
    <div>
      <SeoTags
        title={`Leaders & Staff · ${settings?.churchName ?? ""}`}
        description="The people who answer the phone."
      />

      {/* ── Header ────────────────────────────────────────────── */}
      <header className="mx-auto max-w-7xl px-6 py-12 md:py-16">
        <Eyebrow accent>Leaders &amp; Staff</Eyebrow>
        <Headline as="h1" size="display" className="mt-3 max-w-4xl">
          The people who answer the phone.
        </Headline>
        <p className="mt-4 max-w-2xl text-fg-soft leading-relaxed">
          We&rsquo;re a small staff — five pastors and seven lay leaders. We&rsquo;d be glad
          to meet you. The fastest way to reach any of us is the church office; emails listed
          below go straight to inboxes.
        </p>
      </header>

      {loading && <p className="mx-auto max-w-7xl px-6 text-muted">Loading…</p>}

      {grouped.map((group) => (
        <section key={group.category} className="mx-auto max-w-7xl px-6 pb-12">
          <Eyebrow accent className="mb-4">{group.category}</Eyebrow>
          <div className="grid gap-px border border-border-soft bg-border-soft md:grid-cols-3">
            {group.leaders.map((l) => (
              <div key={l.id} className="bg-background p-6">
                <Link to={`/leaders/${l.id}`} className="block">
                  {l.photoUrl ? (
                    <picture>
                      {l.photoWebpUrl && <source srcSet={l.photoWebpUrl} type="image/webp" />}
                      <img
                        src={l.photoUrl}
                        alt={l.photoAlt ?? l.fullName}
                        className="aspect-[4/5] w-full object-cover"
                      />
                    </picture>
                  ) : (
                    <ImageSlot ratio="4:5" label={l.fullName} alt={l.fullName} />
                  )}
                </Link>
                {l.title && (
                  <p className="mt-4 text-[11px] font-semibold uppercase tracking-[0.14em] text-accent">
                    {l.title}
                  </p>
                )}
                <h3 className="mt-1 text-xl font-semibold">{l.fullName}</h3>
                {l.bioJson && (
                  <p className="mt-2 text-sm text-fg-soft leading-relaxed">
                    {bioExcerpt(l.bioJson)}
                  </p>
                )}
                {l.email && (
                  <a
                    href={`mailto:${l.email}`}
                    className="mt-3 inline-flex items-center gap-1.5 text-sm text-accent hover:underline"
                  >
                    <Mail size={14} strokeWidth={1.5} /> {l.email}
                  </a>
                )}
              </div>
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
