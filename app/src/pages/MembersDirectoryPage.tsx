import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Mail, Phone, ChevronLeft, ChevronRight } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { membersApi, type MemberListItem } from "@/lib/api/members";
import type { PagedResult } from "@/types/api";

const PAGE_SIZE = 24;

export function MembersDirectoryPage() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PagedResult<MemberListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    membersApi
      .list({ search: search || undefined, page, pageSize: PAGE_SIZE })
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load members."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [search, page]);

  // Reset to page 1 whenever the search changes so the user doesn't end up on
  // page 5 of a 1-page result.
  useEffect(() => { setPage(1); }, [search]);

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-10">
          <header className="flex flex-wrap items-end justify-between gap-3 border-b pb-6">
            <div>
              <h1 className="text-2xl font-bold">Members directory</h1>
              <p className="mt-1 text-sm text-muted">
                Members who chose to be listed appear below. You can update your own
                listing on your{" "}
                <Link to="/profile" className="text-accent hover:underline">profile</Link>.
              </p>
            </div>
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by name…"
              className="h-10 w-full max-w-xs rounded-md border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              aria-label="Search members"
            />
          </header>

          <section className="mt-6">
            {loading && <p className="text-muted">Loading…</p>}
            {!loading && error && <p className="text-danger">{error}</p>}
            {!loading && !error && data && data.items.length === 0 && (
              <p className="text-muted">No members found.</p>
            )}
            {!loading && !error && data && data.items.length > 0 && (
              <ul className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {data.items.map((m) => <MemberCard key={m.id} member={m} />)}
              </ul>
            )}

            {data && data.totalPages > 1 && (
              <Pagination
                page={page}
                totalPages={data.totalPages}
                onPrev={() => setPage((p) => Math.max(1, p - 1))}
                onNext={() => setPage((p) => Math.min(data.totalPages, p + 1))}
              />
            )}
          </section>
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function MemberCard({ member }: { member: MemberListItem }) {
  return (
    <li>
      <Link
        to={`/members/${member.id}`}
        className="flex h-full items-center gap-4 rounded-lg border bg-card p-4 transition-colors hover:bg-panel-alt"
      >
        {member.photoBlobUrl ? (
          <picture>
            {member.photoWebpBlobUrl && (
              <source srcSet={member.photoWebpBlobUrl} type="image/webp" />
            )}
            <img
              src={member.photoBlobUrl}
              alt={member.photoAltText ?? ""}
              className="h-14 w-14 shrink-0 object-cover"
            />
          </picture>
        ) : (
          <span
            aria-hidden
            className="grid h-14 w-14 shrink-0 place-items-center bg-panel-alt text-base font-bold text-fg-soft"
          >
            {initials(member.firstName, member.lastName)}
          </span>
        )}
        <div className="min-w-0 flex-1">
          <p className="truncate font-semibold">{member.displayName}</p>
          {member.email && (
            <p className="mt-1 flex items-center gap-1 truncate text-xs text-muted">
              <Mail className="h-3 w-3 shrink-0" /> {member.email}
            </p>
          )}
          {member.phoneNumber && (
            <p className="mt-0.5 flex items-center gap-1 truncate text-xs text-muted">
              <Phone className="h-3 w-3 shrink-0" /> {member.phoneNumber}
            </p>
          )}
        </div>
      </Link>
    </li>
  );
}

function Pagination({
  page, totalPages, onPrev, onNext,
}: {
  page: number;
  totalPages: number;
  onPrev: () => void;
  onNext: () => void;
}) {
  return (
    <nav className="mt-6 flex items-center justify-end gap-2 text-sm" aria-label="Pagination">
      <button
        type="button"
        onClick={onPrev}
        disabled={page === 1}
        className="inline-flex h-9 items-center gap-1 border bg-card px-3 hover:bg-panel-alt disabled:opacity-50"
      >
        <ChevronLeft className="h-4 w-4" /> Prev
      </button>
      <span className="text-muted" style={{ fontVariantNumeric: "tabular-nums" }}>
        Page {page} of {totalPages}
      </span>
      <button
        type="button"
        onClick={onNext}
        disabled={page === totalPages}
        className="inline-flex h-9 items-center gap-1 border bg-card px-3 hover:bg-panel-alt disabled:opacity-50"
      >
        Next <ChevronRight className="h-4 w-4" />
      </button>
    </nav>
  );
}

function initials(first: string, last: string): string {
  return `${first.slice(0, 1)}${last.slice(0, 1)}`.toUpperCase();
}
