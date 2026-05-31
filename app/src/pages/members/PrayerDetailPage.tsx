import { lazy, Suspense, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Heart, MessageSquare, Edit, User } from "lucide-react";
import {
  memberPrayerApi,
  type MemberPrayerRequest,
} from "@/lib/api/prayerRequests";
import {
  Content,
  InlineError,
  PageHead,
  Panel,
  Skeleton,
} from "@/components/members/portal-primitives";

const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly })),
);

/**
 * Prayer detail. "I prayed for this" toggle is the only member-facing
 * action. Updates list is rendered as staff posts — no member comment
 * composer per resolution. Terminal state is "Archived" only; "Answered"
 * is never surfaced to members.
 */
export function PrayerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [item, setItem] = useState<MemberPrayerRequest | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [toggling, setToggling] = useState(false);

  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    setLoading(true);
    setError(false);
    memberPrayerApi.get(id)
      .then((d) => { if (!cancelled) setItem(d); })
      .catch(() => { if (!cancelled) setError(true); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [id]);

  const togglePrayed = async () => {
    if (!item || toggling) return;
    setToggling(true);
    // Optimistic update.
    const wasOn = item.viewerHasPrayed;
    setItem({ ...item, viewerHasPrayed: !wasOn, prayedForCount: item.prayedForCount + (wasOn ? -1 : 1) });
    try {
      const { count } = wasOn
        ? await memberPrayerApi.unmarkPrayed(item.id)
        : await memberPrayerApi.markPrayed(item.id);
      setItem((prev) => prev ? { ...prev, prayedForCount: count, viewerHasPrayed: !wasOn } : prev);
    } catch {
      // Roll back.
      setItem({ ...item, viewerHasPrayed: wasOn, prayedForCount: item.prayedForCount });
    } finally {
      setToggling(false);
    }
  };

  if (loading) {
    return (
      <Content maxWidth="max-w-[680px]">
        <PageHead title="" onBack={() => navigate("/members/prayer")} />
        <Skeleton className="mb-3 h-8 w-2/3" />
        <Skeleton className="mb-2 h-3 w-1/3" />
        <Skeleton className="mt-6 h-32 w-full" />
      </Content>
    );
  }

  if (error || !item) {
    return (
      <Content maxWidth="max-w-[680px]">
        <PageHead title="" onBack={() => navigate("/members/prayer")} />
        <InlineError onRetry={() => location.reload()} />
      </Content>
    );
  }

  const submitter = item.isAnonymous ? "Anonymous" : item.submitterDisplayName ?? "Member";
  const createdLabel = new Date(item.createdAt).toLocaleDateString("en-US", {
    month: "short", day: "numeric", year: "numeric",
  });

  return (
    <Content maxWidth="max-w-[680px]">
      <PageHead
        title={item.title}
        onBack={() => navigate("/members/prayer")}
        actions={
          item.viewerCanEdit && (
            <button
              type="button"
              onClick={() => navigate(`/members/prayer/${item.id}/edit`)}
              className="inline-flex items-center gap-2 border border-border bg-panel px-3 py-1.5 text-sm font-medium hover:bg-panel-alt"
            >
              <Edit strokeWidth={1.75} className="h-4 w-4" /> Edit
            </button>
          )
        }
      />

      <p className="mb-4 flex items-center gap-2 font-mono text-[11px] uppercase tracking-[0.12em] text-muted">
        <User strokeWidth={1.5} className="h-3.5 w-3.5" /> {submitter} · {createdLabel}
      </p>

      <Panel className="mb-5">
        <Suspense fallback={<Skeleton className="h-24 w-full" />}>
          <div className="prose-editorial">
            <TipTapReadOnly json={item.bodyJson} />
          </div>
        </Suspense>
      </Panel>

      {/* I prayed for this */}
      <div className="mb-6 flex items-center justify-between border border-border bg-panel px-4 py-3">
        <p className="text-sm">
          <span className="font-mono text-xs uppercase tracking-[0.12em] text-muted">
            {item.prayedForCount}
          </span>{" "}
          {item.prayedForCount === 1 ? "person has" : "people have"} prayed for this.
        </p>
        <button
          type="button"
          onClick={togglePrayed}
          disabled={toggling}
          aria-pressed={item.viewerHasPrayed}
          className={`inline-flex items-center gap-2 border px-4 py-2 text-sm font-semibold transition-colors disabled:opacity-50 ${
            item.viewerHasPrayed
              ? "border-accent bg-accent text-accent-foreground"
              : "border-border bg-panel-alt text-foreground hover:bg-panel"
          }`}
        >
          <Heart
            strokeWidth={1.75}
            className={`h-4 w-4 ${item.viewerHasPrayed ? "fill-current" : ""}`}
          />
          {item.viewerHasPrayed ? "You're praying" : "I prayed for this"}
        </button>
      </div>

      {/* Staff updates */}
      {item.updates.length > 0 && (
        <section>
          <h3 className="mb-3 flex items-center gap-2 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
            <MessageSquare strokeWidth={1.75} className="h-3.5 w-3.5" />
            Updates from staff
          </h3>
          <ul className="space-y-3">
            {item.updates.map((u) => {
              const when = new Date(u.createdAt).toLocaleDateString("en-US", {
                month: "short", day: "numeric", year: "numeric",
              });
              return (
                <li key={u.id}>
                  <Panel>
                    <p className="mb-2 font-mono text-[10.5px] uppercase tracking-[0.12em] text-muted">
                      {u.postedByLabel} · {when}
                    </p>
                    <Suspense fallback={<Skeleton className="h-12 w-full" />}>
                      <div className="prose-editorial">
                        <TipTapReadOnly json={u.bodyJson} />
                      </div>
                    </Suspense>
                  </Panel>
                </li>
              );
            })}
          </ul>
        </section>
      )}
    </Content>
  );
}
