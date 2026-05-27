import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import {
  broadcastsApi,
  BROADCAST_STATUS_LABELS,
  RECIPIENT_STATUS_LABELS,
  type EmailBroadcast,
  type EmailBroadcastRecipient,
  type RecipientStatus,
} from "@/lib/api/broadcasts";
import { PageHeader } from "@/components/shared/admin/EditorialPrimitives";

export function AdminBroadcastDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [broadcast, setBroadcast] = useState<EmailBroadcast | null>(null);
  const [recipients, setRecipients] = useState<EmailBroadcastRecipient[]>([]);
  const [statusFilter, setStatusFilter] = useState<RecipientStatus | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    broadcastsApi.get(id).then(setBroadcast).catch((e) => setError(String(e)));
  }, [id]);

  useEffect(() => {
    if (!id) return;
    broadcastsApi.recipients(id, { status: statusFilter, pageSize: 100 })
      .then((r) => setRecipients(r.items))
      .catch((e) => setError(String(e)));
  }, [id, statusFilter]);

  if (!broadcast) return <div className="text-sm text-gray-500">Loading…</div>;

  const cancelDisabled = ![0, 1].includes(broadcast.status);

  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Broadcast" title={broadcast.subject} />
      {error && <div className="rounded-none border border-red-300 bg-red-50 p-4 text-red-800">{error}</div>}

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-5">
        <Stat label="Status" value={BROADCAST_STATUS_LABELS[broadcast.status]} />
        <Stat label="Recipients" value={broadcast.recipientCountAtSend ?? "—"} />
        <Stat label="Delivered" value={broadcast.deliveredCount} />
        <Stat label="Bounced" value={broadcast.bouncedCount} />
        <Stat label="Spam" value={broadcast.complaintCount} />
      </div>

      <div className="flex items-center gap-2">
        <button
          type="button"
          disabled={cancelDisabled}
          onClick={async () => {
            if (!id) return;
            if (!confirm("Cancel this scheduled broadcast?")) return;
            await broadcastsApi.cancel(id);
            broadcastsApi.get(id).then(setBroadcast);
          }}
          className="border border-gray-300 px-3 py-2 text-sm hover:bg-gray-50 disabled:opacity-50"
        >
          Cancel scheduled send
        </button>
        <select
          value={statusFilter ?? ""}
          onChange={(e) => setStatusFilter(e.target.value === "" ? undefined : (Number(e.target.value) as RecipientStatus))}
          className="border border-gray-300 px-3 py-2 text-sm"
        >
          <option value="">All recipients</option>
          {Object.entries(RECIPIENT_STATUS_LABELS).map(([k, v]) => (
            <option key={k} value={k}>{v}</option>
          ))}
        </select>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200 border border-gray-200">
          <thead className="bg-gray-50 text-left text-xs uppercase tracking-wider text-gray-500">
            <tr>
              <th className="px-4 py-3">Recipient</th>
              <th className="px-4 py-3">Email</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Delivered</th>
              <th className="px-4 py-3">Opened</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 bg-white">
            {recipients.map((r) => (
              <tr key={r.id} className="hover:bg-gray-50">
                <td className="px-4 py-3 text-sm">{r.displayNameSnapshot}</td>
                <td className="px-4 py-3 text-sm text-gray-600">{r.emailAddressSnapshot}</td>
                <td className="px-4 py-3 text-sm">{RECIPIENT_STATUS_LABELS[r.status]}</td>
                <td className="px-4 py-3 text-sm text-gray-600">
                  {r.deliveredAt ? new Date(r.deliveredAt).toLocaleString() : "—"}
                </td>
                <td className="px-4 py-3 text-sm text-gray-600">
                  {r.openedAt ? new Date(r.openedAt).toLocaleString() : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: number | string }) {
  return (
    <div className="border border-gray-200 p-4">
      <div className="text-xs uppercase tracking-wider text-gray-500">{label}</div>
      <div className="mt-1 text-2xl font-semibold tabular-nums">{value}</div>
    </div>
  );
}
