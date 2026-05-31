import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Search, Mail, Phone, ChevronRight, Users } from "lucide-react";
import { membersApi, type MemberListItem } from "@/lib/api/members";
import type { PagedResult } from "@/types/api";
import {
  Avatar,
  Content,
  EmptyState,
  InlineError,
  PageHead,
  Panel,
  SkeletonCard,
} from "@/components/members/portal-primitives";

const PAGE_SIZE = 24;

/**
 * Directory list — opt-in members only (server enforces it). Cards show
 * contact affordances ONLY when the field is shared; absence is the
 * privacy state. No role chip per resolution.
 *
 * "By household" toggle is intentionally NOT rendered in v1 — needs a
 * backend Household entity. The List view alone ships now.
 */
export function DirectoryListPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const query = searchParams.get("q") ?? "";
  const [searchInput, setSearchInput] = useState(query);
  const [data, setData] = useState<PagedResult<MemberListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => { setSearchInput(query); }, [query]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(false);
    membersApi.list({ search: query || undefined, page: 1, pageSize: PAGE_SIZE })
      .then((d) => { if (!cancelled) setData(d); })
      .catch(() => { if (!cancelled) setError(true); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [query]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const next = new URLSearchParams(searchParams);
    const v = searchInput.trim();
    if (v) next.set("q", v); else next.delete("q");
    setSearchParams(next);
  };

  return (
    <Content>
      <PageHead
        title="Directory"
        count={data ? `${data.totalCount} ${data.totalCount === 1 ? "member" : "members"}` : undefined}
        sub="Other signed-in members who've chosen to be listed. You control what you share in your Profile."
      />

      {/* Search */}
      <form onSubmit={handleSearch} role="search" className="mb-5">
        <div className="relative">
          <Search
            aria-hidden="true"
            strokeWidth={1.75}
            className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted"
          />
          <input
            type="search"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search by name…"
            aria-label="Search directory"
            className="h-9 w-full border border-border bg-panel pl-9 pr-3 text-sm focus-visible:border-accent focus-visible:outline-none"
          />
        </div>
      </form>

      {error && <InlineError onRetry={() => setSearchParams(new URLSearchParams(searchParams))} />}

      {loading && (
        <div className="grid gap-3 sm:grid-cols-2">
          <SkeletonCard /><SkeletonCard /><SkeletonCard /><SkeletonCard />
        </div>
      )}

      {!loading && !error && data && data.items.length === 0 && (
        <EmptyState
          icon={<Users strokeWidth={1.6} className="h-5 w-5" />}
          title={query ? "No matches" : "Nobody's joined the directory yet"}
          body={
            query
              ? "Try a different name."
              : "Once members opt in via their Profile, they'll show up here. Be the first by turning on directory listing in My Profile."
          }
          action={
            !query && (
              <Link
                to="/members/profile"
                className="inline-flex items-center gap-2 border border-border bg-panel px-4 py-2 text-sm font-medium hover:bg-panel-alt"
              >
                Open My Profile
              </Link>
            )
          }
        />
      )}

      {!loading && data && data.items.length > 0 && (
        <ul className="grid gap-3 sm:grid-cols-2">
          {data.items.map((m) => <MemberCard key={m.id} member={m} />)}
        </ul>
      )}
    </Content>
  );
}

function MemberCard({ member }: { member: MemberListItem }) {
  return (
    <li>
      <Link
        to={`/members/directory/${member.id}`}
        className="flex w-full items-center gap-3 border border-border bg-panel p-3 transition-colors hover:bg-panel-alt"
      >
        <Avatar
          name={member.displayName}
          size={44}
          src={member.photoBlobUrl}
          webpSrc={member.photoWebpBlobUrl}
          alt={member.photoAltText}
        />
        <div className="min-w-0 flex-1">
          <p className="truncate font-heading text-sm font-semibold">{member.displayName}</p>
          {(member.email || member.phoneNumber) ? (
            <p className="mt-1 flex flex-wrap items-center gap-2 font-mono text-[10.5px] uppercase tracking-[0.10em] text-muted">
              {member.email && (
                <a
                  href={`mailto:${member.email}`}
                  onClick={(e) => e.stopPropagation()}
                  className="inline-flex items-center gap-1 hover:text-foreground"
                >
                  <Mail strokeWidth={1.75} className="h-3 w-3" /> Email
                </a>
              )}
              {member.phoneNumber && (
                <a
                  href={`tel:${member.phoneNumber}`}
                  onClick={(e) => e.stopPropagation()}
                  className="inline-flex items-center gap-1 hover:text-foreground"
                >
                  <Phone strokeWidth={1.75} className="h-3 w-3" /> Call
                </a>
              )}
            </p>
          ) : (
            <p className="mt-1 text-[11px] text-muted">Member</p>
          )}
        </div>
        <ChevronRight strokeWidth={1.5} className="h-4 w-4 shrink-0 text-border" />
      </Link>
    </li>
  );
}
