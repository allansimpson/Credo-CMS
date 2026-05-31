import { lazy, Suspense, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Lock, UsersRound, Mail, Clock, Check, Crown, LogOut } from "lucide-react";
import {
  GroupJoinability,
  GroupVisibility,
  MessageOnJoinRequest,
  profileGroupsApi,
  publicGroupsApi,
  type PublicGroupDetail,
} from "@/lib/api/groups";
import {
  Avatar,
  Banner,
  Content,
  EmptyState,
  InlineError,
  PageHead,
  Panel,
  Skeleton,
} from "@/components/members/portal-primitives";

const TipTapReadOnly = lazy(() =>
  import("@/components/shared/TipTapReadOnly").then((m) => ({ default: m.TipTapReadOnly })),
);

export function GroupDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [group, setGroup] = useState<PublicGroupDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<"notfound" | "load" | null>(null);
  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setLoading(true);
    setError(null);
    publicGroupsApi.get(slug)
      .then((d) => { if (!cancelled) setGroup(d); })
      .catch((err: { status?: number }) => {
        if (!cancelled) setError(err?.status === 404 ? "notfound" : "load");
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [slug]);

  const requestJoin = async () => {
    if (!group || !slug) return;
    const requirement = group.requiresMessageOnJoinRequest;
    if (requirement === MessageOnJoinRequest.Required && !message.trim()) {
      setActionError("A message is required to request joining this group.");
      return;
    }
    setSubmitting(true);
    setActionError(null);
    try {
      await publicGroupsApi.requestJoin(slug, { message: message.trim() || null });
      const refreshed = await publicGroupsApi.get(slug);
      setGroup(refreshed);
      setMessage("");
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Couldn't send your request. Try again."];
      setActionError(messages[0] ?? "Couldn't send your request. Try again.");
    } finally {
      setSubmitting(false);
    }
  };

  const leave = async () => {
    if (!group || !slug) return;
    if (!confirm(`Leave ${group.name}?`)) return;
    setSubmitting(true);
    setActionError(null);
    try {
      await profileGroupsApi.leave(group.id);
      const refreshed = await publicGroupsApi.get(slug);
      setGroup(refreshed);
    } catch {
      setActionError("Couldn't leave the group. Try again.");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <Content>
        <PageHead title="" onBack={() => navigate("/members/groups")} />
        <Skeleton className="mb-4 aspect-[16/6] w-full" />
        <Skeleton className="mb-2 h-7 w-1/2" />
        <Skeleton className="h-3 w-1/3" />
      </Content>
    );
  }

  if (error === "notfound" || !group) {
    return (
      <Content>
        <PageHead title="Not found" onBack={() => navigate("/members/groups")} />
        <EmptyState
          title="This group isn't available"
          body="It may have been removed, or you may not have access."
        />
      </Content>
    );
  }

  if (error === "load") {
    return (
      <Content>
        <PageHead title="" onBack={() => navigate("/members/groups")} />
        <InlineError onRetry={() => location.reload()} />
      </Content>
    );
  }

  const isMembersOnly = group.visibility === GroupVisibility.MembersOnly;

  return (
    <Content>
      <PageHead title={group.name} onBack={() => navigate("/members/groups")} />

      {group.imageBlobUrl && (
        <picture>
          {group.imageWebpBlobUrl && (
            <source srcSet={group.imageWebpBlobUrl} type="image/webp" />
          )}
          <img
            src={group.imageBlobUrl}
            alt={group.imageAltText ?? ""}
            className="mb-5 aspect-[16/6] w-full border border-border object-cover"
          />
        </picture>
      )}

      {group.viewerHasPendingRequest && (
        <Banner tone="warn" icon={<Clock strokeWidth={1.75} className="h-4 w-4" />}>
          Your request to join is pending leader approval.
        </Banner>
      )}

      {/* Description */}
      {group.descriptionJson && (
        <Panel className="mb-5">
          <Suspense fallback={<Skeleton className="h-12 w-full" />}>
            <div className="prose-editorial">
              <TipTapReadOnly json={group.descriptionJson} />
            </div>
          </Suspense>
        </Panel>
      )}

      {/* Meta */}
      <div className="mb-5 flex flex-wrap gap-3 text-sm">
        {group.meetingInfo && (
          <span className="inline-flex items-center gap-2 border border-border bg-panel px-3 py-1.5">
            <UsersRound strokeWidth={1.5} className="h-3.5 w-3.5 text-muted" />
            {group.meetingInfo}
          </span>
        )}
        {isMembersOnly && (
          <span className="inline-flex items-center gap-2 border border-border bg-panel px-3 py-1.5 text-muted">
            <Lock strokeWidth={1.5} className="h-3.5 w-3.5" />
            Members only
          </span>
        )}
        {group.contactEmail && (
          <a
            href={`mailto:${group.contactEmail}`}
            className="inline-flex items-center gap-2 border border-border bg-panel px-3 py-1.5 text-accent hover:underline"
          >
            <Mail strokeWidth={1.5} className="h-3.5 w-3.5" />
            {group.contactEmail}
          </a>
        )}
      </div>

      {/* Action area */}
      <section className="mb-6">
        {group.viewerIsMember ? (
          <div className="flex items-center justify-between border border-border bg-panel p-4">
            <p className="flex items-center gap-2 text-sm">
              <Check strokeWidth={1.75} className="h-4 w-4 text-success" />
              You're a member of this group.
            </p>
            <button
              type="button"
              onClick={leave}
              disabled={submitting}
              className="inline-flex items-center gap-2 border border-danger/30 bg-panel px-4 py-2 text-sm text-danger hover:bg-danger/10 disabled:opacity-50"
            >
              <LogOut strokeWidth={1.5} className="h-4 w-4" />
              Leave group
            </button>
          </div>
        ) : group.viewerHasPendingRequest ? (
          <Panel>
            <p className="text-sm text-fg-soft">
              Your request is awaiting leader approval. You'll be notified once it's reviewed.
            </p>
          </Panel>
        ) : group.joinability === GroupJoinability.InviteOnly ? (
          <Panel>
            <p className="text-sm text-muted">
              This group is invite only. Reach out to a leader if you'd like to join.
            </p>
          </Panel>
        ) : group.joinability === GroupJoinability.Closed ? (
          <Panel>
            <p className="text-sm text-muted">
              This group isn't accepting new members right now.
            </p>
          </Panel>
        ) : (
          <JoinRequestForm
            messageRequirement={group.requiresMessageOnJoinRequest}
            message={message}
            onMessageChange={setMessage}
            submitting={submitting}
            error={actionError}
            onSubmit={requestJoin}
          />
        )}
      </section>

      {/* Roster */}
      <section>
        <h3 className="mb-3 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
          Members
        </h3>
        {group.roster === null ? (
          <Panel>
            <p className="text-sm text-muted">
              {group.viewerIsMember
                ? "Roster is visible to group leaders only."
                : "The member list is visible after you join."}
            </p>
          </Panel>
        ) : group.roster.length === 0 ? (
          <Panel>
            <p className="text-sm text-muted">No members yet.</p>
          </Panel>
        ) : (
          <ul className="grid gap-2 sm:grid-cols-2">
            {group.roster.map((r) => (
              <li
                key={r.userId}
                className="flex items-center gap-3 border border-border bg-panel p-3"
              >
                <Avatar
                  name={r.displayName}
                  size={36}
                  src={r.photoBlobUrl}
                  webpSrc={r.photoWebpBlobUrl}
                  alt={r.photoAltText}
                />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{r.displayName}</p>
                  {r.isLeader && (
                    <p className="mt-0.5 inline-flex items-center gap-1 font-mono text-[10px] uppercase tracking-[0.12em] text-accent">
                      <Crown strokeWidth={1.75} className="h-3 w-3" /> Leader
                    </p>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </Content>
  );
}

function JoinRequestForm({
  messageRequirement,
  message,
  onMessageChange,
  submitting,
  error,
  onSubmit,
}: {
  messageRequirement: MessageOnJoinRequest;
  message: string;
  onMessageChange: (v: string) => void;
  submitting: boolean;
  error: string | null;
  onSubmit: () => void;
}) {
  const showMessage = messageRequirement !== MessageOnJoinRequest.Hidden;
  const required = messageRequirement === MessageOnJoinRequest.Required;
  return (
    <Panel>
      <h3 className="mb-3 font-heading text-base font-semibold">Request to join</h3>
      {showMessage && (
        <div className="mb-3">
          <label htmlFor="join-message" className="mb-1.5 block text-sm font-medium">
            Message {required && <span className="text-danger">*</span>}
            {!required && <span className="text-muted"> (optional)</span>}
          </label>
          <textarea
            id="join-message"
            rows={3}
            maxLength={1000}
            required={required}
            value={message}
            onChange={(e) => onMessageChange(e.target.value)}
            placeholder={required ? "Tell the leaders why you'd like to join…" : "Anything the leaders should know? (optional)"}
            className="w-full border border-border bg-panel px-3 py-2 text-sm focus-visible:border-accent focus-visible:outline-none"
          />
        </div>
      )}
      {error && <InlineError message={error} />}
      <div className="mt-3">
        <button
          type="button"
          onClick={onSubmit}
          disabled={submitting}
          className="inline-flex items-center gap-2 bg-accent px-4 py-2 text-sm font-semibold text-accent-foreground hover:bg-accent/90 disabled:opacity-50"
        >
          {submitting ? "Sending…" : "Request to join"}
        </button>
      </div>
    </Panel>
  );
}
