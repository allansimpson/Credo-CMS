import { useState } from "react";
import { Link } from "react-router-dom";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { authApi } from "@/lib/api/auth";

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await authApi.forgotPassword({ email });
    } finally {
      setSubmitting(false);
      setSubmitted(true);
    }
  }

  return (
    <ChurchThemeLayout>
      <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4 py-12">
        {submitted ? (
          <div className="rounded-lg border bg-card p-6 text-center shadow-sm">
            <h1 className="text-xl font-semibold">Check your email</h1>
            <p className="mt-3 text-sm text-muted-foreground">
              If an account exists for that address, we've sent a password-reset link.
            </p>
            <Link to="/login" className="mt-6 inline-block text-sm text-primary hover:underline">
              Back to sign in
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border bg-card p-6 shadow-sm">
            <h1 className="text-xl font-semibold">Reset your password</h1>
            <p className="text-sm text-muted-foreground">
              Enter the email address on your account. We'll send you a reset link.
            </p>

            <div>
              <label htmlFor="email" className="mb-1 block text-sm font-medium">Email</label>
              <input
                id="email"
                type="email"
                required
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              />
            </div>

            <button
              type="submit"
              disabled={submitting}
              className="inline-flex h-10 w-full items-center justify-center rounded-md bg-primary text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              {submitting ? "Sending…" : "Send reset link"}
            </button>
            <div className="text-center text-sm">
              <Link to="/login" className="text-primary hover:underline">Back to sign in</Link>
            </div>
          </form>
        )}
      </main>
    </ChurchThemeLayout>
  );
}
