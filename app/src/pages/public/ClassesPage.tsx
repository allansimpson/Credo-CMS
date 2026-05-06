import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Calendar, Clock, MapPin, Users } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { useAuth } from "@/hooks/useAuth";
import {
  isMemberSlot,
  publicClassesApi,
  type ClassSlotResponse,
  type MemberClassSlot,
  type PublicClassSlot,
} from "@/lib/api/classes";

const ALL = "__all";

export function ClassesPage() {
  const { isAuthenticated } = useAuth();
  const [slots, setSlots] = useState<ClassSlotResponse[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeAge, setActiveAge] = useState<string>(ALL);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    publicClassesApi
      .list()
      .then((d) => { if (!cancelled) { setSlots(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load classes."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
    // Re-fetch on auth change so member-only fields appear after sign-in.
  }, [isAuthenticated]);

  const ageGroups = useMemo(() => {
    if (!slots) return [] as string[];
    const set = new Set(slots.map((s) => s.audienceAgeGroup).filter(Boolean));
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  }, [slots]);

  const grouped = useMemo(() => {
    const filtered = slots?.filter((s) => activeAge === ALL || s.audienceAgeGroup === activeAge) ?? [];
    const map = new Map<string, ClassSlotResponse[]>();
    for (const s of filtered) {
      const key = s.audienceAgeGroup || "Other";
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(s);
    }
    // Stable per-key sort by displayOrder, then name.
    for (const [, arr] of map) {
      arr.sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name));
    }
    return Array.from(map.entries()).sort(([a], [b]) => a.localeCompare(b));
  }, [slots, activeAge]);

  // The configurable page label lands in Q16 alongside the SiteSettings DTO
  // expansion. Phase 4 ships with a static fallback.
  const pageLabel = "Classes";

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-10">
          <header className="border-b pb-6">
            <h1 className="text-3xl font-bold">{pageLabel}</h1>
            <p className="mt-2 text-muted">
              Find a class that fits where you are.
              {!isAuthenticated && " Sign in to see teacher and room details."}
            </p>
          </header>

          {ageGroups.length > 1 && (
            <nav className="mt-6 flex flex-wrap gap-2" aria-label="Filter by age group">
              <FilterChip
                active={activeAge === ALL}
                label="All ages"
                onClick={() => setActiveAge(ALL)}
              />
              {ageGroups.map((g) => (
                <FilterChip
                  key={g}
                  active={activeAge === g}
                  label={g}
                  onClick={() => setActiveAge(g)}
                />
              ))}
            </nav>
          )}

          <section className="mt-6">
            {loading && <p className="text-muted">Loading classes…</p>}
            {error && <p className="text-danger">{error}</p>}
            {!loading && !error && grouped.length === 0 && (
              <p className="text-muted">No classes available right now.</p>
            )}

            {grouped.map(([ageGroup, groupSlots]) => (
              <section key={ageGroup} className="mt-8 first:mt-0">
                <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
                  {ageGroup}
                </h2>
                <ul className="mt-3 grid grid-cols-1 gap-4 md:grid-cols-2">
                  {groupSlots.map((s) => <SlotCard key={s.id} slot={s} />)}
                </ul>
              </section>
            ))}
          </section>
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function SlotCard({ slot }: { slot: ClassSlotResponse }) {
  const memberView = isMemberSlot(slot);
  const member = memberView ? (slot as MemberClassSlot) : null;
  const pub = slot as PublicClassSlot;
  const offering = pub.currentOffering ?? pub.upcomingOffering ?? pub.recentPastOffering;
  const offeringLabel = pub.currentOffering ? "Current"
    : pub.upcomingOffering ? "Upcoming"
    : pub.recentPastOffering ? "Recently ended"
    : null;

  return (
    <li>
      <Link
        to={`/classes/${slot.slug}`}
        className="flex h-full flex-col overflow-hidden rounded-lg border bg-card transition-colors hover:bg-panel-alt"
      >
        {slot.imageBlobUrl ? (
          <picture>
            {slot.imageWebpBlobUrl && <source srcSet={slot.imageWebpBlobUrl} type="image/webp" />}
            <img src={slot.imageBlobUrl} alt={slot.imageAltText ?? ""} className="h-40 w-full object-cover" />
          </picture>
        ) : (
          <div aria-hidden className="grid h-40 w-full place-items-center bg-panel-alt text-muted">
            <Users className="h-8 w-8" />
          </div>
        )}
        <div className="flex flex-1 flex-col p-4">
          <div className="flex items-center gap-2">
            <h3 className="font-heading text-lg font-semibold">{slot.name}</h3>
          </div>
          <p className="text-xs uppercase tracking-wide text-muted">{slot.audienceAgeGroup}</p>

          <dl className="mt-3 space-y-1 text-sm">
            {slot.generalMeetingTime && (
              <Row icon={<Clock className="h-3.5 w-3.5" />}>{slot.generalMeetingTime}</Row>
            )}
            {member?.defaultRoom && (
              <Row icon={<MapPin className="h-3.5 w-3.5" />}>{member.defaultRoom}</Row>
            )}
          </dl>

          {offering && (
            <div className="mt-auto pt-3">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-accent">
                {offeringLabel}
              </p>
              <p className="font-medium">{offering.subject}</p>
              <p className="flex items-center gap-1 text-xs text-muted">
                <Calendar className="h-3 w-3" />
                {formatRange(offering.startDate, offering.endDate)}
              </p>
              {memberView && "teacherLeaderName" in offering && offering.teacherLeaderName != null && (
                <p className="text-xs text-muted">
                  Teacher: {String(offering.teacherLeaderName)}
                </p>
              )}
            </div>
          )}
        </div>
      </Link>
    </li>
  );
}

function FilterChip({
  active, label, onClick,
}: { active: boolean; label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={
        "h-8 px-3 text-xs font-medium transition-colors " +
        (active
          ? "bg-accent text-accent-foreground"
          : "border border-border bg-card text-fg-soft hover:bg-panel-alt")
      }
    >
      {label}
    </button>
  );
}

function Row({ icon, children }: { icon: React.ReactNode; children: React.ReactNode }) {
  return (
    <div className="flex items-center gap-2 text-xs text-fg-soft">
      <span aria-hidden className="text-muted">{icon}</span>
      <span>{children}</span>
    </div>
  );
}

function formatRange(start: string, end: string): string {
  const s = new Date(start);
  const e = new Date(end);
  const sameYear = s.getUTCFullYear() === e.getUTCFullYear();
  const fmt = (d: Date) => d.toLocaleDateString(undefined, {
    month: "short", day: "numeric",
    ...(sameYear ? {} : { year: "numeric" }),
  });
  return `${fmt(s)} – ${fmt(e)}, ${e.getUTCFullYear()}`;
}
