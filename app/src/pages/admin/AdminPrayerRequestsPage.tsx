import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import {
  adminPrayerApi,
  PrayerRequestStatus,
  type AdminPrayerRequest,
} from "@/lib/api/prayerRequests";
import { usePrayerRequestUpdates } from "@/hooks/usePrayerRequestUpdates";
import {
  Btn,
  Chip,
  FilterPills,
  PageHeader,
} from "@/components/shared/admin/EditorialPrimitives";

type Tab = "active" | "answered" | "archived" | "all";

export function AdminPrayerRequestsPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>("active");
  const [search, setSearch] = useState("");
  const [data, setData] = useState<AdminPrayerRequest[] | null>(null);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const status = tab === "all" ? undefined
        : tab === "active" ? PrayerRequestStatus.Active
        : tab === "answered" ? PrayerRequestStatus.Answered
        : PrayerRequestStatus.Archived;
      const rows = await adminPrayerApi.list({
        status,
        search: search || undefined,
      });
      setData(rows);
      setSelected((prev) => new Set([...prev].filter((id) => rows.some((r) => r.id === id))));
      setError(null);
    } catch {
      setError("Could not load prayer requests.");
    } finally {
      setLoading(false);
    }
  }, [tab, search]);

  useEffect(() => { void load(); }, [load]);

  // Real-time refresh — admin moderation view should reflect new submissions
  // and prayed-for counts without manual reload.
  usePrayerRequestUpdates(useCallback(() => { void load(); }, [load]));

  const counts = useMemo(() => ({
    all: data?.length ?? 0,
  }), [data]);

  const toggleSelect = (id: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleBulkArchive = async () => {
    if (selected.size === 0) return;
    if (!window.confirm(`Archive ${selected.size} request${selected.size === 1 ? "" : "s"}?`)) return;
    try {
      await adminPrayerApi.bulkArchive([...selected]);
      setSelected(new Set());
      await load();
    } catch (err) {
      const messages = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Could not archive."];
      window.alert(messages.join("; "));
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={`${counts.all} requests`}
        title="Prayer requests"
        kicker="moderation, status, pastoral updates"
        actions={
          selected.size > 0 ? (
            <Btn variant="accent" onClick={handleBulkArchive}>
              Archive {selected.size} selected
            </Btn>
          ) : null
        }
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <FilterPills
          activeValue={tab}
          onChange={(v) => setTab(v as Tab)}
          items={[
            { value: "active", label: "Active" },
            { value: "answered", label: "Answered" },
            { value: "archived", label: "Archived" },
            { value: "all", label: "All" },
          ]}
        />
        <input
          type="search"
          placeholder="Search title…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-9 w-full max-w-xs border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
        />
      </div>

      {loading && <p className="text-muted">Loading…</p>}
      {error && <p className="text-danger">{error}</p>}
      {!loading && !error && data && data.length === 0 && (
        <p className="text-muted">No requests match.</p>
      )}

      {!loading && !error && data && data.length > 0 && (
        <article className="border border-border bg-panel">
          <header
            className="grid items-center gap-3 border-b border-border bg-panel-alt px-5 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted"
            style={{ gridTemplateColumns: "32px 3fr 1.4fr 1fr 0.7fr 1fr" }}
          >
            <span aria-label="Select" />
            <span>Request</span>
            <span>Submitter</span>
            <span>Status</span>
            <span>Prayed</span>
            <span className="text-right">Actions</span>
          </header>
          <ul className="divide-y divide-border-soft">
            {data.map((p) => (
              <li
                key={p.id}
                className="grid items-center gap-3 px-5 py-3"
                style={{ gridTemplateColumns: "32px 3fr 1.4fr 1fr 0.7fr 1fr" }}
              >
                <input
                  type="checkbox"
                  checked={selected.has(p.id)}
                  onChange={() => toggleSelect(p.id)}
                  aria-label={`Select ${p.title}`}
                />
                <div className="min-w-0">
                  <button
                    type="button"
                    onClick={() => navigate(`/prayer-requests/${p.id}`)}
                    className="text-left font-heading text-base font-semibold hover:underline"
                  >
                    {p.title}
                  </button>
                  <p className="font-mono text-xs text-muted">
                    {new Date(p.createdAt).toLocaleString()}
                  </p>
                </div>
                <div className="text-sm">
                  <span>{p.submitterDisplayName}</span>
                  {p.isAnonymous && (
                    <Chip tone="warn" className="ml-2">Anon</Chip>
                  )}
                </div>
                <div>
                  {p.status === PrayerRequestStatus.Active && <Chip tone="accent" dot>Active</Chip>}
                  {p.status === PrayerRequestStatus.Answered && <Chip tone="success" dot>Answered</Chip>}
                  {p.status === PrayerRequestStatus.Archived && <Chip tone="muted">Archived</Chip>}
                </div>
                <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono text-sm">
                  {p.prayedForCount}
                </span>
                <div className="flex justify-end">
                  <Btn
                    size="sm"
                    iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                    onClick={() => navigate(`/prayer-requests/${p.id}`)}
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
