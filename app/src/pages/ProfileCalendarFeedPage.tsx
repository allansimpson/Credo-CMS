import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { profileCalendarFeedApi, type FeedTokenStatus } from "@/lib/api/profileCalendarFeed";

export function ProfileCalendarFeedPage() {
  const [status, setStatus] = useState<FeedTokenStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [issuedUrl, setIssuedUrl] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function reload() {
    setLoading(true);
    try { setStatus(await profileCalendarFeedApi.status()); }
    finally { setLoading(false); }
  }
  useEffect(() => { reload(); }, []);

  async function issue() {
    if (status?.hasToken && !window.confirm("Re-issuing will invalidate your existing feed URL. Continue?")) return;
    setBusy(true);
    try {
      const r = await profileCalendarFeedApi.issue();
      setIssuedUrl(r.url);
      await reload();
    } finally { setBusy(false); }
  }

  async function revoke() {
    if (!window.confirm("Revoke your calendar feed URL?")) return;
    setBusy(true);
    try {
      await profileCalendarFeedApi.revoke();
      setIssuedUrl(null);
      await reload();
    } finally { setBusy(false); }
  }

  return (
    <article className="mx-auto max-w-2xl px-4 py-8">
      <Link to="/profile" className="text-sm text-primary hover:underline">← Profile</Link>
      <h1 className="mt-2 text-2xl font-bold">My calendar feed</h1>
      <p className="mt-2 text-sm text-muted-foreground">
        Subscribe to all upcoming events (including members-only events) from
        Apple Calendar, Google Calendar, or Outlook. Each member has their
        own feed URL — don't share it.
      </p>

      {loading && <p className="mt-4 text-muted-foreground">Loading…</p>}

      {!loading && status && (
        <div className="mt-6 space-y-4">
          {status.hasToken ? (
            <div className="border bg-card p-4 text-sm">
              <p>
                <span className="font-medium">A feed URL is active.</span>
                {status.createdAt && <> Created {new Date(status.createdAt).toLocaleDateString()}.</>}
                {status.lastUsedAt && <> Last used {new Date(status.lastUsedAt).toLocaleDateString()}.</>}
              </p>
              <p className="mt-2 text-xs text-muted-foreground">
                For privacy we only store a hash of the URL — copy it from the
                "Re-issue" step below if you don't have it saved.
              </p>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">No feed URL has been generated yet.</p>
          )}

          {issuedUrl && (
            <div className="border border-emerald-300 bg-emerald-50 p-4 text-sm">
              <p className="font-semibold text-emerald-900">Your feed URL:</p>
              <p className="mt-2 break-all text-emerald-900">{issuedUrl}</p>
              <p className="mt-2 text-xs text-emerald-900/80">
                Copy this now — it won't be shown again. To paste into Apple
                Calendar: File → New Calendar Subscription.
              </p>
            </div>
          )}

          <div className="flex flex-wrap gap-3">
            <button type="button" onClick={issue} disabled={busy}
              className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
              {status.hasToken ? "Re-issue feed URL" : "Generate feed URL"}
            </button>
            {status.hasToken && (
              <button type="button" onClick={revoke} disabled={busy}
                className="inline-flex h-10 items-center justify-center border border-destructive/30 bg-card px-4 text-sm text-destructive hover:bg-destructive/10 disabled:opacity-50">
                Revoke
              </button>
            )}
          </div>
        </div>
      )}
    </article>
  );
}
