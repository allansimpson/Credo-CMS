import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Search, Trash2, UserPlus } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";
import {
  adminGroupsApi,
  GroupJoinability,
  GroupMembershipStatus,
  GroupVisibility,
  MessageOnJoinRequest,
  RosterVisibility,
  type AdminGroupDetail,
  type AdminMembership,
  type CreateGroupRequest,
  type UpdateGroupRequest,
} from "@/lib/api/groups";
import { usersApi } from "@/lib/api/users";
import type { UserListItem } from "@/types/api";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import { slugify } from "@/lib/slug";
import {
  Btn,
  Chip,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";

interface FormState {
  slug: string;
  name: string;
  descriptionJson: string | null;
  imageBlobUrl: string | null;
  imageWebpBlobUrl: string | null;
  imageAltText: string | null;
  contactEmail: string;
  meetingInfo: string;
  visibility: GroupVisibility;
  joinability: GroupJoinability;
  requiresMessageOnJoinRequest: MessageOnJoinRequest;
  rosterVisibility: RosterVisibility;
  isActive: boolean;
}

const emptyForm: FormState = {
  slug: "",
  name: "",
  descriptionJson: null,
  imageBlobUrl: null,
  imageWebpBlobUrl: null,
  imageAltText: null,
  contactEmail: "",
  meetingInfo: "",
  visibility: GroupVisibility.MembersOnly,
  joinability: GroupJoinability.Open,
  requiresMessageOnJoinRequest: MessageOnJoinRequest.Optional,
  rosterVisibility: RosterVisibility.LeadersOnly,
  isActive: true,
};

type Tab = "details" | "roster" | "pending";

export function GroupEditorPage() {
  const { id } = useParams<{ id?: string }>();
  const isNew = !id || id === "new";
  const navigate = useNavigate();

  const [form, setForm] = useState<FormState>(emptyForm);
  const [original, setOriginal] = useState<AdminGroupDetail | null>(null);
  const [loading, setLoading] = useState(!isNew);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [activeTab, setActiveTab] = useState<Tab>("details");
  const [slugAutoGen, setSlugAutoGen] = useState(isNew);

  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    adminGroupsApi.get(id!)
      .then((g) => {
        if (cancelled) return;
        setOriginal(g);
        setForm({
          slug: g.slug,
          name: g.name,
          descriptionJson: g.descriptionJson,
          imageBlobUrl: g.imageBlobUrl,
          imageWebpBlobUrl: g.imageWebpBlobUrl,
          imageAltText: g.imageAltText,
          contactEmail: g.contactEmail ?? "",
          meetingInfo: g.meetingInfo ?? "",
          visibility: g.visibility,
          joinability: g.joinability,
          requiresMessageOnJoinRequest: g.requiresMessageOnJoinRequest,
          rosterVisibility: g.rosterVisibility,
          isActive: g.isActive,
        });
        setSlugAutoGen(false);
        setLoading(false);
      })
      .catch(() => {
        if (cancelled) return;
        setErrors(["Could not load group."]);
        setLoading(false);
      });
    return () => { cancelled = true; };
  }, [id, isNew]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);

    const body: CreateGroupRequest | UpdateGroupRequest = {
      slug: form.slug,
      name: form.name,
      descriptionJson: form.descriptionJson,
      imageBlobUrl: form.imageBlobUrl,
      imageWebpBlobUrl: form.imageWebpBlobUrl,
      imageAltText: form.imageAltText,
      contactEmail: form.contactEmail || null,
      meetingInfo: form.meetingInfo || null,
      visibility: form.visibility,
      joinability: form.joinability,
      requiresMessageOnJoinRequest: form.requiresMessageOnJoinRequest,
      rosterVisibility: form.rosterVisibility,
      isActive: form.isActive,
    };

    try {
      if (isNew) {
        const created = await adminGroupsApi.create(body);
        navigate(`/admin/groups/${created.id}`);
      } else {
        const updated = await adminGroupsApi.update(id!, body);
        setOriginal(updated);
        setSuccess(true);
      }
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Save failed."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!id) return;
    if (!window.confirm("Soft-delete this group? It will be hidden from public + admin lists.")) return;
    await adminGroupsApi.softDelete(id);
    navigate("/admin/groups");
  };

  if (loading) return <p className="text-muted">Loading…</p>;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={isNew ? "New group" : `Editing · /${form.slug}`}
        title={form.name || "Untitled group"}
        actions={
          !isNew && original && (
            <Btn variant="danger" onClick={handleDelete}>Delete group</Btn>
          )
        }
      />

      {!isNew && (
        <nav className="flex border-b border-border" aria-label="Group sections">
          <TabButton active={activeTab === "details"} onClick={() => setActiveTab("details")}>Details</TabButton>
          <TabButton active={activeTab === "roster"} onClick={() => setActiveTab("roster")}>Roster</TabButton>
          <TabButton active={activeTab === "pending"} onClick={() => setActiveTab("pending")}>Pending requests</TabButton>
        </nav>
      )}

      {activeTab === "details" && (
        <form onSubmit={handleSubmit} className="space-y-6">
          {errors.length > 0 && (
            <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
              <ul className="list-disc pl-5">{errors.map((e) => <li key={e}>{e}</li>)}</ul>
            </div>
          )}
          {success && (
            <div role="status" className="border border-success/30 bg-success/10 p-3 text-sm text-success">
              Saved.
            </div>
          )}

          <section className="space-y-4">
            <SectionHead number="01" title="Identity" />
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label="Name" required>
                <input
                  required
                  value={form.name}
                  onChange={(e) => {
                    setForm({
                      ...form,
                      name: e.target.value,
                      slug: slugAutoGen ? slugify(e.target.value) : form.slug,
                    });
                  }}
                  className="input"
                />
              </Field>
              <Field
                label="Slug"
                required
                hint={slugAutoGen ? "Auto-generating from name." : "Editing manually."}
              >
                <input
                  required
                  value={form.slug}
                  onChange={(e) => { setSlugAutoGen(false); setForm({ ...form, slug: e.target.value }); }}
                  className="input"
                />
              </Field>
            </div>
          </section>

          <section className="space-y-4">
            <SectionHead number="02" title="Visibility & joining" />
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label="Visibility">
                <select
                  value={form.visibility}
                  onChange={(e) => setForm({ ...form, visibility: Number(e.target.value) as GroupVisibility })}
                  className="input"
                >
                  <option value={GroupVisibility.Public}>Public</option>
                  <option value={GroupVisibility.MembersOnly}>Members only</option>
                  <option value={GroupVisibility.Hidden}>Hidden (admin/leader only)</option>
                </select>
              </Field>
              <Field label="Joinability">
                <select
                  value={form.joinability}
                  onChange={(e) => setForm({ ...form, joinability: Number(e.target.value) as GroupJoinability })}
                  className="input"
                >
                  <option value={GroupJoinability.Open}>Open — members can request</option>
                  <option value={GroupJoinability.InviteOnly}>Invite only</option>
                  <option value={GroupJoinability.Closed}>Closed</option>
                </select>
              </Field>
              <Field label="Message on join request">
                <select
                  value={form.requiresMessageOnJoinRequest}
                  onChange={(e) => setForm({ ...form, requiresMessageOnJoinRequest: Number(e.target.value) as MessageOnJoinRequest })}
                  className="input"
                >
                  <option value={MessageOnJoinRequest.Hidden}>No field shown</option>
                  <option value={MessageOnJoinRequest.Optional}>Optional</option>
                  <option value={MessageOnJoinRequest.Required}>Required</option>
                </select>
              </Field>
              <Field label="Roster visibility">
                <select
                  value={form.rosterVisibility}
                  onChange={(e) => setForm({ ...form, rosterVisibility: Number(e.target.value) as RosterVisibility })}
                  className="input"
                >
                  <option value={RosterVisibility.LeadersOnly}>Leaders only</option>
                  <option value={RosterVisibility.AllGroupMembers}>All group members</option>
                </select>
              </Field>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              Group is active (inactive groups are hidden from public lists)
            </label>
          </section>

          <section className="space-y-4">
            <SectionHead number="03" title="Contact & meeting" />
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label="Contact email">
                <input
                  type="email"
                  value={form.contactEmail}
                  maxLength={200}
                  onChange={(e) => setForm({ ...form, contactEmail: e.target.value })}
                  className="input"
                />
              </Field>
              <Field label="Meeting info" hint="Time, place, cadence — single line.">
                <input
                  value={form.meetingInfo}
                  maxLength={500}
                  onChange={(e) => setForm({ ...form, meetingInfo: e.target.value })}
                  className="input"
                />
              </Field>
            </div>
          </section>

          <section className="space-y-4">
            <SectionHead number="04" title="Image" />
            <ImageUpload
              ariaLabel="Group image"
              value={{
                url: form.imageBlobUrl,
                webpUrl: form.imageWebpBlobUrl,
                alt: form.imageAltText,
              }}
              onChange={(next) => setForm({
                ...form,
                imageBlobUrl: next.url,
                imageWebpBlobUrl: next.webpUrl,
                imageAltText: next.alt,
              })}
            />
          </section>

          <section className="space-y-4">
            <SectionHead number="05" title="Description" />
            <TipTapEditor
              ariaLabel="Group description"
              valueJson={form.descriptionJson}
              onChangeJson={(json) => setForm({ ...form, descriptionJson: json })}
              placeholder="Describe what this group is about…"
            />
          </section>

          <div>
            <Btn type="submit" variant="accent" size="lg" disabled={submitting}>
              {submitting ? "Saving…" : isNew ? "Create group" : "Save changes"}
            </Btn>
          </div>

          <Styles />
        </form>
      )}

      {activeTab === "roster" && id && <RosterTab groupId={id} />}
      {activeTab === "pending" && id && <PendingTab groupId={id} />}
    </div>
  );
}

