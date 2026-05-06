import { useEffect, useMemo, useState } from "react";
import { Download, Filter, Terminal } from "lucide-react";
import { auditLogApi, type AuditLogQuery } from "@/lib/api/auditLog";
import type { AuditLogEntry } from "@/types/api";
import {
  Avatar,
  Btn,
  Chip,
  PageHeader,
} from "@/components/shared/admin/EditorialPrimitives";

type Tone = "accent" | "success" | "warn" | "danger" | "muted";

export function AuditLogPage() {
  const [entries, setEntries] = useState<AuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterAction, setFilterAction] = useState("");
  const [filterEntityType, setFilterEntityType] = useState("");
  const [selected, setSelected] = useState<AuditLogEntry | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const query: AuditLogQuery = { pageSize: 100 };
      if (filterAction) query.action = filterAction;
      if (filterEntityType) query.entityType = filterEntityType;
      const result = await auditLogApi.list(query);
      setEntries(result.items);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to load audit log.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, [filterAction, filterEntityType]);

  const grouped = useMemo(() => groupByDay(entries), [entries]);
  const byType = useMemo(() => countByEntityType(entries), [entries]);
  const byPerson = useMemo(() => countByActor(entries), [entries]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={`${entries.length} entries`}
        title="Audit Log"
        kicker="who did what, across the system"
        actions={
          <>
            <Btn iconLeft={<Filter className="h-3.5 w-3.5" />}>Filter</Btn>
            <Btn iconLeft={<Download className="h-3.5 w-3.5" />}>Export CSV</Btn>
          </>
        }
      />

      <div className="flex flex-wrap gap-3">
        <input
          type="text"
          placeholder="Action contains…"
          value={filterAction}
          onChange={(e) => setFilterAction(e.target.value)}
          className="h-9 w-56 border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
        />
        <input
          type="text"
          placeholder="Entity type…"
          value={filterEntityType}
          onChange={(e) => setFilterEntityType(e.target.value)}
          className="h-9 w-56 border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
        />
      </div>

      {error && (
        <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="grid gap-8 lg:grid-cols-[1fr_280px]">
        <section className="space-y-8">
          {loading && <p className="text-muted">Loading…</p>}
          {!loading && grouped.length === 0 && <p className="text-muted">No entries match your filters.</p>}
          {!loading && grouped.map(({ label, items }) => (
            <article key={label} className="space-y-3">
              <header className="flex items-baseline justify-between border-b border-border pb-2">
                <h2 className="font-heading text-base font-semibold">{label}</h2>
                <span
                  style={{ fontVariantNumeric: "tabular-nums" }}
                  className="font-mono text-xs text-muted"
                >
                  {items.length} event{items.length === 1 ? "" : "s"}
                </span>
              </header>
              <ul className="divide-y divide-border-soft border border-border bg-panel">
                {items.map((entry) => {
                  const tone = toneFor(entry);
                  const isSystem = !entry.userId;
                  return (
                    <li
                      key={entry.id}
                      className="relative grid items-center gap-4 px-5 py-3"
                      style={{ gridTemplateColumns: "92px 32px 1fr 120px" }}
                    >
                      <span aria-hidden className={`absolute inset-y-2 left-0 w-[3px] ${toneBar(tone)}`} />
                      <span
                        style={{ fontVariantNumeric: "tabular-nums" }}
                        className="font-mono text-xs text-muted"
                      >
                        {new Date(entry.timestamp).toLocaleTimeString(undefined, {
                          hour: "2-digit",
                          minute: "2-digit",
                          second: "2-digit",
                        })}
                      </span>
                      {isSystem ? (
                        <span
                          aria-hidden
                          className="grid h-7 w-7 place-items-center bg-foreground text-background"
                        >
                          <Terminal className="h-3.5 w-3.5" />
                        </span>
                      ) : (
                        <Avatar name={entry.userDisplayNameSnapshot ?? "?"} size="sm" />
                      )}
                      <div className="min-w-0">
                        <p className="text-sm">
                          <span className="font-medium">
                            {isSystem ? "system" : entry.userDisplayNameSnapshot}
                          </span>{" "}
                          <span className="text-fg-soft">{verbify(entry.action)}</span>{" "}
                          <span className="font-mono text-xs">
                            {entry.entityType}
                            {entry.entityId ? ` · ${entry.entityId.slice(0, 8)}` : ""}
                          </span>
                        </p>
                        {entry.ipAddress && (
                          <p className="mt-0.5 font-mono text-[11px] text-muted">{entry.ipAddress}</p>
                        )}
                      </div>
                      <div className="flex justify-end">
                        <Btn size="sm" variant="ghost" onClick={() => setSelected(entry)}>
                          Diff
                        </Btn>
                      </div>
                    </li>
                  );
                })}
              </ul>
            </article>
          ))}
        </section>

        <aside className="space-y-6">
          <section className="border border-border bg-panel p-5">
            <h2 className="font-heading text-base font-semibold">By type</h2>
            <ul className="mt-3 space-y-2 text-sm">
              {byType.map((row) => (
                <li key={row.label} className="relative flex items-center justify-between pl-3">
                  <span aria-hidden className={`absolute inset-y-1 left-0 w-[2px] ${toneBar(row.tone)}`} />
                  <span>{row.label}</span>
                  <span
                    style={{ fontVariantNumeric: "tabular-nums" }}
                    className="font-mono text-xs text-muted"
                  >
                    {String(row.count).padStart(3, "0")}
                  </span>
                </li>
              ))}
            </ul>
          </section>

          <section className="border border-border bg-panel p-5">
            <h2 className="font-heading text-base font-semibold">By person</h2>
            <ul className="mt-3 space-y-2 text-sm">
              {byPerson.slice(0, 6).map((row) => (
                <li key={row.label} className="flex items-center gap-3">
                  <Avatar name={row.label} size="sm" />
                  <span className="flex-1 truncate">{row.label}</span>
                  <span
                    style={{ fontVariantNumeric: "tabular-nums" }}
                    className="font-mono text-xs text-muted"
                  >
                    {String(row.count).padStart(3, "0")}
                  </span>
                </li>
              ))}
            </ul>
          </section>

          <section className="border border-border-soft bg-panel-alt p-5">
            <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-muted">
              Retention
            </p>
            <p className="mt-2 text-xs leading-relaxed text-fg-soft">
              Audit entries are kept for 90 days, then archived to cold storage.
              Older entries can be exported to CSV for compliance purposes.
            </p>
          </section>
        </aside>
      </div>

      {selected && <AuditDetailDrawer entry={selected} onClose={() => setSelected(null)} />}
    </div>
  );
}

function AuditDetailDrawer({ entry, onClose }: { entry: AuditLogEntry; onClose: () => void }) {
  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-30 flex justify-end bg-foreground/40">
      <div className="h-full w-full max-w-md overflow-y-auto border-l border-border bg-panel p-6">
        <div className="flex items-start justify-between gap-3">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-muted">
              Audit entry
            </p>
            <h2 className="mt-1 font-heading text-lg font-semibold">{entry.action}</h2>
          </div>
          <Btn size="sm" onClick={onClose}>Close</Btn>
        </div>
        <dl className="mt-6 space-y-4 text-sm">
          <Detail label="Timestamp"><span className="font-mono text-xs">{new Date(entry.timestamp).toLocaleString()}</span></Detail>
          <Detail label="Actor">{entry.userDisplayNameSnapshot ?? <Chip tone="muted">system</Chip>}</Detail>
          <Detail label="Entity">
            <span className="font-mono text-xs">
              {entry.entityType}{entry.entityId ? ` · ${entry.entityId}` : ""}
            </span>
          </Detail>
          {entry.ipAddress && (
            <Detail label="IP"><span className="font-mono text-xs">{entry.ipAddress}</span></Detail>
          )}
          {entry.detailsJson && (
            <Detail label="Details">
              <pre className="mt-1 max-h-96 overflow-auto bg-panel-alt p-3 font-mono text-xs">
                {tryFormatJson(entry.detailsJson)}
              </pre>
            </Detail>
          )}
        </dl>
      </div>
    </div>
  );
}

