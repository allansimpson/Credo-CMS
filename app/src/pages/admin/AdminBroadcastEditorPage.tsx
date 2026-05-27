import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  broadcastsApi,
  type BroadcastTargetMode,
  type EmailBroadcast,
  type EmailCategory,
  type RecipientPreview,
} from "@/lib/api/broadcasts";
import { PageHeader } from "@/components/shared/admin/EditorialPrimitives";

/**
 * Broadcast composer. Minimal but functional: subject, body
 * (textarea — TipTap integration deferred to a follow-up commit so the
 * initial ship doesn't gate on RTE plumbing), target mode, send-now /
 * schedule. Recipient preview hits the server-side resolver so the
 * editor sees the actual count + sample.
 */
export function AdminBroadcastEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [draft, setDraft] = useState<EmailBroadcast | null>(null);
  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");
  const [targetMode, setTargetMode] = useState<BroadcastTargetMode>(0);
  const [scheduleAt, setScheduleAt] = useState<string>("");
  const [preview, setPreview] = useState<RecipientPreview | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    broadcastsApi.get(id).then((b) => {
      setDraft(b);
      setSubject(b.subject);
      setBody(b.body);
      setTargetMode(b.targetMode);
    }).catch((e) => setError(String(e)));
  }, [id]);

  const ensureDraft = async () => {
    if (draft) {
      const updated = await broadcastsApi.update(draft.id, {
        subject,
        body,
        plainTextBody: null,
        targetMode,
        targetGroupIds: null,
        category: 3 as EmailCategory,
      });
      setDraft(updated);
      return updated;
    }
    const created = await broadcastsApi.create({
      subject,
      body,
      plainTextBody: null,
      targetMode,
      targetGroupIds: null,
      category: 3 as EmailCategory,
    });
    setDraft(created);
    return created;
  };

  const onSaveDraft = async () => {
    setBusy(true); setError(null);
    try { await ensureDraft(); }
    catch (e) { setError(String(e)); }
    finally { setBusy(false); }
  };

  const onPreview = async () => {
    setBusy(true); setError(null);
    try {
      const b = await ensureDraft();
      setPreview(await broadcastsApi.preview(b.id));
    } catch (e) { setError(String(e)); }
    finally { setBusy(false); }
  };

  const onSendNow = async () => {
    if (!confirm("Send this broadcast now? This cannot be undone.")) return;
    setBusy(true); setError(null);
    try {
      const b = await ensureDraft();
      await broadcastsApi.send(b.id);
      navigate(`/admin/broadcasts/${b.id}`);
    } catch (e) { setError(String(e)); }
    finally { setBusy(false); }
  };

  const onSchedule = async () => {
    if (!scheduleAt) { setError("Pick a scheduled send time first."); return; }
    setBusy(true); setError(null);
    try {
      const b = await ensureDraft();
      await broadcastsApi.schedule(b.id, new Date(scheduleAt).toISOString());
      navigate(`/admin/broadcasts/${b.id}`);
    } catch (e) { setError(String(e)); }
    finally { setBusy(false); }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Email"
        title={draft ? "Edit broadcast" : "Compose broadcast"}
      />
      {error && <div className="rounded-none border border-red-300 bg-red-50 p-4 text-red-800">{error}</div>}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-2">
          <label className="block">
            <span className="block text-sm font-medium text-gray-700">Subject</span>
            <input
              type="text" value={subject} onChange={(e) => setSubject(e.target.value)}
              className="mt-1 block w-full border border-gray-300 px-3 py-2"
              placeholder="Subject line"
            />
          </label>
          <label className="block">
            <span className="block text-sm font-medium text-gray-700">Body (HTML)</span>
            <textarea
              rows={16} value={body} onChange={(e) => setBody(e.target.value)}
              className="mt-1 block w-full border border-gray-300 px-3 py-2 font-mono text-sm"
              placeholder="HTML body. Use {{firstName}}, {{lastName}}, {{unsubscribeUrl}} as merge fields."
            />
          </label>
        </div>

        <aside className="space-y-4">
          <div className="border border-gray-200 p-4">
            <h3 className="text-sm font-medium text-gray-700">Target</h3>
            <label className="mt-2 flex items-center gap-2 text-sm">
              <input type="radio" checked={targetMode === 0} onChange={() => setTargetMode(0)} />
              All members
            </label>
            <label className="mt-1 flex items-center gap-2 text-sm">
              <input type="radio" checked={targetMode === 1} onChange={() => setTargetMode(1)} />
              Specific groups (configure on detail view)
            </label>
          </div>

          <div className="border border-gray-200 p-4">
            <h3 className="text-sm font-medium text-gray-700">Recipient preview</h3>
            <button
              type="button" onClick={onPreview} disabled={busy}
              className="mt-2 text-sm text-blue-700 underline disabled:text-gray-400"
            >
              Refresh preview
            </button>
            {preview && (
              <div className="mt-2 text-sm">
                <div className="font-medium">{preview.totalCount} recipients</div>
                <ul className="mt-1 list-inside list-disc text-gray-600">
                  {preview.sample.map((r) => <li key={r.emailAddress}>{r.displayName} &lt;{r.emailAddress}&gt;</li>)}
                </ul>
              </div>
            )}
          </div>

          <div className="border border-gray-200 p-4">
            <h3 className="text-sm font-medium text-gray-700">Schedule</h3>
            <input
              type="datetime-local" value={scheduleAt} onChange={(e) => setScheduleAt(e.target.value)}
              className="mt-2 block w-full border border-gray-300 px-3 py-2 text-sm"
            />
            <div className="mt-3 grid grid-cols-2 gap-2">
              <button type="button" onClick={onSaveDraft} disabled={busy}
                className="border border-gray-300 px-3 py-2 text-sm hover:bg-gray-50 disabled:opacity-50">
                Save draft
              </button>
              <button type="button" onClick={onSchedule} disabled={busy}
                className="border border-gray-300 px-3 py-2 text-sm hover:bg-gray-50 disabled:opacity-50">
                Schedule
              </button>
              <button type="button" onClick={onSendNow} disabled={busy}
                className="col-span-2 bg-gray-900 px-3 py-2 text-sm text-white hover:bg-gray-800 disabled:opacity-50">
                Send now
              </button>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
