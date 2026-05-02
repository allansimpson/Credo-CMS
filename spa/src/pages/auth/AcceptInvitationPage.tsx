import { useState } from "react";
import { Link, useSearchParams, useNavigate } from "react-router-dom";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { authApi } from "@/lib/api/auth";

export function AcceptInvitationPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const [email] = useState(searchParams.get("email") ?? "");
  const [token] = useState(searchParams.get("token") ?? "");
  const [newPassword, setNewPassword] = useState("");
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    try {
      await authApi.acceptInvitation({ email, token, newPassword });
      navigate("/login?accepted=1", { replace: true });
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Could not accept the invitation. The link may have expired."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <ChurchThemeLayout>
      <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4 py-12">
        <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border bg-card p-6 shadow-sm">
          <h1 className="text-xl font-semibold">Welcome — set your password</h1>
          <p className="text-sm text-muted-foreground">
            You're accepting an invitation for <strong>{email || "your email"}</strong>.
          </p>

          {errors.length > 0 && (
            <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
              <ul className="list-disc pl-5">
                {errors.map((err) => <li key={err}>{err}</li>)}
              </ul>
            </div>
          )}

          <div>
            <label htmlFor="newPassword" className="mb-1 block text-sm font-medium">Choose a password</label>
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
            <p className="mt-1 text-xs text-muted-foreground">
              Minimum 12 characters with uppercase, lowercase, digit, and a non-alphanumeric character.
            </p>
          </div>

          <button
            type="submit"
            disabled={submitting || !token || !email}
            className="inline-flex h-10 w-full items-center justify-center rounded-md bg-primary text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {submitting ? "Activating…" : "Accept and set password"}
          </button>

          <div className="text-center text-sm">
            <Link to="/login" className="text-primary hover:underline">Already activated? Sign in</Link>
          </div>
        </form>
      </main>
    </ChurchThemeLayout>
  );
}
