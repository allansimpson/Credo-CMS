import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import {
  adminClassOfferingsApi,
  adminClassSlotsApi,
  OfferingStatusFilter,
  type AdminClassOffering,
  type AdminClassSlotListItem,
} from "@/lib/api/classes";
import {
  Btn,
  Chip,
  FilterPills,
  PageHeader,
} from "@/components/shared/admin/EditorialPrimitives";

export function ClassOfferingsListPage() {
  const navigate = useNavigate();
  const [status, setStatus] = useState<OfferingStatusFilter>(OfferingStatusFilter.All);
  const [slotId, setSlotId] = useState<string>("");
  const [data, setData] = useState<AdminClassOffering[] | null>(null);
  const [slots, setSlots] = useState<AdminClassSlotListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminClassSlotsApi.list(undefined, true).then(setSlots).catch(() => setSlots([]));
  }, []);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    adminClassOfferingsApi
      .list({
        status,
        classSlotId: slotId || undefined,
      })
      .then((d) => { if (!cancelled) { setData(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Could not load offerings."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [status, slotId]);

  const counts = useMemo(() => {
    if (!data) return { all: 0 };
    return { all: data.length };
  }, [data]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={`${counts.all} offerings`}
        title="Class offerings"
        kicker="time-bounded curricula filling each slot"
        actions={
          <>
            <Btn onClick={() => navigate("/admin/class-slots")}>Slots</Btn>
            <Btn variant="accent" size="lg" onClick={() => navigate("/admin/class-offerings/new")}>
              New offering
            </Btn>
          </>
        }
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <FilterPills
          activeValue={String(status)}
          onChange={(v) => setStatus(Number(v) as OfferingStatusFilter)}
          items={[
            { value: String(OfferingStatusFilter.All), label: "All" },
            { value: String(OfferingStatusFilter.Current), label: "Current" },
            { value: String(OfferingStatusFilter.Upcoming), label: "Upcoming" },
            { value: String(OfferingStatusFilter.Past), label: "Past" },
          ]}
        />
        <select
          value={slotId}
          onChange={(e) => setSlotId(e.target.value)}
          className="h-9 border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
        >
          <option value="">All slots</option>
          {slots.map((s) => (
            <option key={s.id} value={s.id}>{s.name} · {s.audienceAgeGroup}</option>
          ))}
        </select>
      </div>

      {loading && <p className="text-muted">Loading…</p>}
      {error && <p className="text-danger">{error}</p>}
      {!loading && !error && data && data.length === 0 && (
        <p className="text-muted">No offerings match.</p>
      )}

      {!loading && !error && data && data.length > 0 && (
        <article className="border border-border bg-panel">
          <header
            className="grid items-center gap-4 border-b border-border bg-panel-alt px-5 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted"
            style={{ gridTemplateColumns: "2.5fr 1.4fr 1.6fr 1.6fr 1fr" }}
          >
            <span>Subject</span>
            <span>Slot</span>
            <span>Dates</span>
            <span>Teacher</span>
            <span className="text-right">Actions</span>
          </header>
          <ul className="divide-y divide-border-soft">
            {data.map((o) => (
              <OfferingRow key={o.id} offering={o} onEdit={() => navigate(`/admin/class-offerings/${o.id}`)} />
            ))}
          </ul>
        </article>
      )}
    </div>
  );
}

function OfferingRow({
  offering, onEdit,
}: { offering: AdminClassOffering; onEdit: () => void }) {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const start = new Date(offering.startDate);
  const end = new Date(offering.endDate);
  const status = end < today ? "past" : start > today ? "upcoming" : "current";

  return (
    <li
      className="grid items-center gap-4 px-5 py-3"
      style={{ gridTemplateColumns: "2.5fr 1.4fr 1.6fr 1.6fr 1fr" }}
    >
      <div className="min-w-0">
        <button
          type="button"
          onClick={onEdit}
          className="text-left font-heading text-base font-semibold hover:underline"
        >
          {offering.subject}
        </button>
        <div className="mt-1">
          <Chip
            tone={status === "current" ? "success" : status === "upcoming" ? "accent" : "muted"}
            dot={status !== "past"}
          >
            {status[0].toUpperCase() + status.slice(1)}
          </Chip>
        </div>
      </div>
      <span className="text-sm">{offering.classSlotName}</span>
      <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono text-xs">
        {offering.startDate} → {offering.endDate}
      </span>
      <span className="text-sm text-muted">
        {offering.teacherFreeText ?? "—"}
      </span>
      <div className="flex justify-end">
        <Btn
          size="sm"
          iconRight={<ArrowRight className="h-3.5 w-3.5" />}
          onClick={onEdit}
        >
          Edit
        </Btn>
      </div>
    </li>
  );
}
