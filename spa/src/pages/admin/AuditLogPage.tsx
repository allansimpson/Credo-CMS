import { useEffect, useState } from "react";
import { ResponsiveTable, type ColumnDef } from "@/components/shared/ResponsiveTable";
import { auditLogApi, type AuditLogQuery } from "@/lib/api/auditLog";
import type { AuditLogEntry } from "@/types/api";

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

  const columns: ColumnDef<AuditLogEntry>[] = [
    {
      id: "timestamp",
      header: "Time",
      accessor: (e) => new Date(e.timestamp).toLocaleString(),
      mobilePriority: 1,
      sortBy: (e) => e.timestamp,
    },
    {
      id: "actor",
      header: "Actor",
      accessor: (e) => e.userDisplayNameSnapshot,
      mobilePriority: 2,
    },
    {
      id: "action",
      header: "Action",
      accessor: (e) => e.action,
      mobilePriority: 3,
    },
    {
      id: "entity",
      header: "Entity",
      accessor: (e) => e.entityId ? `${e.entityType} (${e.entityId.slice(0, 8)}…)` : e.entityType,
    },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold">Audit Log</h1>
      <p className="mt-2 text-muted-foreground">Append-only record of who did what across the system.</p>

      <div className="mt-4 flex flex-wrap gap-3">
        <input
          type="text"
          placeholder="Action contains…"
          value={filterAction}
          onChange={(e) => setFilterAction(e.target.value)}
          className="h-10 rounded-md border border-input bg-background px-3 text-sm"
        />
        <input
          type="text"
          placeholder="Entity type…"
          value={filterEntityType}
          onChange={(e) => setFilterEntityType(e.target.value)}
          className="h-10 rounded-md border border-input bg-background px-3 text-sm"
        />
      </div>

      {error && (
        <div role="alert" className="mt-4 rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          {error}
        </div>
      )}

      <div className="mt-6">
        {loading ? (
          <p className="text-muted-foreground">Loading…</p>
        ) : (
          <ResponsiveTable
            data={entries}
            columns={columns}
            rowKey={(e) => e.id}
            searchPlaceholder="Search…"
            emptyMessage="No entries match your filters."
            onRowClick={setSelected}
          />
        )}
      </div>

      {selected && <AuditDetailDrawer entry={selected} onClose={() => setSelected(null)} />}
    </div>
  );
}

function AuditDetailDrawer({ entry, onClose }: { entry: AuditLogEntry; onClose: () => void }) {
  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-30 flex justify-end bg-foreground/40">
      <div className="h-full w-full max-w-md overflow-y-auto bg-background p-6 shadow-xl">
        <div className="flex items-start justify-between">
          <h2 className="text-lg font-semibold">{entry.action}</h2>
          <button onClick={onClose} className="rounded-md border px-2 py-1 text-sm">Close</button>
        </div>
        <dl className="mt-4 space-y-3 text-sm">
          <div>
            <dt className="text-xs uppercase tracking-wide text-muted-foreground">Timestamp</dt>
            <dd>{new Date(entry.timestamp).toLocaleString()}</dd>
          </div>
          <div>
            <dt className="text-xs uppercase tracking-wide text-muted-foreground">Actor</dt>
            <dd>{entry.userDisplayNameSnapshot}</dd>
          </div>
          <div>
            <dt className="text-xs uppercase tracking-wide text-muted-foreground">Entity</dt>
            <dd>{entry.entityType}{entry.entityId ? ` · ${entry.entityId}` : ""}</dd>
          </div>
          {entry.ipAddress && (
            <div>
              <dt className="text-xs uppercase tracking-wide text-muted-foreground">IP</dt>
              <dd>{entry.ipAddress}</dd>
            </div>
          )}
          {entry.detailsJson && (
            <div>
              <dt className="text-xs uppercase tracking-wide text-muted-foreground">Details</dt>
              <dd>
                <pre className="mt-1 max-h-96 overflow-auto rounded-md bg-muted p-3 text-xs">
                  {tryFormatJson(entry.detailsJson)}
                </pre>
              </dd>
            </div>
          )}
        </dl>
      </div>
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