// ---- Roster tab ----------------------------------------------------------

function RosterTab({ groupId }: { groupId: string }) {
  const { hasAnyRole } = useAuth();
  const isAdmin = hasAnyRole(["Administrator"]);
  const [members, setMembers] = useState<AdminMembership[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const m = await adminGroupsApi.listMemberships(groupId, GroupMembershipStatus.Active);
      setMembers(m);
      setError(null);
    } catch {
      setError("Could not load roster.");
    } finally {
      setLoading(false);
    }
  };
  useEffect(() => { void load(); }, [groupId]);

  const handleRemove = async (m: AdminMembership) => {
    if (!window.confirm(`Remove ${m.userDisplayName} from this group?`)) return;
    try {
      await adminGroupsApi.removeMember(groupId, m.userId);
      await load();
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Could not remove member."];
      window.alert(messages.join("; "));
    }
  };

  const handleSetLeader = async (m: AdminMembership, isLeader: boolean) => {
    try {
      await adminGroupsApi.setLeader(groupId, m.userId, isLeader);
      await load();
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Could not change leader status."];
      window.alert(messages.join("; "));
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Active members ({members?.length ?? 0})</h2>
        <Btn iconLeft={<UserPlus className="h-4 w-4" />} onClick={() => setAdding(true)}>
          Add member
        </Btn>
      </div>

      {loading && <p className="text-muted">Loading…</p>}
      {error && <p className="text-danger">{error}</p>}
      {!loading && members && members.length === 0 && (
        <p className="text-muted">No active members yet.</p>
      )}
      {!loading && members && members.length > 0 && (
        <ul className="divide-y border border-border bg-panel">
          {members.map((m) => (
            <li key={m.id} className="flex flex-wrap items-center gap-3 px-5 py-3">
              <div className="min-w-0 flex-1">
                <p className="font-semibold">{m.userDisplayName}</p>
                {m.userEmail && (
                  <p className="font-mono text-xs text-muted">{m.userEmail}</p>
                )}
              </div>
              {m.isLeader && <Chip tone="accent">Leader</Chip>}
              {isAdmin && (
                <button
                  type="button"
                  onClick={() => handleSetLeader(m, !m.isLeader)}
                  className="text-xs font-medium text-primary hover:underline"
                >
                  {m.isLeader ? "Demote" : "Promote to leader"}
                </button>
              )}
              <Btn
                size="sm"
                variant="danger"
                iconLeft={<Trash2 className="h-3.5 w-3.5" />}
                onClick={() => handleRemove(m)}
              >
                Remove
              </Btn>
            </li>
          ))}
        </ul>
      )}

      {adding && (
        <AddMemberDialog
          groupId={groupId}
          isAdmin={isAdmin}
          onClose={() => setAdding(false)}
          onAdded={async () => { setAdding(false); await load(); }}
        />
      )}
    </div>
  );
}

