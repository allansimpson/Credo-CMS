import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Clock, MapPin, GraduationCap } from "lucide-react";
import {
  isMemberSlot,
  publicClassesApi,
  type ClassSlotResponse,
  type MemberClassSlot,
} from "@/lib/api/classes";
import {
  Content,
  EmptyState,
  InlineError,
  PageHead,
  Panel,
  SkeletonCard,
} from "@/components/members/portal-primitives";

/**
 * Read-only classes index, grouped by AudienceAgeGroup. No registration,
 * no capacity, no "my classes" — Classes are curriculum, not events.
 * Member-augmented fields (DefaultRoom, teacher names) are role-gated
 * operational data, not enrollment state.
 */
export function ClassesListPage() {
  const [slots, setSlots] = useState<ClassSlotResponse[] | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setError(false);
    publicClassesApi.list()
      .then((d) => { if (!cancelled) setSlots(d); })
      .catch(() => { if (!cancelled) setError(true); });
    return () => { cancelled = true; };
  }, []);

  const grouped = useMemo(() => {
    if (!slots) return [];
    const map = new Map<string, ClassSlotResponse[]>();
    for (const s of slots) {
      const key = s.audienceAgeGroup || "Other";
      const bucket = map.get(key);
      if (bucket) bucket.push(s); else map.set(key, [s]);
    }
    return Array.from(map.entries()).map(([audience, items]) => ({
      audience,
      items: items.slice().sort((a, b) => a.displayOrder - b.displayOrder),
    }));
  }, [slots]);

  return (
    <Content>
      <PageHead
        title="Classes"
        sub="Browse what's offered. Classes here aren't registration — just show up at the meeting time."
      />

      {error && <InlineError onRetry={() => location.reload()} />}

      {!error && slots === null && (
        <div className="space-y-3"><SkeletonCard /><SkeletonCard /></div>
      )}

      {slots && slots.length === 0 && (
        <EmptyState
          icon={<GraduationCap strokeWidth={1.6} className="h-5 w-5" />}
          title="No classes yet"
          body="Classes will show up here as soon as your church publishes them."
        />
      )}

      {grouped.map((group) => (
        <section key={group.audience} className="mb-8">
          <h2 className="mb-3 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
            {group.audience}
          </h2>
          <ul className="grid gap-3 sm:grid-cols-2">
            {group.items.map((s) => <ClassCard key={s.id} slot={s} />)}
          </ul>
        </section>
      ))}
    </Content>
  );
}

function ClassCard({ slot }: { slot: ClassSlotResponse }) {
  const isMember = isMemberSlot(slot);
  const memberSlot = isMember ? (slot as MemberClassSlot) : null;
  const headline = slot.currentOffering ?? slot.upcomingOffering ?? null;
  const room = memberSlot?.defaultRoom;
  return (
    <li>
      <Link
        to={`/members/classes/${encodeURIComponent(slot.slug)}`}
        className="block h-full border border-border bg-panel transition-colors hover:bg-panel-alt"
      >
        <Panel noPad className="border-0">
          <div className="p-4">
            <h3 className="truncate font-heading text-base font-semibold">{slot.name}</h3>
            <div className="mt-2 flex flex-wrap items-center gap-3 font-mono text-[10.5px] uppercase tracking-[0.10em] text-muted">
              {slot.generalMeetingTime && (
                <span className="inline-flex items-center gap-1">
                  <Clock strokeWidth={1.75} className="h-3 w-3" />
                  {slot.generalMeetingTime}
                </span>
              )}
              {room && (
                <span className="inline-flex items-center gap-1">
                  <MapPin strokeWidth={1.75} className="h-3 w-3" />
                  {room}
                </span>
              )}
            </div>
            {headline && (
              <div className="mt-3 border-t border-border-soft pt-3">
                <p className="font-mono text-[10px] uppercase tracking-[0.12em] text-accent">
                  {slot.currentOffering ? "Now" : "Upcoming"}
                </p>
                <p className="mt-1 font-heading text-sm font-medium">{headline.subject}</p>
                <p className="mt-1 font-mono text-[10.5px] uppercase tracking-[0.10em] text-muted">
                  {formatDate(headline.startDate)} – {formatDate(headline.endDate)}
                </p>
              </div>
            )}
          </div>
        </Panel>
      </Link>
    </li>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
}
