import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Heart, MessageSquare, Plus } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import {
  memberPrayerApi,
  PrayerRequestStatus,
  type PrayerRequestListItem,
} from "@/lib/api/prayerRequests";
import { usePrayerRequestUpdates } from "@/hooks/usePrayerRequestUpdates";

type Tab = "active" | "answered";

export function PrayerRequestsListPage() {
  const [items, setItems] = useState<PrayerRequestListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>("active");
  // Tracks ids that arrived via SignalR after the initial load so we can
  // give them a brief fade-in for visual feedback.
  const [highlightIds, setHighlightIds] = useState<Set<string>>(new Set());

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await memberPrayerApi.list();
      setItems(data);
      setError(null);
    } catch {
      setError("Could not load prayer requests.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  // Real-time: any time something happens upstream we re-fetch the list. The
  // `kind` is used to short-circuit the rare cases (PrayedForCountChanged
  // can update inline without a refetch, but the simpler path keeps the
  // list in sync with the server).
  usePrayerRequestUpdates(useCallback((event) => {
    if (event.kind === "PrayerRequestPrayedForCountChanged") {
      setItems((prev) => prev.map((p) => p.id === event.prayerRequestId
        ? { ...p, prayedForCount: event.prayedForCount ?? p.prayedForCount }
        : p));
      return;
    }
    if (event.kind === "PrayerRequestCreated") {
      setHighlightIds((prev) => new Set(prev).add(event.prayerRequestId));
      window.setTimeout(() => {
        setHighlightIds((prev) => {
          const next = new Set(prev);
          next.delete(event.prayerRequestId);
          return next;
        });
      }, 4000);
    }
    void load();
  }, [load]));

  const filtered = useMemo(() => {
    return items.filter((p) => tab === "active"
      ? p.status === PrayerRequestStatus.Active
      : p.status === PrayerRequestStatus.Answered);
  }, [items, tab]);

  const counts = useMemo(() => ({
    active: items.filter((p) => p.status === PrayerRequestStatus.Active).length,
    answered: items.filter((p) => p.status === PrayerRequestStatus.Answered).length,
  }), [items]);

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-10">
          <header className="flex flex-wrap items-end justify-between gap-3 border-b pb-6">
            <div>
              <h1 className="text-3xl font-bold">Prayer requests</h1>
              <p className="mt-1 text-sm text-muted">
                Pray with us. Submit your own request, or mark "I prayed for this" on others.
              </p>
            </div>
            <Link
              to="/prayer-requests/new"
              className="inline-flex h-10 items-center gap-2 bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
            >
              <Plus className="h-4 w-4" /> New request
            </Link>
          </header>

          <nav className="mt-6 flex gap-1 border-b" aria-label="Filter by status">
            <TabButton active={tab === "active"} onClick={() => setTab("active")}>
              Active <span className="ml-1 font-mono text-xs text-muted">{counts.active}</span>
            </TabButton>
            <TabButton active={tab === "answered"} onClick={() => setTab("answered")}>
              Answered <span className="ml-1 font-mono text-xs text-muted">{counts.answered}</span>
            </TabButton>
          </nav>

          <section className="mt-6 space-y-3">
            {loading && <p className="text-muted">Loading…</p>}
            {error && <p className="text-danger">{error}</p>}
            {!loading && !error && filtered.length === 0 && (
              <p className="text-muted">
                {tab === "active"
                  ? "No active prayer requests right now."
                  : "No answered prayers yet."}
              </p>
            )}
            {filtered.map((p) => (
              <PrayerCard
                key={p.id}
                request={p}
                isNew={highlightIds.has(p.id)}
                onChange={(next) => setItems((prev) => prev.map((x) => x.id === p.id ? next : x))}
              />
            ))}
          </section>
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function PrayerCard({
  request, isNew, onChange,
}: {
  request: PrayerRequestListItem;
  isNew: boolean;
  onChange: (next: PrayerRequestListItem) => void;
}) {
  const [submitting, setSubmitting] = useState(false);
  const togglePrayed = async () => {
    setSubmitting(true);
    try {
      const result = request.viewerHasPrayed
        ? await memberPrayerApi.unmarkPrayed(request.id)
        : await memberPrayerApi.markPrayed(request.id);
      onChange({ ...request, viewerHasPrayed: !request.viewerHasPrayed, prayedForCount: result.count });
    } catch {
      // swallow — optimistic rollback isn't worth the complexity here, the
      // SignalR count event will reconcile any drift.
    } finally {
      setSubmitting(false);
    }
  };

  const submitter = request.isAnonymous
    ? "Anonymous"
    : request.submitterDisplayName ?? "Anonymous";

  return (
    <article
      className={
        "rounded-lg border bg-card p-5 transition-colors " +
        (isNew ? "animate-pulse border-accent" : "")
      }
    >
      <header className="flex flex-wrap items-baseline gap-2">
        <h2 className="text-lg font-semibold">
          <Link to={`/prayer-requests/${request.id}`} className="hover:underline">
            {request.title}
          </Link>
        </h2>
        <span className="text-xs text-muted">
          by {submitter} · {new Date(request.createdAt).toLocaleDateString()}
        </span>
        {request.status === PrayerRequestStatus.Answered && (
          <span className="ml-auto rounded bg-success/15 px-2 py-0.5 text-[11px] font-medium uppercase tracking-wider text-success">
            Answered
          </span>
        )}
      </header>

      <div className="prose prose-sm mt-3 max-w-none text-fg-soft">
        <TipTapReadOnly valueJson={request.bodyJson} />
      </div>

      <footer className="mt-4 flex flex-wrap items-center gap-3 border-t pt-3 text-sm">
        <button
          type="button"
          disabled={submitting}
          onClick={togglePrayed}
          className={
            "inline-flex h-8 items-center gap-1.5 px-3 text-xs font-medium transition-colors disabled:opacity-50 " +
            (request.viewerHasPrayed
              ? "bg-accent text-accent-foreground"
              : "border border-border bg-card hover:bg-panel-alt")
          }
        >
          <Heart
            className={"h-3.5 w-3.5 " + (request.viewerHasPrayed ? "fill-current" : "")}
            aria-hidden
          />
          {request.viewerHasPrayed ? "I'm praying" : "I'll pray for this"}
          <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono">
            · {request.prayedForCount}
          </span>
        </button>
        {request.updateCount > 0 && (
          <Link
            to={`/prayer-requests/${request.id}`}
            className="inline-flex items-center gap-1 text-xs text-muted hover:text-foreground"
          >
            <MessageSquare className="h-3.5 w-3.5" />
            {request.updateCount} update{request.updateCount === 1 ? "" : "s"}
          </Link>
        )}
        <Link
          to={`/prayer-requests/${request.id}`}
          className="ml-auto text-xs text-primary hover:underline"
        >
          Open →
        </Link>
      </footer>
    </article>
  );
}

function TabButton({
  active, onClick, children,
}: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={
        "px-4 py-3 text-sm transition-colors " +
        (active
          ? "border-b-2 border-accent font-semibold text-foreground"
          : "text-muted hover:text-foreground")
      }
    >
      {children}
    </button>
  );
}