function AddMemberDialog({
  groupId, isAdmin, onClose, onAdded,
}: {
  groupId: string;
  isAdmin: boolean;
  onClose: () => void;
  onAdded: () => void;
}) {
  const [search, setSearch] = useState("");
  const [results, setResults] = useState<UserListItem[]>([]);
  const [makeLeader, setMakeLeader] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    if (!search.trim()) { setResults([]); return; }
    const handle = window.setTimeout(() => {
      usersApi
        .list({ search: search.trim(), pageSize: 20 })
        .then((d) => { if (!cancelled) setResults(d.items); })
        .catch(() => { if (!cancelled) setResults([]); });
    }, 200);
    return () => { cancelled = true; window.clearTimeout(handle); };
  }, [search]);

  const handleAdd = async (userId: string) => {
    setSubmitting(true); setError(null);
    try {
      await adminGroupsApi.addMember(groupId, { userId, isLeader: makeLeader });
      onAdded();
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Could not add member."];
      setError(messages.join("; "));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-40 flex items-center justify-center bg-foreground/40 p-4">
      <div className="w-full max-w-lg space-y-4 rounded-lg bg-background p-6 shadow-xl">
        <h2 className="text-lg font-semibold">Add member</h2>

        {error && (
          <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
            {error}
          </div>
        )}

        <div className="relative">
          <Search aria-hidden className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted" />
          <input
            type="search"
            autoFocus
            placeholder="Search by name or email…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="h-10 w-full rounded-md border border-input bg-background pl-9 pr-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          />
        </div>

        {isAdmin && (
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={makeLeader}
              onChange={(e) => setMakeLeader(e.target.checked)}
            />
            Add as leader
          </label>
        )}

        <ul className="max-h-72 divide-y overflow-y-auto rounded-md border bg-card">
          {results.length === 0 && (
            <li className="p-3 text-sm text-muted">{search ? "No matches." : "Type to search."}</li>
          )}
          {results.map((u) => (
            <li key={u.id} className="flex items-center gap-3 p-3 text-sm">
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium">{u.displayName}</p>
                <p className="truncate font-mono text-xs text-muted">{u.email}</p>
              </div>
              <button
                type="button"
                disabled={submitting}
                onClick={() => handleAdd(u.id)}
                className="inline-flex h-8 items-center justify-center bg-primary px-3 text-xs font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
              >
                Add
              </button>
            </li>
          ))}
        </ul>

        <div className="flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="inline-flex h-10 items-center justify-center rounded-md border bg-background px-4 text-sm"
          >
            Done
          </button>
        </div>
      </div>
    </div>
  );
}