function Detail({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-[11px] font-semibold uppercase tracking-[0.16em] text-muted">{label}</dt>
      <dd className="mt-1">{children}</dd>
    </div>
  );
}

function tryFormatJson(s: string): string {
  try {
    return JSON.stringify(JSON.parse(s), null, 2);
  } catch {
    return s;
  }
}

function groupByDay(entries: AuditLogEntry[]): { label: string; items: AuditLogEntry[] }[] {
  const buckets = new Map<string, AuditLogEntry[]>();
  for (const e of entries) {
    const d = new Date(e.timestamp);
    const key = d.toDateString();
    if (!buckets.has(key)) buckets.set(key, []);
    buckets.get(key)!.push(e);
  }
  const today = new Date().toDateString();
  return Array.from(buckets.entries()).map(([key, items]) => {
    const d = new Date(key);
    const isToday = key === today;
    const label = isToday
      ? `Today · ${d.toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric" })}`
      : d.toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric" });
    return { label, items };
  });
}

function toneFor(entry: AuditLogEntry): Tone {
  const a = entry.action.toLowerCase();
  if (a.includes("delete") || a.includes("fail")) return "danger";
  if (a.includes("publish") || a.includes("activate")) return "success";
  if (a.includes("role") || a.includes("permission")) return "warn";
  if (a.includes("create") || a.includes("update") || a.includes("edit")) return "accent";
  return "muted";
}

function toneBar(tone: Tone): string {
  if (tone === "warn") return "bg-warn";
  if (tone === "danger") return "bg-danger";
  if (tone === "success") return "bg-success";
  if (tone === "accent") return "bg-accent";
  return "bg-border";
}

function verbify(action: string): string {
  return action.replace(/[A-Z]/g, (m) => " " + m.toLowerCase()).trim();
}

interface RollupRow {
  label: string;
  count: number;
  tone: Tone;
}

function countByEntityType(entries: AuditLogEntry[]): RollupRow[] {
  const counts = new Map<string, number>();
  for (const e of entries) {
    counts.set(e.entityType, (counts.get(e.entityType) ?? 0) + 1);
  }
  return Array.from(counts.entries())
    .sort((a, b) => b[1] - a[1])
    .slice(0, 5)
    .map(([label, count]) => ({
      label,
      count,
      tone: label.toLowerCase().includes("user") ? "warn"
        : label.toLowerCase().includes("page") || label.toLowerCase().includes("news") ? "accent"
        : "muted",
    }));
}

function countByActor(entries: AuditLogEntry[]): RollupRow[] {
  const counts = new Map<string, number>();
  for (const e of entries) {
    const name = e.userDisplayNameSnapshot ?? "system";
    counts.set(name, (counts.get(name) ?? 0) + 1);
  }
  return Array.from(counts.entries())
    .sort((a, b) => b[1] - a[1])
    .map(([label, count]) => ({ label, count, tone: "muted" as Tone }));
}
