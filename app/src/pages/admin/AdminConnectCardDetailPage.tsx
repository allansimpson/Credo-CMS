import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Mail, Phone, RotateCw, Trash2 } from "lucide-react";
import {
  adminConnectCardApi,
  ConnectCardStatus,
  type AdminConnectCardDetail,
} from "@/lib/api/connectCard";
import { useAuth } from "@/hooks/useAuth";
import {
  Btn,
  Chip,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";

export function AdminConnectCardDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasAnyRole } = useAuth();
  const isAdmin = hasAnyRole(["Administrator"]);

  const [card, setCard] = useState<AdminConnectCardDetail | null>(null);
  const [notes, setNotes] = useState("");
  const [loading, setLoading] = useState(true);
  const [savingNotes, setSavingNotes] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    adminConnectCardApi.get(id)
      .then((c) => { setCard(c); setNotes(c.adminNotes ?? ""); })
      .catch(() => setError("Could not load card."))
      .finally(() => setLoading(false));
  }, [id]);

  const setStatus = async (status: ConnectCardStatus) => {
    if (!id) return;
    try {
      const updated = await adminConnectCardApi.updateStatus(id, status);
      setCard(updated);
    } catch (err) {
      const messages = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Could not update status."];
      window.alert(messages.join("; "));
    }
  };

  const saveNotes = async () => {
    if (!id) return;
    setSavingNotes(true);
    try {
      const updated = await adminConnectCardApi.updateNotes(id, notes || null);
      setCard(updated);
    } catch {
      // surfaced via inline error if needed
    } finally {
      setSavingNotes(false);
    }
  };

  const resend = async () => {
    if (!id) return;
    try {
      await adminConnectCardApi.resend(id);
      const fresh = await adminConnectCardApi.get(id);
      setCard(fresh);
    } catch (err) {
      const messages = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Could not resend."];
      window.alert(messages.join("; "));
    }
  };

  const handleDelete = async () => {
    if (!id) return;
    if (!window.confirm("Delete this connect card? (Status set to Not legit.)")) return;
    await adminConnectCardApi.delete(id);
    navigate("/admin/connect-cards");
  };

  if (loading) return <p className="text-muted">Loading…</p>;
  if (error || !card) return <p className="text-danger">{error ?? "Not found."}</p>;

  return (
    <div className="space-y-6">
      <Link
        to="/admin/connect-cards"
        className="inline-flex items-center gap-1 text-sm text-muted hover:text-foreground"
      >
        <ArrowLeft className="h-4 w-4" /> Back to list
      </Link>

      <PageHeader
        eyebrow={`Submitted ${new Date(card.submittedAt).toLocaleString()}`}
        title={card.name}
        actions={
          <>
            {card.email && (
              <Btn iconLeft={<RotateCw className="h-3.5 w-3.5" />} onClick={resend}>
                Resend ack email
              </Btn>
            )}
            {isAdmin && (
              <Btn variant="danger" iconLeft={<Trash2 className="h-3.5 w-3.5" />} onClick={handleDelete}>
                Mark not legit
              </Btn>
            )}
          </>
        }
      />

      <section className="space-y-4">
        <SectionHead number="01" title="Contact & visit" />
        <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {card.email && (
            <Row icon={<Mail className="h-4 w-4" />} label="Email">
              <a href={`mailto:${card.email}`} className="text-primary hover:underline">{card.email}</a>
            </Row>
          )}
          {card.phone && (
            <Row icon={<Phone className="h-4 w-4" />} label="Phone">
              <a href={`tel:${card.phone}`} className="text-primary hover:underline">{card.phone}</a>
            </Row>
          )}
          <Row label="First-time visitor">
            {card.isFirstTimeVisitor
              ? <Chip tone="accent" dot>First-time</Chip>
              : <span className="text-muted">No</span>}
          </Row>
          {card.serviceDate && <Row label="Service date">{card.serviceDate}</Row>}
          <Row label="How did you hear?">{card.howDidYouHear}</Row>
          {card.acknowledgmentEmailSentAt && (
            <Row label="Ack email sent">
              <span className="font-mono text-xs">
                {new Date(card.acknowledgmentEmailSentAt).toLocaleString()}
              </span>
            </Row>
          )}
        </dl>
        {card.comments && (
          <div className="rounded-md border bg-panel p-4">
            <p className="text-xs uppercase tracking-wide text-muted">Comments</p>
            <p className="mt-2 whitespace-pre-wrap text-sm">{card.comments}</p>
          </div>
        )}
        {card.interests.length > 0 && (
          <div>
            <p className="text-xs uppercase tracking-wide text-muted">Interests</p>
            <ul className="mt-2 flex flex-wrap gap-2">
              {card.interests.map((i) => (
                <li key={i}>
                  <Chip tone="muted">{i}</Chip>
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>

      <section className="space-y-3">
        <SectionHead number="02" title="Status" />
        <div className="flex flex-wrap gap-2">
          <StatusButton active={card.status === ConnectCardStatus.New} onClick={() => setStatus(ConnectCardStatus.New)}>
            New
          </StatusButton>
          <StatusButton active={card.status === ConnectCardStatus.FollowUpNeeded} onClick={() => setStatus(ConnectCardStatus.FollowUpNeeded)}>
            Follow up
          </StatusButton>
          <StatusButton active={card.status === ConnectCardStatus.FollowedUp} onClick={() => setStatus(ConnectCardStatus.FollowedUp)}>
            Followed up
          </StatusButton>
          <StatusButton active={card.status === ConnectCardStatus.Closed} onClick={() => setStatus(ConnectCardStatus.Closed)}>
            Close
          </StatusButton>
        </div>
      </section>

      <section className="space-y-3">
        <SectionHead number="03" title="Internal notes" />
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          className="min-h-32 w-full border border-border bg-background p-3 text-sm focus-visible:border-accent focus-visible:outline-none"
          placeholder="Notes only visible to editors and administrators…"
        />
        <div>
          <Btn onClick={saveNotes} disabled={savingNotes}>
            {savingNotes ? "Saving…" : "Save notes"}
          </Btn>
        </div>
      </section>
    </div>
  );
}

function Row({ icon, label, children }: { icon?: React.ReactNode; label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-start gap-2">
      {icon && <span aria-hidden className="mt-0.5 text-muted">{icon}</span>}
      <div>
        <dt className="text-xs uppercase tracking-wide text-muted">{label}</dt>
        <dd className="mt-0.5 text-sm">{children}</dd>
      </div>
    </div>
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