// ---- Pending tab ---------------------------------------------------------

function PendingTab({ groupId }: { groupId: string }) {
  const [pending, setPending] = useState<AdminMembership[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const m = await adminGroupsApi.listMemberships(groupId, GroupMembershipStatus.Pending);
      setPending(m);
      setError(null);
    } catch {
      setError("Could not load pending requests.");
    } finally {
      setLoading(false);
    }
  };
  useEffect(() => { void load(); }, [groupId]);

  const handleApprove = async (m: AdminMembership) => {
    try { await adminGroupsApi.approve(m.id); await load(); }
    catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Could not approve."];
      window.alert(messages.join("; "));
    }
  };

  const handleDecline = async (m: AdminMembership) => {
    if (!window.confirm(`Decline ${m.userDisplayName}'s request?`)) return;
    try { await adminGroupsApi.decline(m.id); await load(); }
    catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Could not decline."];
      window.alert(messages.join("; "));
    }
  };

  return (
    <div className="space-y-4">
      <h2 className="text-lg font-semibold">
        Pending requests ({pending?.length ?? 0})
      </h2>

      {loading && <p className="text-muted">Loading…</p>}
      {error && <p className="text-danger">{error}</p>}
      {!loading && pending && pending.length === 0 && (
        <p className="text-muted">No pending requests.</p>
      )}
      {!loading && pending && pending.length > 0 && (
        <ul className="divide-y border border-border bg-panel">
          {pending.map((m) => (
            <li key={m.id} className="space-y-2 px-5 py-4">
              <div className="flex flex-wrap items-baseline gap-2">
                <p className="font-semibold">{m.userDisplayName}</p>
                {m.userEmail && (
                  <span className="font-mono text-xs text-muted">{m.userEmail}</span>
                )}
                {m.requestedAt && (
                  <span className="ml-auto font-mono text-xs text-muted">
                    {new Date(m.requestedAt).toLocaleString()}
                  </span>
                )}
              </div>
              {m.joinRequestMessage && (
                <p className="border-l-2 border-accent bg-panel-alt px-3 py-2 text-sm italic">
                  "{m.joinRequestMessage}"
                </p>
              )}
              <div className="flex flex-wrap gap-2">
                <Btn variant="accent" size="sm" onClick={() => handleApprove(m)}>
                  Approve
                </Btn>
                <Btn size="sm" onClick={() => handleDecline(m)}>
                  Decline
                </Btn>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

// ---- bits ----------------------------------------------------------------

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

function Field({
  label, hint, required, children,
}: { label: string; hint?: string; required?: boolean; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium">
        {label}{required && <span className="text-danger"> *</span>}
      </span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted">{hint}</span>}
    </label>
  );
}

function Styles() {
  return (
    <style>{`
      .input {
        height: 2.5rem;
        width: 100%;
        border: 1px solid hsl(var(--border));
        background: hsl(var(--background));
        padding: 0 0.75rem;
        font-size: 0.875rem;
      }
      .input:focus { outline: none; border-color: hsl(var(--accent)); }
    `}</style>
  );
}
