import { useState } from "react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { useAuth } from "@/hooks/useAuth";
import { authApi } from "@/lib/api/auth";

export function ProfilePage() {
  const { user, refresh } = useAuth();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  async function handlePasswordChange(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);
    try {
      await authApi.changePassword({ currentPassword, newPassword });
      setCurrentPassword("");
      setNewPassword("");
      setSuccess(true);
      await refresh();
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Password change failed."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  }

  if (!user) return null;

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-10">
          <h1 className="text-2xl font-bold">Your profile</h1>

          <section className="mt-8 rounded-lg border bg-card p-6 shadow-sm">
            <h2 className="text-lg font-semibold">Account details</h2>
            <dl className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <dt className="text-xs uppercase tracking-wide text-muted-foreground">Name</dt>
                <dd className="mt-1 text-sm">{user.displayName}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-muted-foreground">Email</dt>
                <dd className="mt-1 text-sm">{user.email}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase tracking-wide text-muted-foreground">Roles</dt>
                <dd className="mt-1 text-sm">{user.roles.join(", ") || "Member"}</dd>
              </div>
            </dl>
          </section>

          <section className="mt-8 rounded-lg border bg-card p-6 shadow-sm">
            <h2 className="text-lg font-semibold">My calendar feed</h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Subscribe to upcoming events (including members-only) from any
              calendar app.
            </p>
            <a href="/profile/calendar-feed"
              className="mt-3 inline-flex h-10 items-center justify-center border bg-card px-4 text-sm hover:bg-muted">
              Manage feed URL →
            </a>
          </section>

          <section className="mt-8 rounded-lg border bg-card p-6 shadow-sm">
            <h2 className="text-lg font-semibold">My event registrations</h2>
            <p className="mt-2 text-sm text-muted-foreground">View or cancel your upcoming registrations.</p>
            <a href="/profile/registrations"
              className="mt-3 inline-flex h-10 items-center justify-center border bg-card px-4 text-sm hover:bg-muted">
              View registrations →
            </a>
          </section>

          <section className="mt-8 rounded-lg border bg-card p-6 shadow-sm">
            <h2 className="text-lg font-semibold">Change password</h2>
            <form onSubmit={handlePasswordChange} className="mt-4 space-y-4">
              {errors.length > 0 && (
                <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
                  <ul className="list-disc pl-5">
                    {errors.map((err) => <li key={err}>{err}</li>)}
                  </ul>
                </div>
              )}
              {success && (
                <div role="status" className="rounded-md border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300">
                  Password updated.
                </div>
              )}

              <div>
                <label htmlFor="currentPassword" className="mb-1 block text-sm font-medium">Current password</label>
                <input
                  id="currentPassword"
                  type="password"
                  required
                  autoComplete="current-password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                />
              </div>

              <div>
                <label htmlFor="newPassword" className="mb-1 block text-sm font-medium">New password</label>
                <input
                  id="newPassword"
                  type="password"
                  required
                  minLength={12}
                  autoComplete="new-password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                />
              </div>

              <button
                type="submit"
                disabled={submitting}
                className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
              >
                {submitting ? "Saving…" : "Update password"}
              </button>
            </form>
          </section>
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}
