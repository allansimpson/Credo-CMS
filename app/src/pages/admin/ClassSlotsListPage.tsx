import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import {
  adminClassSlotsApi,
  type AdminClassSlotListItem,
} from "@/lib/api/classes";
import {
  Btn,
  Chip,
  FilterPills,
  PageHeader,
} from "@/components/shared/admin/EditorialPrimitives";

type Tab = "active" | "inactive" | "all";

export function ClassSlotsListPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>("active");
  const [search, setSearch] = useState("");
  const [data, setData] = useState<AdminClassSlotListItem[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    adminClassSlotsApi
      .list(search || undefined, true)
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load class slots."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [search]);

  const filtered = useMemo(() => {
    if (!data) return [];
    if (tab === "active") return data.filter((s) => s.isActive);
    if (tab === "inactive") return data.filter((s) => !s.isActive);
    return data;
  }, [data, tab]);

  const counts = useMemo(() => ({
    active: data?.filter((s) => s.isActive).length ?? 0,
    inactive: data?.filter((s) => !s.isActive).length ?? 0,
    all: data?.length ?? 0,
  }), [data]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={`${counts.all} slots`}
        title="Class slots"
        kicker="persistent class containers — fill with offerings"
        actions={
          <>
            <Btn onClick={() => navigate("/admin/class-offerings")}>Offerings</Btn>
            <Btn variant="accent" size="lg" onClick={() => navigate("/admin/class-slots/new")}>
              New slot
            </Btn>
          </>
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
          placeholder="Search name, slug, or audience…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-9 w-full max-w-xs border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
        />
      </div>

      {loading && <p className="text-muted">Loading…</p>}
      {error && <p className="text-danger">{error}</p>}
      {!loading && !error && filtered.length === 0 && <p className="text-muted">No slots match.</p>}

      {!loading && !error && filtered.length > 0 && (
        <article className="border border-border bg-panel">
          <header
            className="grid items-center gap-4 border-b border-border bg-panel-alt px-5 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted"
            style={{ gridTemplateColumns: "3fr 1.4fr 1fr 1fr 1fr" }}
          >
            <span>Slot</span>
            <span>Audience</span>
            <span>Order</span>
            <span>Offerings</span>
            <span className="text-right">Actions</span>
          </header>
          <ul className="divide-y divide-border-soft">
            {filtered.map((s) => (
              <li
                key={s.id}
                className="grid items-center gap-4 px-5 py-3"
                style={{ gridTemplateColumns: "3fr 1.4fr 1fr 1fr 1fr" }}
              >
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => navigate(`/admin/class-slots/${s.id}`)}
                      className="text-left font-heading text-base font-semibold hover:underline"
                    >
                      {s.name}
                    </button>
                    {!s.isActive && <Chip tone="warn" dot>Inactive</Chip>}
                  </div>
                  <p className="mt-1 truncate font-mono text-xs text-muted">/{s.slug}</p>
                </div>
                <span className="text-sm">{s.audienceAgeGroup}</span>
                <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono text-sm">
                  {s.displayOrder}
                </span>
                <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono text-sm">
                  {s.offeringCount}
                </span>
                <div className="flex justify-end">
                  <Btn
                    size="sm"
                    iconRight={<ArrowRight className="h-3.5 w-3.5" />}
                    onClick={() => navigate(`/admin/class-slots/${s.id}`)}
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
