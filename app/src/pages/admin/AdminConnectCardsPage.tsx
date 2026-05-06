import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import {
  adminConnectCardApi,
  ConnectCardStatus,
  type AdminConnectCardListItem,
} from "@/lib/api/connectCard";
import {
  Btn,
  Chip,
  FilterPills,
  PageHeader,
} from "@/components/shared/admin/EditorialPrimitives";

type Tab = "new" | "follow-up" | "followed-up" | "closed" | "all";

export function AdminConnectCardsPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>("new");
  const [search, setSearch] = useState("");
  const [data, setData] = useState<AdminConnectCardListItem[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const status = tab === "all" ? undefined
        : tab === "new" ? ConnectCardStatus.New
        : tab === "follow-up" ? ConnectCardStatus.FollowUpNeeded
        : tab === "followed-up" ? ConnectCardStatus.FollowedUp
        : ConnectCardStatus.Closed;
      const rows = await adminConnectCardApi.list({
        status,
        search: search || undefined,
      });
      setData(rows);
      setError(null);
    } catch {
      setError("Could not load connect cards.");
    } finally {
      setLoading(false);
    }
  }, [tab, search]);

  useEffect(() => { void load(); }, [load]);

  const counts = useMemo(() => ({ all: data?.length ?? 0 }), [data]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={`${counts.all} cards`}
        title="Connect cards"
        kicker="visitor follow-up queue"
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <FilterPills
          activeValue={tab}
          onChange={(v) => setTab(v as Tab)}
          items={[
            { value: "new", label: "New" },
            { value: "follow-up", label: "Follow up" },
            { value: "followed-up", label: "Followed up" },
            { value: "closed", label: "Closed" },
            { value: "all", label: "All" },
          ]}
        />
        <input
          type="search"
          placeholder="Search name or email…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-9 w-full max-w-xs border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
        />
      </div>

      {loading && <p className="text-muted">Loading…</p>}
      {error && <p className="text-danger">{error}</p>}
      {!loading && !error && data && data.length === 0 && (
        <p className="text-muted">No cards match.</p>
      )}

      {!loading && !error && data && data.length > 0 && (
        <article className="border border-border bg-panel">
          <header
            className="grid items-center gap-3 border-b border-border bg-panel-alt px-5 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted"
            style={{ gridTemplateColumns: "2fr 1.5fr 1fr 1.4fr 1fr" }}
          >
            <span>Name</span>
            <span>Contact</span>
            <span>Status</span>
            <span>Submitted</span>
            <span className="text-right">Actions</span>
          </header>
          <ul className="divide-y divide-border-soft">
            {data.map((c) => (
              <li
                key={c.id}
                className="grid items-center gap-3 px-5 py-3"
                style={{ gridTemplateColumns: "2fr 1.5fr 1fr 1.4fr 1fr" }}
              >
                <div className="min-w-0">
                  <button
                    type="button"
                    onClick={() => navigate(`/admin/connect-cards/${c.id}`)}
                    className="text-left font-heading text-base font-semibold hover:underline"
                  >
                    {c.name}
                  </button>
                  {c.isFirstTimeVisitor && (
                    <Chip tone="accent" className="mt-1">First-time</Chip>
                  )}
                </div>
                <div className="min-w-0 truncate text-xs">
                  {c.email && <p className="truncate font-mono">{c.email}</p>}
                  {c.phone && <p className="truncate font-mono text-muted">{c.phone}</p>}
                </div>
                <div>{statusChip(c.status)}</div>
                <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono text-xs">
                  {new Date(c.submittedAt).toLocaleString()}
                </span>
                <div className="flex justify-end">
                  <Btn
                    size="sm"
                    iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                    onClick={() => navigate(`/admin/connect-cards/${c.id}`)}
                  >
                    Open
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

function statusChip(status: ConnectCardStatus) {
  if (status === ConnectCardStatus.New) return <Chip tone="accent" dot>New</Chip>;
  if (status === ConnectCardStatus.FollowUpNeeded) return <Chip tone="warn" dot>Follow up</Chip>;
  if (status === ConnectCardStatus.FollowedUp) return <Chip tone="success" dot>Followed up</Chip>;
  if (status === ConnectCardStatus.Closed) return <Chip tone="muted">Closed</Chip>;
  return <Chip tone="danger">Not legit</Chip>;
}
