import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Lock, Users, ArrowRight } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { useAuth } from "@/hooks/useAuth";
import {
  publicGroupsApi,
  GroupJoinability,
  GroupVisibility,
  type PublicGroupListItem,
} from "@/lib/api/groups";

/**
 * Public-facing groups landing page. Visibility is enforced by the API:
 * anonymous callers only see Public groups, authenticated members see
 * Public + MembersOnly. Hidden groups never appear here.
 */
export function GetInvolvedPage() {
  const { isAuthenticated } = useAuth();
  const [groups, setGroups] = useState<PublicGroupListItem[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    publicGroupsApi
      .list()
      .then((d) => { setGroups(d); setError(null); })
      .catch(() => setError("Could not load groups."))
      .finally(() => setLoading(false));
  }, [isAuthenticated]); // re-fetch on sign-in/out so MembersOnly groups appear

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-10">
          <header className="border-b pb-6">
            <h1 className="text-3xl font-bold">Get involved</h1>
            <p className="mt-2 text-muted">
              Find a group, ministry, or community that fits where you are.
              {!isAuthenticated && " Sign in to see members-only groups."}
            </p>
          </header>

          <section className="mt-6">
            {loading && <p className="text-muted">Loading groups…</p>}
            {error && <p className="text-danger">{error}</p>}
            {groups && groups.length === 0 && (
              <p className="text-muted">No groups available right now.</p>
            )}
            {groups && groups.length > 0 && (
              <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {groups.map((g) => <GroupCard key={g.id} group={g} isAuthenticated={isAuthenticated} />)}
              </ul>
            )}
          </section>
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function GroupCard({
  group, isAuthenticated,
}: { group: PublicGroupListItem; isAuthenticated: boolean }) {
  const ctaLabel = !isAuthenticated
    ? "Sign in to join"
    : group.joinability === GroupJoinability.Closed ? "Closed"
    : group.joinability === GroupJoinability.InviteOnly ? "Invite only"
    : "Request to Join";

  const ctaHref = !isAuthenticated
    ? `/login?return=${encodeURIComponent(`/groups/${group.slug}`)}`
    : `/groups/${group.slug}`;

  return (
    <li>
      <Link
        to={ctaHref}
        className="flex h-full flex-col overflow-hidden rounded-lg border bg-card transition-colors hover:bg-panel-alt"
      >
        {group.imageBlobUrl ? (
          <picture>
            {group.imageWebpBlobUrl && <source srcSet={group.imageWebpBlobUrl} type="image/webp" />}
            <img
              src={group.imageBlobUrl}
              alt={group.imageAltText ?? ""}
              className="h-44 w-full object-cover"
            />
          </picture>
        ) : (
          <div
            aria-hidden
            className="grid h-44 w-full place-items-center bg-panel-alt text-muted"
          >
            <Users className="h-10 w-10" />
          </div>
        )}
        <div className="flex flex-1 flex-col p-4">
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="font-heading text-lg font-semibold">{group.name}</h2>
            {group.visibility === GroupVisibility.MembersOnly && (
              <span className="inline-flex items-center gap-1 rounded bg-panel-alt px-2 py-0.5 text-[11px] font-medium uppercase tracking-wider text-muted">
                <Lock className="h-3 w-3" /> Members
              </span>
            )}
          </div>
          {group.meetingInfo && (
            <p className="mt-2 text-xs text-muted">{group.meetingInfo}</p>
          )}
          <p className="mt-auto inline-flex items-center gap-1.5 pt-3 text-sm font-medium text-primary">
            {ctaLabel}
            <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
          </p>
        </div>
      </Link>
    </li>
  );
}
