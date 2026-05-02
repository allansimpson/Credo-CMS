import { useEffect, useState } from "react";
import { Plus } from "lucide-react";
import { ResponsiveTable, type ColumnDef } from "@/components/shared/ResponsiveTable";
import { usersApi, type UserListQuery } from "@/lib/api/users";
import type { CreateUserRequest, Role, UserDetail, UserListItem } from "@/types/api";

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
      header: "Name",
      accessor: (u) => u.displayName,
      mobilePriority: 1,
      sortBy: (u) => u.lastName,
    },
    {
      id: "email",
      header: "Email",
      accessor: (u) => u.email,
      mobilePriority: 2,
    },
    {
      id: "roles",
      header: "Roles",
      accessor: (u) => u.roles.join(", ") || "—",
      mobilePriority: 3,
    },
    {
      id: "status",
      header: "Status",
      accessor: (u) => (u.isActive ? "Active" : "Deactivated"),
      mobilePriority: 4,
    },
    {
      id: "lastLogin",
      header: "Last login",
      accessor: (u) => (u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString() : "—"),
    },
  ];

  return (
    <div>
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <h1 className="text-2xl font-bold">Users</h1>
        <button
          type="button"
          onClick={() => setCreateOpen(true)}
          className="inline-flex h-10 items-center gap-2 rounded-md bg-accent px-4 text-sm font-semibold text-accent-foreground hover:bg-accent/90"
        >
          <Plus className="h-4 w-4" />
          New user
        </button>
      </div>

      <div className="mt-4 flex flex-wrap gap-3">
        <select
          value={filterRole}
          onChange={(e) => setFilterRole(e.target.value as Role | "")}
          className="h-10 rounded-md border border-input bg-background px-3 text-sm"
        >
          <option value="">All roles</option>
          {ROLE_OPTIONS.map((r) => <option key={r} value={r}>{r}</option>)}
        </select>
        <select
          value={filterActive}
          onChange={(e) => setFilterActive(e.target.value as "" | "active" | "deactivated")}
          className="h-10 rounded-md border border-input bg-background px-3 text-sm"
        >
          <option value="">All statuses</option>
          <option value="active">Active</option>
          <option value="deactivated">Deactivated</option>
        </select>
      </div>

      {error && (
        <div role="alert" className="mt-4 rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          {error}
        </div>
      )}

      <div className="mt-6">
        {loading ? (
          <p className="text-muted-foreground">Loading…</p>
        ) : (
          <ResponsiveTable
            data={users}
            columns={columns}
            rowKey={(u) => u.id}
            searchPlaceholder="Search name or email…"
            emptyMessage="No users match your filters."
          />
        )}
      </div>

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
          <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
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
