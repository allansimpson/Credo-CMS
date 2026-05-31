import { lazy, Suspense, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Clock, MapPin, User, BookOpen } from "lucide-react";
import {
  isMemberSlot,
  publicClassesApi,
  type ClassSlotResponse,
  type MemberClassOffering,
  type MemberClassSlot,
} from "@/lib/api/classes";
import {
  Content,
  EmptyState,
  InlineError,
  MetaRow,
  PageHead,
  Panel,
  Skeleton,
} from "@/components/members/portal-primitives";

const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly })),
);

export function ClassDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [slot, setSlot] = useState<ClassSlotResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<"notfound" | "load" | null>(null);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setLoading(true);
    setError(null);
    publicClassesApi.get(slug)
      .then((d) => { if (!cancelled) setSlot(d); })
      .catch((err: { status?: number }) => {
        if (!cancelled) setError(err?.status === 404 ? "notfound" : "load");
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [slug]);

  if (loading) {
    return (
      <Content>
        <PageHead title="" onBack={() => navigate("/members/classes")} />
        <Skeleton className="h-8 w-2/3" />
      </Content>
    );
  }

  if (error === "notfound" || !slot) {
    return (
      <Content>
        <PageHead title="Not found" onBack={() => navigate("/members/classes")} />
        <EmptyState title="This class isn't available" />
      </Content>
    );
  }

  if (error === "load") {
    return (
      <Content>
        <PageHead title="" onBack={() => navigate("/members/classes")} />
        <InlineError onRetry={() => location.reload()} />
      </Content>
    );
  }

  const isMember = isMemberSlot(slot);
  const memberSlot = isMember ? (slot as MemberClassSlot) : null;

  return (
    <Content>
      <PageHead
        title={slot.name}
        sub={slot.audienceAgeGroup}
        onBack={() => navigate("/members/classes")}
      />

      {/* Slot meta */}
      <Panel noPad className="mb-5 px-4">
        {slot.generalMeetingTime && (
          <MetaRow
            icon={<Clock strokeWidth={1.5} className="h-4 w-4" />}
            label="Meets"
            value={slot.generalMeetingTime}
          />
        )}
        {memberSlot?.defaultRoom && (
          <MetaRow
            icon={<MapPin strokeWidth={1.5} className="h-4 w-4" />}
            label="Room"
            value={memberSlot.defaultRoom}
          />
        )}
      </Panel>

      {/* Description */}
      {slot.descriptionJson && (
        <Panel className="mb-5">
          <Suspense fallback={<Skeleton className="h-12 w-full" />}>
            <div className="prose-editorial">
              <TipTapReadOnly json={slot.descriptionJson} />
            </div>
          </Suspense>
        </Panel>
      )}

      {/* Offerings */}
      {(slot.currentOffering || slot.upcomingOffering || slot.recentPastOffering) && (
        <section className="mb-5">
          <h2 className="mb-3 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
            Offerings
          </h2>
          <div className="space-y-3">
            {slot.currentOffering && (
              <OfferingPanel label="Now" offering={slot.currentOffering as MemberClassOffering} isMember={isMember} />
            )}
            {slot.upcomingOffering && (
              <OfferingPanel label="Upcoming" offering={slot.upcomingOffering as MemberClassOffering} isMember={isMember} />
            )}
            {slot.recentPastOffering && (
              <OfferingPanel label="Recent" offering={slot.recentPastOffering as MemberClassOffering} isMember={isMember} />
            )}
          </div>
        </section>
      )}
    </Content>
  );
}

function OfferingPanel({
  label,
  offering,
  isMember,
}: {
  label: string;
  offering: MemberClassOffering;
  isMember: boolean;
}) {
  const teacher = isMember ? (offering.teacherLeaderName ?? offering.teacherFreeText) : null;
  return (
    <Panel>
      <p className="mb-1 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-accent">
        {label}
      </p>
      <h3 className="font-heading text-base font-semibold">{offering.subject}</h3>
      <p className="mt-1 font-mono text-[10.5px] uppercase tracking-[0.10em] text-muted">
        {formatDate(offering.startDate)} – {formatDate(offering.endDate)}
      </p>
      {offering.descriptionJson && (
        <div className="mt-3 text-sm text-fg-soft">
          <Suspense fallback={<Skeleton className="h-12 w-full" />}>
            <TipTapReadOnly json={offering.descriptionJson} />
          </Suspense>
        </div>
      )}
      {isMember && (teacher || offering.materialsNeeded || offering.detailedScheduleJson) && (
        <div className="mt-4 space-y-2 border-t border-border-soft pt-3">
          {teacher && (
            <p className="flex items-center gap-2 text-sm">
              <User strokeWidth={1.5} className="h-4 w-4 text-muted" />
              <span className="text-muted">Teacher:</span> {teacher}
            </p>
          )}
          {offering.materialsNeeded && (
            <p className="flex items-start gap-2 text-sm">
              <BookOpen strokeWidth={1.5} className="mt-0.5 h-4 w-4 shrink-0 text-muted" />
              <span>
                <span className="text-muted">Materials:</span> {offering.materialsNeeded}
              </span>
            </p>
          )}
        </div>
      )}
    </Panel>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
}
