import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Calendar, Clock, MapPin, Users } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { useAuth } from "@/hooks/useAuth";
import {
  isMemberSlot,
  publicClassesApi,
  type ClassOfferingResponse,
  type ClassSlotResponse,
  type MemberClassOffering,
  type MemberClassSlot,
} from "@/lib/api/classes";

export function ClassDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const { isAuthenticated } = useAuth();
  const [slot, setSlot] = useState<ClassSlotResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setLoading(true); setNotFound(false);
    publicClassesApi
      .get(slug)
      .then((d) => { if (!cancelled) setSlot(d); })
      .catch((err) => {
        if (cancelled) return;
        if ((err as { status?: number }).status === 404) setNotFound(true);
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [slug, isAuthenticated]);

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-10">
          <Link
            to="/classes"
            className="mb-6 inline-flex items-center gap-1 text-sm text-muted hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" /> All classes
          </Link>

          {loading && <p className="text-muted">Loading…</p>}
          {!loading && notFound && (
            <div className="rounded-lg border bg-card p-6">
              <h1 className="text-xl font-bold">Class not found</h1>
              <p className="mt-2 text-sm text-muted">This class may have moved or been removed.</p>
            </div>
          )}
          {!loading && slot && <SlotDetail slot={slot} />}
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function SlotDetail({ slot }: { slot: ClassSlotResponse }) {
  const memberView = isMemberSlot(slot);
  const member = memberView ? (slot as MemberClassSlot) : null;

  return (
    <article className="space-y-8">
      {slot.imageBlobUrl && (
        <picture>
          {slot.imageWebpBlobUrl && <source srcSet={slot.imageWebpBlobUrl} type="image/webp" />}
          <img src={slot.imageBlobUrl} alt={slot.imageAltText ?? ""} className="h-72 w-full object-cover" />
        </picture>
      )}

      <header className="border-b pb-6">
        <p className="text-xs font-semibold uppercase tracking-wide text-muted">
          {slot.audienceAgeGroup}
        </p>
        <h1 className="mt-2 text-3xl font-bold leading-tight">{slot.name}</h1>
        <dl className="mt-4 grid grid-cols-1 gap-2 text-sm sm:grid-cols-2">
          {slot.generalMeetingTime && (
            <Row icon={<Clock className="h-4 w-4" />} label="Meets">{slot.generalMeetingTime}</Row>
          )}
          {member?.defaultRoom && (
            <Row icon={<MapPin className="h-4 w-4" />} label="Room">{member.defaultRoom}</Row>
          )}
        </dl>
      </header>

      {slot.descriptionJson && (
        <section className="prose prose-sm max-w-none">
          <TipTapReadOnly json={slot.descriptionJson} />
        </section>
      )}

      {slot.currentOffering && (
        <OfferingSection title="Current series" offering={slot.currentOffering} memberView={memberView} accent />
      )}
      {!slot.currentOffering && slot.upcomingOffering && (
        <OfferingSection title="Upcoming series" offering={slot.upcomingOffering} memberView={memberView} accent />
      )}
      {!slot.currentOffering && !slot.upcomingOffering && slot.recentPastOffering && (
        <OfferingSection
          title="Recently ended"
          offering={slot.recentPastOffering}
          memberView={memberView}
        />
      )}
      {/* Even when a current series is shown, the upcoming series gets a smaller
          callout below — useful when an offering ends mid-month. */}
      {slot.currentOffering && slot.upcomingOffering && (
        <OfferingSection title="Up next" offering={slot.upcomingOffering} memberView={memberView} />
      )}

      {!member && (
        <aside className="rounded-lg border bg-panel-alt p-4 text-sm text-muted">
          <p className="flex items-center gap-2">
            <Users className="h-4 w-4" />
            Sign in to see teacher, room, and detailed schedule for members.
          </p>
        </aside>
      )}
    </article>
  );
}

function OfferingSection({
  title, offering, memberView, accent,
}: {
  title: string;
  offering: ClassOfferingResponse;
  memberView: boolean;
  accent?: boolean;
}) {
  const member = memberView ? (offering as MemberClassOffering) : null;
  return (
    <section className={
      "relative space-y-3 border bg-card p-5 " +
      (accent ? "border-l-[3px] border-l-accent" : "")
    }>
      <p className="text-[11px] font-semibold uppercase tracking-wider text-accent">
        {title}
      </p>
      <h2 className="text-2xl font-bold">{offering.subject}</h2>
      <p className="flex items-center gap-1.5 text-sm text-muted">
        <Calendar className="h-3.5 w-3.5" />
        {formatRange(offering.startDate, offering.endDate)}
      </p>
      {offering.descriptionJson && (
        <div className="prose prose-sm max-w-none pt-2">
          <TipTapReadOnly json={offering.descriptionJson} />
        </div>
      )}
      {member && (member.teacherLeaderName || member.teacherFreeText || member.materialsNeeded || member.detailedScheduleJson) && (
        <div className="mt-3 space-y-3 border-t pt-3 text-sm">
          {(member.teacherLeaderName || member.teacherFreeText) && (
            <p>
              <span className="text-muted">Teacher: </span>
              <span className="font-medium">
                {member.teacherLeaderName ?? member.teacherFreeText}
              </span>
            </p>
          )}
          {member.materialsNeeded && (
            <p>
              <span className="text-muted">Materials: </span>
              <span>{member.materialsNeeded}</span>
            </p>
          )}
          {member.detailedScheduleJson && (
            <details className="text-sm">
              <summary className="cursor-pointer text-primary">Weekly schedule</summary>
              <div className="prose prose-sm mt-2 max-w-none">
                <TipTapReadOnly json={member.detailedScheduleJson} />
              </div>
            </details>
          )}
        </div>
      )}
    </section>
  );
}

function Row({ icon, label, children }: { icon: React.ReactNode; label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-start gap-2">
      <span aria-hidden className="mt-0.5 text-muted">{icon}</span>
      <div>
        <dt className="text-xs uppercase tracking-wide text-muted">{label}</dt>
        <dd className="mt-0.5">{children}</dd>
      </div>
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
