import { useState } from "react";
import { Link, useSearchParams, useNavigate } from "react-router-dom";
import { SystemThemeLayout } from "@/themes/SystemThemeLayout";
import { authApi } from "@/lib/api/auth";

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const [email, setEmail] = useState(searchParams.get("email") ?? "");
  const [token] = useState(searchParams.get("token") ?? "");
  const [newPassword, setNewPassword] = useState("");
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    try {
      await authApi.resetPassword({ email, token, newPassword });
      navigate("/login", { replace: true });
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Reset failed. The link may have expired."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <SystemThemeLayout>
      <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4 py-12">
        <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border bg-card p-6 shadow-sm">
          <h1 className="text-xl font-semibold">Choose a new password</h1>

          {errors.length > 0 && (
            <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
              <ul className="list-disc pl-5">
                {errors.map((err) => <li key={err}>{err}</li>)}
              </ul>
            </div>
          )}

          <div>
            <label htmlFor="email" className="mb-1 block text-sm font-medium">Email</label>
            <input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
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
            <p className="mt-1 text-xs text-muted">
              Minimum 12 characters with uppercase, lowercase, digit, and a non-alphanumeric character.
            </p>
          </div>

          <button
            type="submit"
            disabled={submitting || !token}
            className="inline-flex h-10 w-full items-center justify-center rounded-md bg-primary text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {submitting ? "Saving…" : "Set password"}
          </button>
          <div className="text-center text-sm">
            <Link to="/login" className="text-primary hover:underline">Back to sign in</Link>
          </div>
        </form>
      </main>
    </SystemThemeLayout>
  );
}
