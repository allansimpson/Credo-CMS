import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  broadcastsApi,
  BROADCAST_STATUS_LABELS,
  type EmailBroadcast,
} from "@/lib/api/broadcasts";
import { PageHeader } from "@/components/shared/admin/EditorialPrimitives";

export function AdminBroadcastsListPage() {
  const [items, setItems] = useState<EmailBroadcast[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    setLoading(true);
    broadcastsApi.list({ pageSize: 50 })
      .then((r) => active && setItems(r.items))
      .catch((e) => active && setError(String(e)))
      .finally(() => active && setLoading(false));
    return () => { active = false; };
  }, []);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Email"
        title="Broadcasts"
        subtitle="Compose, schedule, and track email broadcasts to members and groups."
        actions={
          <Link
            to="/admin/broadcasts/new"
            className="inline-flex items-center gap-2 bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-800"
          >
            New broadcast
          </Link>
        }
      />

      {error && <div className="rounded-none border border-red-300 bg-red-50 p-4 text-red-800">{error}</div>}
      {loading ? (
        <div className="text-sm text-gray-500">Loading…</div>
      ) : items.length === 0 ? (
        <div className="rounded-none border border-gray-200 bg-gray-50 p-8 text-center text-sm text-gray-500">
          No broadcasts yet. <Link to="/admin/broadcasts/new" className="underline">Compose your first.</Link>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 border border-gray-200">
            <thead className="bg-gray-50 text-left text-xs uppercase tracking-wider text-gray-500">
              <tr>
                <th className="px-4 py-3">Subject</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Recipients</th>
                <th className="px-4 py-3">Delivered</th>
                <th className="px-4 py-3">Bounced</th>
                <th className="px-4 py-3">Sent</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 bg-white">
              {items.map((b) => (
                <tr key={b.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <Link to={`/admin/broadcasts/${b.id}`} className="font-medium hover:underline">
                      {b.subject}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-600">{BROADCAST_STATUS_LABELS[b.status]}</td>
                  <td className="px-4 py-3 text-sm text-gray-600">{b.recipientCountAtSend ?? "—"}</td>
                  <td className="px-4 py-3 text-sm text-gray-600">{b.deliveredCount}</td>
                  <td className="px-4 py-3 text-sm text-gray-600">{b.bouncedCount}</td>
                  <td className="px-4 py-3 text-sm text-gray-600">
                    {b.sentAt ? new Date(b.sentAt).toLocaleString() : "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
