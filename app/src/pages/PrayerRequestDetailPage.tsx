import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Heart, Trash2 } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { useAuth } from "@/hooks/useAuth";
import { usePrayerRequestUpdates } from "@/hooks/usePrayerRequestUpdates";
import {
  adminPrayerApi,
  memberPrayerApi,
  PrayerRequestStatus,
  type MemberPrayerRequest,
} from "@/lib/api/prayerRequests";

export function PrayerRequestDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasAnyRole } = useAuth();
  const isEditorPlus = hasAnyRole(["Editor", "Administrator"]);

  const [request, setRequest] = useState<MemberPrayerRequest | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true); setNotFound(false);
    try {
      const data = await memberPrayerApi.get(id);
      setRequest(data);
    } catch (err) {
      if ((err as { status?: number }).status === 404) setNotFound(true);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { void load(); }, [load]);

  // Real-time refresh — reload on any event scoped to this id.
  usePrayerRequestUpdates(useCallback(() => { void load(); }, [load]), id);

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-10">
          <Link
            to="/prayer-requests"
            className="mb-6 inline-flex items-center gap-1 text-sm text-muted hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" /> All requests
          </Link>

          {loading && <p className="text-muted">Loading…</p>}
          {!loading && notFound && (
            <div className="rounded-lg border bg-card p-6">
              <h1 className="text-xl font-bold">Request not found</h1>
              <p className="mt-2 text-sm text-muted">It may have been removed or archived.</p>
            </div>
          )}
          {!loading && request && (
            <Detail
              request={request}
              isEditorPlus={isEditorPlus}
              onChange={setRequest}
              onReload={load}
              onDeleted={() => navigate("/prayer-requests")}
            />
          )}
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function Detail({
  request, isEditorPlus, onChange, onReload, onDeleted,
}: {
  request: MemberPrayerRequest;
  isEditorPlus: boolean;
  onChange: (next: MemberPrayerRequest) => void;
  onReload: () => Promise<void>;
  onDeleted: () => void;
}) {
  const submitter = request.isAnonymous ? "Anonymous" : (request.submitterDisplayName ?? "Anonymous");

  const togglePrayed = async () => {
    try {
      const result = request.viewerHasPrayed
        ? await memberPrayerApi.unmarkPrayed(request.id)
        : await memberPrayerApi.markPrayed(request.id);
      onChange({ ...request, viewerHasPrayed: !request.viewerHasPrayed, prayedForCount: result.count });
    } catch { /* SignalR will reconcile */ }
  };

  const handleDelete = async () => {
    if (!window.confirm("Delete this prayer request?")) return;
    try {
      await memberPrayerApi.delete(request.id);
      onDeleted();
    } catch (err) {
      const messages = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Could not delete."];
      window.alert(messages.join("; "));
    }
  };

  return (
    <article className="space-y-6">
      <header className="border-b pb-6">
        <div className="flex flex-wrap items-baseline gap-2">
          <h1 className="text-3xl font-bold">{request.title}</h1>
          {request.status === PrayerRequestStatus.Answered && (
            <span className="rounded bg-success/15 px-2 py-0.5 text-[11px] font-medium uppercase tracking-wider text-success">
              Answered
            </span>
          )}
          {request.status === PrayerRequestStatus.Archived && (
            <span className="rounded bg-panel-alt px-2 py-0.5 text-[11px] font-medium uppercase tracking-wider text-muted">
              Archived
            </span>
          )}
        </div>
        <p className="mt-2 text-sm text-muted">
          Submitted by {submitter} · {new Date(request.createdAt).toLocaleString()}
        </p>
      </header>

      <section className="prose prose-sm max-w-none">
        <TipTapReadOnly json={request.bodyJson} />
      </section>

      <footer className="flex flex-wrap items-center gap-3 border-y py-4">
        <button
          type="button"
          onClick={togglePrayed}
          className={
            "inline-flex h-10 items-center gap-2 px-4 text-sm font-medium transition-colors " +
            (request.viewerHasPrayed
              ? "bg-accent text-accent-foreground"
              : "border border-border bg-card hover:bg-panel-alt")
          }
        >
          <Heart className={"h-4 w-4 " + (request.viewerHasPrayed ? "fill-current" : "")} aria-hidden />
          {request.viewerHasPrayed ? "I'm praying" : "I'll pray for this"}
          <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono">
            · {request.prayedForCount}
          </span>
        </button>
        {request.viewerCanEdit && (
          <>
            <Link
              to={`/prayer-requests/${request.id}/edit`}
              className="inline-flex h-10 items-center justify-center border bg-card px-4 text-sm hover:bg-panel-alt"
            >
              Edit
            </Link>
            <button
              type="button"
              onClick={handleDelete}
              className="inline-flex h-10 items-center justify-center gap-1 border border-danger/30 bg-card px-4 text-sm text-danger hover:bg-danger/10"
            >
              <Trash2 className="h-4 w-4" /> Delete
            </button>
          </>
        )}
      </footer>

      {request.updates.length > 0 && (
        <section className="space-y-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">Updates</h2>
          <ul className="space-y-3">
            {request.updates.map((u) => (
              <li key={u.id} className="border-l-2 border-accent bg-panel-alt p-4">
                <p className="text-xs uppercase tracking-wide text-muted">
                  {u.postedByLabel} · {new Date(u.createdAt).toLocaleString()}
                </p>
                <div className="prose prose-sm mt-2 max-w-none">
                  <TipTapReadOnly json={u.bodyJson} />
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}

      {isEditorPlus && (
        <AdminActions request={request} onReload={onReload} />
      )}
    </article>
  );
}

function AdminActions({
  request, onReload,
}: { request: MemberPrayerRequest; onReload: () => Promise<void> }) {
  const [updateBody, setUpdateBody] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const postUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!updateBody) return;
    setSubmitting(true); setError(null);
    try {
      await adminPrayerApi.addUpdate(request.id, { bodyJson: updateBody });
      setUpdateBody(null);
      await onReload();
    } catch (err) {
      const messages = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Could not post update."];
      setError(messages.join("; "));
    } finally {
      setSubmitting(false);
    }
  };

  const setStatus = async (status: PrayerRequestStatus) => {
    try {
      await adminPrayerApi.changeStatus(request.id, { status });
      await onReload();
    } catch (err) {
      const messages = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Could not change status."];
      window.alert(messages.join("; "));
    }
  };

  return (
    <section className="space-y-4 rounded-lg border border-dashed bg-panel-alt p-5">
      <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
        Editor / Admin actions
      </h2>

      <form onSubmit={postUpdate} className="space-y-3">
        <p className="text-sm font-medium">Post a pastoral update</p>
        {error && (
          <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-2 text-xs text-danger">
            {error}
          </div>
        )}
        <TipTapEditor
          ariaLabel="Pastoral update"
          valueJson={updateBody}
          onChangeJson={setUpdateBody}
          placeholder="A short note from a pastor or admin…"
        />
        <button
          type="submit"
          disabled={submitting || !updateBody}
          className="inline-flex h-9 items-center justify-center rounded-md bg-primary px-4 text-xs font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
        >
          {submitting ? "Posting…" : "Post update"}
        </button>
      </form>

      <div className="flex flex-wrap gap-2 border-t pt-3">
        <p className="w-full text-sm font-medium">Change status</p>
        <StatusButton
          active={request.status === PrayerRequestStatus.Active}
          onClick={() => setStatus(PrayerRequestStatus.Active)}
        >
          Active
        </StatusButton>
        <StatusButton
          active={request.status === PrayerRequestStatus.Answered}
          onClick={() => setStatus(PrayerRequestStatus.Answered)}
        >
          Mark answered
        </StatusButton>
        <StatusButton
          active={request.status === PrayerRequestStatus.Archived}
          onClick={() => setStatus(PrayerRequestStatus.Archived)}
        >
          Archive
        </StatusButton>
      </div>
    </section>
  );
}

function StatusButton({
  active, onClick, children,
}: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={
        "h-9 px-3 text-xs font-medium transition-colors " +
        (active
          ? "bg-foreground text-background"
          : "border border-border bg-card hover:bg-panel-alt")
      }
    >
      {children}
    </button>
  );
}
