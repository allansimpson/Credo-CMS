import { useEffect, useMemo, useState } from "react";
import { Plus } from "lucide-react";
import { ResponsiveTable, type ColumnDef } from "@/components/shared/ResponsiveTable";
import { usersApi, type UserListQuery } from "@/lib/api/users";
import type { CreateUserRequest, Role, UserDetail, UserListItem } from "@/types/api";
import {
  Avatar,
  BigNum,
  Btn,
  Chip,
  PageHeader,
  SectionHead,
} from "@/components/shared/admin/EditorialPrimitives";

const ROLE_OPTIONS: Role[] = ["Administrator", "Editor", "Member"];

export function UsersPage() {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterRole, setFilterRole] = useState<Role | "">("");
  const [filterActive, setFilterActive] = useState<"" | "active" | "deactivated">("");
  const [createOpen, setCreateOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const query: UserListQuery = {};
      if (filterRole) query.role = filterRole;
      if (filterActive) query.isActive = filterActive === "active";
      const result = await usersApi.list(query);
      setUsers(result.items);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to load users.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, [filterRole, filterActive]);

  const columns: ColumnDef<UserListItem>[] = [
    {
      id: "name",
      header: "Person",
      accessor: (u) => (
        <div className="flex items-center gap-3">
          <Avatar name={u.displayName} size="sm" tone={u.roles.includes("Administrator") ? "accent" : "muted"} />
          <div className="min-w-0">
            <p className="truncate font-medium">{u.displayName}</p>
            <p className="truncate font-mono text-[11px] text-muted">{u.email}</p>
          </div>
        </div>
      ),
      mobilePriority: 1,
      sortBy: (u) => u.lastName,
    },
    {
      id: "roles",
      header: "Role",
      accessor: (u) => (
        <div className="flex flex-wrap gap-1">
          {u.roles.length === 0 ? (
            <span className="text-muted">—</span>
          ) : (
            u.roles.map((r) => (
              <Chip
                key={r}
                tone={r === "Administrator" ? "accent" : r === "Editor" ? "success" : "muted"}
              >
                {r}
              </Chip>
            ))
          )}
        </div>
      ),
      mobilePriority: 3,
    },
    {
      id: "status",
      header: "Status",
      accessor: (u) =>
        u.isActive
          ? <Chip tone="success" dot>Active</Chip>
          : <Chip tone="warn" dot>Deactivated</Chip>,
      mobilePriority: 4,
    },
    {
      id: "lastLogin",
      header: "Last active",
      accessor: (u) => (
        <span style={{ fontVariantNumeric: "tabular-nums" }} className="font-mono text-xs">
          {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString() : "—"}
        </span>
      ),
    },
  ];

  const roleCounts = useMemo(() => {
    const counts: Record<string, number> = { Administrator: 0, Editor: 0, Member: 0, Other: 0 };
    for (const u of users) {
      if (u.roles.length === 0) counts.Other += 1;
      for (const r of u.roles) counts[r] = (counts[r] ?? 0) + 1;
    }
    return counts;
  }, [users]);

  const ROLE_DESCRIPTIONS: Record<Role, string> = {
    Administrator: "Full access to settings, users, and content.",
    Editor: "Compose and publish content. No user management.",
    Member: "Read members-only content; no editing rights.",
  };

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow={`${users.length} accounts`}
        title="Users"
        kicker="staff & members with sign-in access"
        actions={
          <Btn
            variant="accent"
            size="lg"
            iconLeft={<Plus className="h-4 w-4" />}
            onClick={() => setCreateOpen(true)}
          >
            Invite member
          </Btn>
        }
      />

      <section className="space-y-4">
        <SectionHead number="01" title="Roles in use" />
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          {ROLE_OPTIONS.map((r, i) => {
            const isOwner = r === "Administrator";
            return (
              <article
                key={r}
                className="relative border border-border bg-panel p-5"
              >
                {isOwner && (
                  <span aria-hidden className="absolute inset-y-0 left-0 w-[3px] bg-accent" />
                )}
                <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-muted">
                  Role {String(i + 1).padStart(2, "0")}
                </p>
                <h3 className="mt-1 font-heading text-lg font-semibold">{r}</h3>
                <BigNum size="md" className="mt-3">
                  {String(roleCounts[r] ?? 0).padStart(2, "0")}
                </BigNum>
                <p className="mt-3 text-xs text-fg-soft">{ROLE_DESCRIPTIONS[r]}</p>
              </article>
            );
          })}
        </div>
      </section>

      <section className="space-y-4">
        <SectionHead
          number="02"
          title="Members"
          right={
            <>
              <select
                value={filterRole}
                onChange={(e) => setFilterRole(e.target.value as Role | "")}
                className="h-8 border border-border bg-background px-2 text-xs focus-visible:border-accent focus-visible:outline-none"
              >
                <option value="">All roles</option>
                {ROLE_OPTIONS.map((r) => <option key={r} value={r}>{r}</option>)}
              </select>
              <select
                value={filterActive}
                onChange={(e) => setFilterActive(e.target.value as "" | "active" | "deactivated")}
                className="h-8 border border-border bg-background px-2 text-xs focus-visible:border-accent focus-visible:outline-none"
              >
                <option value="">All statuses</option>
                <option value="active">Active</option>
                <option value="deactivated">Deactivated</option>
              </select>
            </>
          }
        />

        {error && (
          <div role="alert" className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
            {error}
          </div>
        )}

        {loading ? (
          <p className="text-muted">Loading…</p>
        ) : (
          <ResponsiveTable
            data={users}
            columns={columns}
            rowKey={(u) => u.id}
            searchPlaceholder="Search name or email…"
            emptyMessage="No users match your filters."
          />
        )}
      </section>

      {createOpen && <CreateUserDialog onClose={() => setCreateOpen(false)} onCreated={() => { setCreateOpen(false); load(); }} />}
    </div>
  );
}

