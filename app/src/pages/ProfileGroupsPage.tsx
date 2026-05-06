import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, Users } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import {
  GroupMembershipStatus,
  profileGroupsApi,
  type ProfileMembership,
} from "@/lib/api/groups";

export function ProfileGroupsPage() {
  const [memberships, setMemberships] = useState<ProfileMembership[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const m = await profileGroupsApi.listMine();
      setMemberships(m);
      setError(null);
    } catch {
      setError("Could not load your groups.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  const handleLeave = async (groupId: string, groupName: string) => {
    if (!window.confirm(`Leave ${groupName}?`)) return;
    try {
      await profileGroupsApi.leave(groupId);
      await load();
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Could not leave the group."];
      window.alert(messages.join("; "));
    }
  };

  const active = memberships?.filter((m) => m.status === GroupMembershipStatus.Active) ?? [];
  const pending = memberships?.filter((m) => m.status === GroupMembershipStatus.Pending) ?? [];

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-10">
          <Link
            to="/profile"
            className="mb-6 inline-flex items-center gap-1 text-sm text-muted hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" /> Back to profile
          </Link>

          <header className="border-b pb-6">
            <h1 className="flex items-center gap-2 text-2xl font-bold">
              <Users className="h-6 w-6" /> My groups
            </h1>
            <p className="mt-1 text-sm text-muted">
              Groups you've joined or have a pending request for.
            </p>
          </header>

          <section className="mt-6">
            {loading && <p className="text-muted">Loading…</p>}
            {error && <p className="text-danger">{error}</p>}

            {!loading && !error && (
              <>
                <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">Active</h2>
                {active.length === 0 ? (
                  <p className="mt-2 text-sm text-muted">
                    You're not in any groups yet.{" "}
                    <Link to="/get-involved" className="text-primary hover:underline">Browse groups</Link>.
                  </p>
                ) : (
                  <ul className="mt-2 divide-y rounded-lg border bg-card">
                    {active.map((m) => (
                      <li key={m.groupId} className="flex flex-wrap items-center gap-3 p-4">
                        <div className="min-w-0 flex-1">
                          <Link to={`/groups/${m.groupSlug}`} className="font-semibold hover:underline">
                            {m.groupName}
                          </Link>
                          {m.isLeader && (
                            <span className="ml-2 rounded bg-accent/15 px-2 py-0.5 text-[11px] font-medium uppercase tracking-wider text-accent">
                              Leader
                            </span>
                          )}
                          {m.joinedAt && (
                            <p className="text-xs text-muted">Joined {new Date(m.joinedAt).toLocaleDateString()}</p>
                          )}
                        </div>
                        <button
                          type="button"
                          onClick={() => handleLeave(m.groupId, m.groupName)}
                          className="inline-flex h-9 items-center justify-center border border-danger/30 bg-card px-3 text-sm text-danger hover:bg-danger/10"
                        >
                          Leave
                        </button>
                      </li>
                    ))}
                  </ul>
                )}

                {pending.length > 0 && (
                  <>
                    <h2 className="mt-8 text-sm font-semibold uppercase tracking-wide text-muted">Pending</h2>
                    <ul className="mt-2 divide-y rounded-lg border bg-card">
                      {pending.map((m) => (
                        <li key={m.groupId} className="flex flex-wrap items-center gap-3 p-4">
                          <div className="min-w-0 flex-1">
                            <Link to={`/groups/${m.groupSlug}`} className="font-semibold hover:underline">
                              {m.groupName}
                            </Link>
                            {m.requestedAt && (
                              <p className="text-xs text-muted">Requested {new Date(m.requestedAt).toLocaleDateString()}</p>
                            )}
                          </div>
                          <button
                            type="button"
                            onClick={() => handleLeave(m.groupId, m.groupName)}
                            className="inline-flex h-9 items-center justify-center border bg-card px-3 text-sm hover:bg-panel-alt"
                          >
                            Cancel request
                          </button>
                        </li>
                      ))}
                    </ul>
                  </>
                )}
              </>
            )}
          </section>
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}
