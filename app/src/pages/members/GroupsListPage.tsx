import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Lock, UsersRound, Check, Clock } from "lucide-react";
import {
  GroupJoinability,
  GroupMembershipStatus,
  GroupVisibility,
  publicGroupsApi,
  profileGroupsApi,
  type ProfileMembership,
  type PublicGroupListItem,
} from "@/lib/api/groups";
import {
  Content,
  EmptyState,
  InlineError,
  PageHead,
  SegTabs,
  SkeletonCard,
} from "@/components/members/portal-primitives";

type Tab = "all" | "mine";

export function GroupsListPage() {
  const [tab, setTab] = useState<Tab>("all");
  const [all, setAll] = useState<PublicGroupListItem[] | null>(null);
  const [mine, setMine] = useState<ProfileMembership[] | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setError(false);
    Promise.all([publicGroupsApi.list(), profileGroupsApi.listMine()])
      .then(([allList, mineList]) => {
        if (cancelled) return;
        setAll(allList);
        setMine(mineList);
      })
      .catch(() => { if (!cancelled) setError(true); });
    return () => { cancelled = true; };
  }, []);

  const myGroupIds = useMemo(
    () => new Set((mine ?? []).map((m) => m.groupId)),
    [mine],
  );

  return (
    <Content>
      <PageHead
        title="Groups"
        sub="Find a community to grow with. Joinable groups are clearly marked."
      />

      <SegTabs
        tabs={[
          { id: "all", label: "All groups" },
          { id: "mine", label: "My groups" },
        ]}
        active={tab}
        onChange={(id) => setTab(id as Tab)}
      />

      {error && <InlineError onRetry={() => location.reload()} />}

      {!error && (all === null || mine === null) && (
        <div className="space-y-3"><SkeletonCard /><SkeletonCard /></div>
      )}

      {tab === "all" && all && (
        <>
          {all.length === 0 ? (
            <EmptyState
              icon={<UsersRound strokeWidth={1.6} className="h-5 w-5" />}
              title="No groups yet"
              body="Groups will show up here as soon as your church publishes them."
            />
          ) : (
            <ul className="grid gap-3 sm:grid-cols-2">
              {all.map((g) => (
                <GroupListCard
                  key={g.id}
                  group={g}
                  viewerIsMember={myGroupIds.has(g.id)}
                  pendingMembership={
                    (mine ?? []).find((m) => m.groupId === g.id && m.status === GroupMembershipStatus.Pending)
                  }
                />
              ))}
            </ul>
          )}
        </>
      )}

      {tab === "mine" && mine && (
        <>
          {mine.length === 0 ? (
            <EmptyState
              icon={<UsersRound strokeWidth={1.6} className="h-5 w-5" />}
              title="You haven't joined any groups"
              body="Browse the All groups tab to find one that fits."
            />
          ) : (
            <ul className="space-y-3">
              {mine.map((m) => <MyGroupRow key={m.groupId} m={m} />)}
            </ul>
          )}
        </>
      )}
    </Content>
  );
}

function GroupListCard({
  group,
  viewerIsMember,
  pendingMembership,
}: {
  group: PublicGroupListItem;
  viewerIsMember: boolean;
  pendingMembership: ProfileMembership | undefined;
}) {
  const joinControl = (() => {
    if (viewerIsMember) return { label: "Joined", tone: "success" as const };
    if (pendingMembership) return { label: "Request pending", tone: "warn" as const };
    switch (group.joinability) {
      case GroupJoinability.Open: return { label: "Request to join", tone: "accent" as const };
      case GroupJoinability.InviteOnly: return { label: "Invite only", tone: "muted" as const };
      case GroupJoinability.Closed: return { label: "Closed", tone: "muted" as const };
      default: return { label: "Group", tone: "muted" as const };
    }
  })();

  const isMembersOnly = group.visibility === GroupVisibility.MembersOnly;

  return (
    <li>
      <Link
        to={`/members/groups/${encodeURIComponent(group.slug)}`}
        className="block border border-border bg-panel transition-colors hover:bg-panel-alt"
      >
        {group.imageBlobUrl && (
          <picture>
            {group.imageWebpBlobUrl && (
              <source srcSet={group.imageWebpBlobUrl} type="image/webp" />
            )}
            <img
              src={group.imageBlobUrl}
              alt={group.imageAltText ?? ""}
              className="aspect-[16/9] w-full object-cover"
            />
          </picture>
        )}
        <div className="p-4">
          <h3 className="truncate font-heading text-base font-semibold">{group.name}</h3>
          {group.meetingInfo && (
            <p className="mt-1 line-clamp-2 font-mono text-[10.5px] uppercase tracking-[0.10em] text-muted">
              {group.meetingInfo}
            </p>
          )}
          <div className="mt-3 flex flex-wrap items-center gap-2">
            <JoinChip label={joinControl.label} tone={joinControl.tone} />
            {isMembersOnly && (
              <span className="inline-flex items-center gap-1 border border-border-soft bg-panel-alt px-2 py-1 text-[10px] uppercase tracking-[0.12em] text-muted">
                <Lock strokeWidth={1.75} className="h-3 w-3" /> Members
              </span>
            )}
          </div>
        </div>
      </Link>
    </li>
  );
}

function MyGroupRow({ m }: { m: ProfileMembership }) {
  const isPending = m.status === GroupMembershipStatus.Pending;
  const requestedAt = m.requestedAt
    ? new Date(m.requestedAt).toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })
    : null;
  return (
    <li>
      <Link
        to={`/members/groups/${encodeURIComponent(m.groupSlug)}`}
        className="flex items-center gap-3 border border-border bg-panel p-3 hover:bg-panel-alt"
      >
        <span
          aria-hidden="true"
          className="flex h-9 w-9 items-center justify-center bg-panel-alt text-muted"
        >
          <UsersRound strokeWidth={1.5} className="h-4 w-4" />
        </span>
        <div className="min-w-0 flex-1">
          <p className="truncate font-heading text-sm font-semibold">{m.groupName}</p>
          {isPending && (
            <p className="mt-1 inline-flex items-center gap-1 font-mono text-[10px] uppercase tracking-[0.12em] text-warn">
              <Clock strokeWidth={1.75} className="h-3 w-3" />
              Requested {requestedAt}
            </p>
          )}
          {!isPending && m.isLeader && (
            <p className="mt-1 font-mono text-[10px] uppercase tracking-[0.12em] text-accent">
              Leader
            </p>
          )}
        </div>
        {!isPending && (
          <Check strokeWidth={1.75} className="h-4 w-4 text-success" />
        )}
      </Link>
    </li>
  );
}

function JoinChip({ label, tone }: { label: string; tone: "success" | "warn" | "accent" | "muted" }) {
  const cls = {
    success: "border-success/40 bg-success/10 text-success",
    warn: "border-warn/40 bg-warn/10 text-warn",
    accent: "border-accent/40 bg-accent/10 text-accent",
    muted: "border-border-soft bg-panel-alt text-muted",
  }[tone];
  return (
    <span className={`inline-flex items-center border px-2 py-1 text-[10px] uppercase tracking-[0.12em] font-semibold ${cls}`}>
      {label}
    </span>
  );
}