function CreateUserDialog({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: (user: UserDetail) => void;
}) {
  const [email, setEmail] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [roles, setRoles] = useState<Role[]>(["Member"]);
  const [sendInvitation, setSendInvitation] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    try {
      const req: CreateUserRequest = { email, firstName, lastName, roles, sendInvitation };
      const created = await usersApi.create(req);
      onCreated(created);
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Failed to create user."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-40 flex items-center justify-center bg-foreground/40 p-4">
      <form onSubmit={handleSubmit} className="w-full max-w-lg space-y-4 rounded-lg bg-background p-6 shadow-xl">
        <h2 className="text-lg font-semibold">Create user</h2>

        {errors.length > 0 && (
          <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
            <ul className="list-disc pl-5">
              {errors.map((err) => <li key={err}>{err}</li>)}
            </ul>
          </div>
        )}

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label htmlFor="firstName" className="mb-1 block text-sm font-medium">First name</label>
            <input id="firstName" required value={firstName} onChange={(e) => setFirstName(e.target.value)} className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm" />
          </div>
          <div>
            <label htmlFor="lastName" className="mb-1 block text-sm font-medium">Last name</label>
            <input id="lastName" required value={lastName} onChange={(e) => setLastName(e.target.value)} className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm" />
          </div>
        </div>

        <div>
          <label htmlFor="email" className="mb-1 block text-sm font-medium">Email</label>
          <input id="email" required type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm" />
        </div>

        <fieldset>
          <legend className="mb-1 block text-sm font-medium">Roles</legend>
          <div className="flex flex-wrap gap-3">
            {ROLE_OPTIONS.map((role) => (
              <label key={role} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={roles.includes(role)}
                  onChange={(e) => setRoles((prev) => e.target.checked ? [...prev, role] : prev.filter((r) => r !== role))}
                />
                {role}
              </label>
            ))}
          </div>
        </fieldset>

        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={sendInvitation} onChange={(e) => setSendInvitation(e.target.checked)} />
          Send invitation email
        </label>

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button type="button" onClick={onClose} className="inline-flex h-10 items-center justify-center rounded-md border bg-background px-4 text-sm">Cancel</button>
          <button type="submit" disabled={submitting} className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50">
            {submitting ? "Creating…" : "Create user"}
          </button>
        </div>
      </form>
    </div>
  );
}
