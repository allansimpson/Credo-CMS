import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Mail, Users, X } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { useAuth } from "@/hooks/useAuth";
import {
  GroupJoinability,
  GroupVisibility,
  MessageOnJoinRequest,
  publicGroupsApi,
  type PublicGroupDetail,
} from "@/lib/api/groups";

export function GroupDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [group, setGroup] = useState<PublicGroupDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [joinModalOpen, setJoinModalOpen] = useState(false);

  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setLoading(true); setNotFound(false);
    publicGroupsApi
      .get(slug)
      .then((d) => { if (!cancelled) setGroup(d); })
      .catch((err) => {
        if (cancelled) return;
        if ((err as { status?: number }).status === 404) setNotFound(true);
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [slug, isAuthenticated]);

  const refresh = async () => {
    if (!slug) return;
    const fresh = await publicGroupsApi.get(slug);
    setGroup(fresh);
  };

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-10">
          <Link
            to="/get-involved"
            className="mb-6 inline-flex items-center gap-1 text-sm text-muted hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" /> All groups
          </Link>

          {loading && <p className="text-muted">Loading…</p>}
          {!loading && notFound && (
            <div className="rounded-lg border bg-card p-6">
              <h1 className="text-xl font-bold">Group not found</h1>
              <p className="mt-2 text-sm text-muted">
                This group isn't available, or you may need to sign in to view it.
              </p>
              {!isAuthenticated && (
                <Link
                  to={`/login?return=${encodeURIComponent(`/groups/${slug}`)}`}
                  className="mt-4 inline-flex h-10 items-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
                >
                  Sign in
                </Link>
              )}
            </div>
          )}
          {!loading && group && (
            <article className="space-y-8">
              {group.imageBlobUrl && (
                <picture>
                  {group.imageWebpBlobUrl && <source srcSet={group.imageWebpBlobUrl} type="image/webp" />}
                  <img
                    src={group.imageBlobUrl}
                    alt={group.imageAltText ?? ""}
                    className="h-72 w-full object-cover"
                  />
                </picture>
              )}

              <header className="border-b pb-6">
                <h1 className="text-3xl font-bold">{group.name}</h1>
                {group.meetingInfo && (
                  <p className="mt-2 text-sm text-fg-soft">{group.meetingInfo}</p>
                )}
                <JoinCta
                  group={group}
                  isAuthenticated={isAuthenticated}
                  onRequestJoin={() => setJoinModalOpen(true)}
                  onSignIn={() => navigate(`/login?return=${encodeURIComponent(`/groups/${group.slug}`)}`)}
                />
              </header>

              {group.descriptionJson && (
                <section>
                  <div className="prose prose-sm max-w-none">
                    <TipTapReadOnly valueJson={group.descriptionJson} />
                  </div>
                </section>
              )}

              {group.contactEmail && (
                <section className="rounded-lg border bg-card p-6">
                  <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">Contact</h2>
                  <a
                    href={`mailto:${group.contactEmail}`}
                    className="mt-2 inline-flex items-center gap-2 text-primary hover:underline"
                  >
                    <Mail className="h-4 w-4" /> {group.contactEmail}
                  </a>
                </section>
              )}

              {group.roster && (
                <section className="rounded-lg border bg-card p-6">
                  <h2 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-muted">
                    <Users className="h-4 w-4" /> Members ({group.roster.length})
                  </h2>
                  {group.roster.length === 0 ? (
                    <p className="mt-3 text-sm text-muted">No members yet.</p>
                  ) : (
                    <ul className="mt-3 grid grid-cols-1 gap-2 sm:grid-cols-2">
                      {group.roster.map((m) => (
                        <li key={m.userId} className="flex items-center gap-3 text-sm">
                          {m.photoBlobUrl ? (
                            <picture>
                              {m.photoWebpBlobUrl && <source srcSet={m.photoWebpBlobUrl} type="image/webp" />}
                              <img
                                src={m.photoBlobUrl}
                                alt={m.photoAltText ?? ""}
                                className="h-9 w-9 object-cover"
                              />
                            </picture>
                          ) : (
                            <span aria-hidden className="grid h-9 w-9 place-items-center bg-panel-alt text-xs font-bold text-fg-soft">
                              {m.displayName.split(" ").map((p) => p[0] ?? "").slice(0, 2).join("")}
                            </span>
                          )}
                          <span className="flex-1 truncate">
                            <Link to={`/members/${m.userId}`} className="hover:underline">
                              {m.displayName}
                            </Link>
                          </span>
                          {m.isLeader && (
                            <span className="rounded bg-accent/15 px-2 py-0.5 text-[11px] font-medium uppercase tracking-wider text-accent">
                              Leader
                            </span>
                          )}
                        </li>
                      ))}
                    </ul>
                  )}
                </section>
              )}
            </article>
          )}

          {joinModalOpen && group && (
            <JoinRequestModal
              group={group}
              onClose={() => setJoinModalOpen(false)}
              onSubmitted={async () => {
                setJoinModalOpen(false);
                await refresh();
              }}
            />
          )}
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function JoinCta({
  group, isAuthenticated, onRequestJoin, onSignIn,
}: {
  group: PublicGroupDetail;
  isAuthenticated: boolean;
  onRequestJoin: () => void;
  onSignIn: () => void;
}) {
  if (!isAuthenticated) {
    return (
      <button
        type="button"
        onClick={onSignIn}
        className="mt-4 inline-flex h-10 items-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
      >
        Sign in to join
      </button>
    );
  }
  if (group.viewerIsMember) {
    return (
      <p className="mt-4 inline-flex h-10 items-center bg-success/15 px-4 text-sm font-medium text-success">
        You're a member
      </p>
    );
  }
  if (group.viewerHasPendingRequest) {
    return (
      <p className="mt-4 inline-flex h-10 items-center bg-warn/15 px-4 text-sm font-medium text-warn">
        Request pending
      </p>
    );
  }
  if (group.joinability === GroupJoinability.Closed) {
    return (
      <p className="mt-4 inline-flex h-10 items-center bg-panel-alt px-4 text-sm font-medium text-muted">
        Closed
      </p>
    );
  }
  if (group.joinability === GroupJoinability.InviteOnly) {
    return (
      <p className="mt-4 inline-flex h-10 items-center bg-panel-alt px-4 text-sm font-medium text-muted">
        Invite only
      </p>
    );
  }
  return (
    <button
      type="button"
      onClick={onRequestJoin}
      className="mt-4 inline-flex h-10 items-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
    >
      Request to join
    </button>
  );
}

function JoinRequestModal({
  group, onClose, onSubmitted,
}: {
  group: PublicGroupDetail;
  onClose: () => void;
  onSubmitted: () => void;
}) {
  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const messageRequired = group.requiresMessageOnJoinRequest === MessageOnJoinRequest.Required;
  const messageHidden = group.requiresMessageOnJoinRequest === MessageOnJoinRequest.Hidden;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true); setError(null);
    try {
      await publicGroupsApi.requestJoin(group.slug, {
        message: message.trim() || null,
      });
      onSubmitted();
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Could not submit request."];
      setError(messages.join("; "));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-40 flex items-center justify-center bg-foreground/40 p-4">
      <form onSubmit={handleSubmit} className="w-full max-w-md space-y-4 rounded-lg bg-background p-6 shadow-xl">
        <div className="flex items-start justify-between">
          <h2 className="text-lg font-semibold">Request to join {group.name}</h2>
          <button type="button" onClick={onClose} aria-label="Close" className="rounded-md border p-1.5">
            <X className="h-4 w-4" />
          </button>
        </div>

        {error && (
          <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
            {error}
          </div>
        )}

        {!messageHidden && (
          <div>
            <label htmlFor="join-message" className="mb-1 block text-sm font-medium">
              Message{messageRequired && <span className="text-danger"> *</span>}
            </label>
            <textarea
              id="join-message"
              required={messageRequired}
              value={message}
              maxLength={1000}
              onChange={(e) => setMessage(e.target.value)}
              className="min-h-24 w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              placeholder={
                messageRequired
                  ? "Tell us a bit about why you'd like to join."
                  : "Anything you'd like the leaders to know? (optional)"
              }
            />
          </div>
        )}

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={onClose}
            className="inline-flex h-10 items-center justify-center rounded-md border bg-background px-4 text-sm"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {submitting ? "Submitting…" : "Submit request"}
          </button>
        </div>
      </form>
    </div>
  );
}
