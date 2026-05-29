import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, ArrowRight } from "lucide-react";
import { profileRegistrationsApi, type MyRegistration } from "@/lib/api/eventRegistration";

const STATUS_LABEL: Record<number, string> = {
  0: "Confirmed",
  1: "Waitlisted",
  2: "Canceled",
};

export function ProfileRegistrationsPage() {
  const [items, setItems] = useState<MyRegistration[] | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function reload() {
    try { setItems(await profileRegistrationsApi.list()); }
    catch { setError("Could not load your registrations."); }
  }

  useEffect(() => { reload(); }, []);

  async function cancel(r: MyRegistration) {
    if (!window.confirm(`Cancel your registration for "${r.eventTitle}"?`)) return;
    setBusyId(r.id);
    try {
      const reason = window.prompt("Optional note:") ?? undefined;
      await profileRegistrationsApi.cancel(r.id, reason);
      await reload();
    } finally {
      setBusyId(null);
    }
  }

  return (
    <article className="mx-auto max-w-3xl px-4 py-8">
      <Link to="/profile" className="inline-flex items-center gap-1.5 text-sm text-primary hover:underline">
        <ArrowLeft aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
        Profile
      </Link>
      <h1 className="mt-2 text-2xl font-bold">My event registrations</h1>

      {error && <p className="mt-4 text-sm text-danger">{error}</p>}

      {items === null && !error && (
        <p className="mt-4 text-muted">Loading…</p>
      )}

      {items && items.length === 0 && (
        <p className="mt-6 text-muted">
          You don't have any current registrations.
          {" "}<Link to="/events" className="inline-flex items-center gap-1.5 text-primary hover:underline">
            Browse upcoming events
            <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
          </Link>
        </p>
      )}

      {items && items.length > 0 && (
        <ul className="mt-6 divide-y border bg-card">
          {items.map((r) => (
            <li key={r.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
              <div>
                <Link to={`/events/${r.eventSlug}`} className="font-medium hover:underline">
                  {r.eventTitle}
                </Link>
                <p className="text-xs text-muted">
                  {new Date(r.eventStartsAt).toLocaleString()}
                  {" · "}{STATUS_LABEL[r.status] ?? "Unknown"}
                </p>
              </div>
              {r.status !== 2 && (
                <button type="button" onClick={() => cancel(r)} disabled={busyId === r.id}
                  className="inline-flex h-9 items-center justify-center border border-danger/30 bg-card px-3 text-xs text-danger hover:bg-danger/10 disabled:opacity-50">
                  {busyId === r.id ? "Cancelling…" : "Cancel"}
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
    </article>
  );
}
