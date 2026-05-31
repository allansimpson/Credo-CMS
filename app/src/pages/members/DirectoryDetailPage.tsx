import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Mail, Phone, MapPin, UsersRound, Crown } from "lucide-react";
import { membersApi, type MemberDetail } from "@/lib/api/members";
import {
  Avatar,
  Content,
  EmptyState,
  InlineError,
  MetaRow,
  PageHead,
  Panel,
  Skeleton,
} from "@/components/members/portal-primitives";

export function DirectoryDetailPage() {
  const { userId } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const [member, setMember] = useState<MemberDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<"notfound" | "load" | null>(null);

  useEffect(() => {
    if (!userId) return;
    let cancelled = false;
    setLoading(true);
    setError(null);
    membersApi.get(userId)
      .then((m) => { if (!cancelled) setMember(m); })
      .catch((err: { status?: number }) => {
        if (!cancelled) setError(err?.status === 404 ? "notfound" : "load");
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [userId]);

  if (loading) {
    return (
      <Content>
        <PageHead title="" onBack={() => navigate("/members/directory")} />
        <Skeleton className="mb-4 h-24 w-24" />
        <Skeleton className="mb-2 h-5 w-1/3" />
        <Skeleton className="h-3 w-1/2" />
      </Content>
    );
  }

  if (error === "notfound" || !member) {
    return (
      <Content>
        <PageHead title="Not found" onBack={() => navigate("/members/directory")} />
        <EmptyState
          title="This member isn't in the directory"
          body="They may not be listed, or the link may be stale."
        />
      </Content>
    );
  }

  if (error === "load") {
    return (
      <Content>
        <PageHead title="" onBack={() => navigate("/members/directory")} />
        <InlineError onRetry={() => location.reload()} />
      </Content>
    );
  }

  const hasContact = member.email || member.phoneNumber || member.addressLine1;
  const cityState = [member.city, member.stateOrRegion].filter(Boolean).join(", ");
  const addressLines = [
    member.addressLine1,
    member.addressLine2,
    [cityState, member.postalCode].filter(Boolean).join(" ").trim(),
    member.country,
  ].filter((line): line is string => !!line && line.length > 0);

  // Bio is already plain text — the directory service strips ProseMirror
  // server-side via ProseMirrorText.Excerpt before the DTO crosses the
  // boundary. Render as-is.
  const bio = member.publicAuthorBio;

  return (
    <Content>
      <PageHead title={member.displayName} onBack={() => navigate("/members/directory")} />

      {/* Identity panel */}
      <Panel className="mb-5 flex flex-col items-start gap-5 sm:flex-row">
        <Avatar
          name={member.displayName}
          size={88}
          src={member.photoBlobUrl}
          webpSrc={member.photoWebpBlobUrl}
          alt={member.photoAltText}
        />
        <div className="min-w-0 flex-1">
          <h2 className="font-heading text-2xl font-semibold tracking-tight">
            {member.displayName}
          </h2>
          {bio && <p className="mt-3 max-w-prose text-sm leading-relaxed text-fg-soft">{bio}</p>}
        </div>
      </Panel>

      {/* Contact */}
      <section className="mb-5">
        <h3 className="mb-3 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
          Contact
        </h3>
        <Panel noPad className="px-4">
          {hasContact ? (
            <>
              {member.email && (
                <MetaRow
                  icon={<Mail strokeWidth={1.5} className="h-4 w-4" />}
                  label="Email"
                  value={
                    <a href={`mailto:${member.email}`} className="text-accent hover:underline">
                      {member.email}
                    </a>
                  }
                />
              )}
              {member.phoneNumber && (
                <MetaRow
                  icon={<Phone strokeWidth={1.5} className="h-4 w-4" />}
                  label="Phone"
                  value={
                    <a href={`tel:${member.phoneNumber}`} className="text-accent hover:underline">
                      {member.phoneNumber}
                    </a>
                  }
                />
              )}
              {addressLines.length > 0 && (
                <MetaRow
                  icon={<MapPin strokeWidth={1.5} className="h-4 w-4" />}
                  label="Address"
                  value={
                    <span className="block whitespace-pre-line">
                      {addressLines.join("\n")}
                    </span>
                  }
                />
              )}
            </>
          ) : (
            <p className="py-4 text-sm text-muted">
              {member.displayName.split(" ")[0]} hasn't shared contact info.
            </p>
          )}
        </Panel>
      </section>

      {/* Groups */}
      {member.groupMemberships.length > 0 && (
        <section>
          <h3 className="mb-3 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
            Groups
          </h3>
          <ul className="flex flex-wrap gap-2">
            {member.groupMemberships.map((g) => (
              <li key={g.groupId}>
                <Link
                  to={`/members/groups/${encodeURIComponent(g.groupSlug)}`}
                  className="inline-flex items-center gap-2 border border-border bg-panel px-3 py-1.5 text-xs font-medium hover:bg-panel-alt"
                >
                  <UsersRound strokeWidth={1.5} className="h-3.5 w-3.5 text-muted" />
                  {g.groupName}
                  {g.isLeader && (
                    <span className="inline-flex items-center gap-1 text-[10px] uppercase tracking-[0.12em] text-accent">
                      <Crown strokeWidth={1.75} className="h-3 w-3" /> Leader
                    </span>
                  )}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}
    </Content>
  );
}

