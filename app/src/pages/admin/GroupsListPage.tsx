import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import {
  adminGroupsApi,
  GroupJoinability,
  GroupVisibility,
  type AdminGroupListItem,
} from "@/lib/api/groups";
import {
  Btn,
  Chip,
  FilterPills,
  PageHeader,
} from "@/components/shared/admin/EditorialPrimitives";

type Tab = "active" | "inactive" | "all";

export function GroupsListPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>("active");
  const [search, setSearch] = useState("");
  const [data, setData] = useState<AdminGroupListItem[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    adminGroupsApi
      .list(search || undefined, true)
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load groups."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [search]);

  const filtered = useMemo(() => {
    if (!data) return [];
    if (tab === "active") return data.filter((g) => g.isActive);
    if (tab === "inactive") return data.filter((g) => !g.isActive);
    return data;
  }, [data, tab]);

  const counts = useMemo(() => ({
    active: data?.filter((g) => g.isActive).length ?? 0,
    inactive: data?.filter((g) => !g.isActive).length ?? 0,
    all: data?.length ?? 0,
  }), [data]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={`${counts.all} groups`}
        title="Groups"
        kicker="ministries, classes, and teams"
        actions={
          <Btn variant="accent" size="lg" onClick={() => navigate("/admin/groups/new")}>
            New group
          </Btn>
        }
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <FilterPills
          activeValue={tab}
          onChange={(v) => setTab(v as Tab)}
          items={[
            { value: "active", label: "Active", count: counts.active },
            { value: "inactive", label: "Inactive", count: counts.inactive },
            { value: "all", label: "All", count: counts.all },
          ]}
        />
        <input
          type="search"
          placeholder="Search name or slug…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-9 w-full max-w-xs border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
        />
      </div>

      {loading && <p className="text-muted">Loading…</p>}
      {error && <p className="text-danger">{error}</p>}
      {!loading && !error && filtered.length === 0 && <p className="text-muted">No groups match.</p>}

      {!loading && !error && filtered.length > 0 && (
        <article className="border border-border bg-panel">
          <header
            className="grid items-center gap-4 border-b border-border bg-panel-alt px-5 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted"
            style={{ gridTemplateColumns: "3fr 1fr 1fr 1fr 1fr" }}
          >
            <span>Group</span>
            <span>Visibility</span>
            <span>Members</span>
            <span>Pending</span>
            <span className="text-right">Actions</span>
          </header>
          <ul className="divide-y divide-border-soft">
            {filtered.map((g) => (
              <li
                key={g.id}
                className="grid items-center gap-4 px-5 py-3"
                style={{ gridTemplateColumns: "3fr 1fr 1fr 1fr 1fr" }}
              >
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => navigate(`/admin/groups/${g.id}`)}
                      className="text-left font-heading text-base font-semibold hover:underline"
                    >
                      {g.name}
                    </button>
                    {!g.isActive && <Chip tone="warn" dot>Inactive</Chip>}
                  </div>
                  <p className="mt-1 truncate font-mono text-xs text-muted">/{g.slug}</p>
                </div>
                <div>
                  <Chip tone={visibilityTone(g.visibility)} dot>
                    {visibilityLabel(g.visibility)}
                  </Chip>
                  {g.joinability !== GroupJoinability.Open && (
                    <p className="mt-1 text-[11px] text-muted">{joinabilityLabel(g.joinability)}</p>
                  )}
                </div>
                <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono text-sm">
                  {g.activeMemberCount}
                </span>
                <span style={{ fontVariantNumeric: "tabular-nums" }}
                  className={"font-mono text-sm " + (g.pendingRequestCount > 0 ? "text-warn" : "text-muted")}>
                  {g.pendingRequestCount}
                </span>
                <div className="flex justify-end">
                  <Btn
                    size="sm"
                    iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                    onClick={() => navigate(`/admin/groups/${g.id}`)}
                  >
                    Edit
                  </Btn>
                </div>
              </li>
            ))}
          </ul>
        </article>
      )}
    </div>
  );
}

function visibilityTone(v: GroupVisibility) {
  if (v === GroupVisibility.Public) return "success" as const;
  if (v === GroupVisibility.MembersOnly) return "accent" as const;
  return "muted" as const;
}

function visibilityLabel(v: GroupVisibility) {
  if (v === GroupVisibility.Public) return "Public";
  if (v === GroupVisibility.MembersOnly) return "Members";
  return "Hidden";
}

function joinabilityLabel(j: GroupJoinability) {
  if (j === GroupJoinability.InviteOnly) return "Invite only";
  if (j === GroupJoinability.Closed) return "Closed";
  return "Open";
}
