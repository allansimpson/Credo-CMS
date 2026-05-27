import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { Church } from "lucide-react";
import { SystemThemeLayout } from "@/themes/SystemThemeLayout";
import { useAuth } from "@/hooks/useAuth";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import {
  Btn,
  Field,
  MetaLabel,
} from "@/components/shared/admin/EditorialPrimitives";

export function LoginPage() {
  const { login } = useAuth();
  const { settings } = useSiteSettings();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    const result = await login(email, password);
    setSubmitting(false);
    if (result.ok) {
      const returnTo = searchParams.get("return") ?? "/";
      navigate(returnTo, { replace: true });
    } else {
      setErrors(result.errors);
    }
  }

  // TODO: wire endpoint — pull current sermon pull-quote from
  // GET /api/admin/dashboard/this-sunday once the dashboard endpoint lands.
  const pullQuote = {
    quote: "Faith is the substance of things hoped for, the evidence of things not seen.",
    attribution: "Hebrews 11:1 · This Sunday",
  };

  return (
    <SystemThemeLayout>
      <main className="grid min-h-screen bg-background text-foreground lg:grid-cols-2">
        {/* Left column — dark sidebar bg with pull-quote */}
        <aside className="relative hidden flex-col justify-between bg-sidebar p-10 text-background lg:flex">
          {/* Logo lockup */}
          <div className="flex items-center gap-3">
            {settings?.logoUrl ? (
              <img
                src={settings.logoUrl}
                alt=""
                className="h-8 w-8 object-cover"
              />
            ) : (
              <span
                aria-hidden
                className="grid h-8 w-8 place-items-center bg-accent text-accent-foreground"
              >
                <Church className="h-4 w-4" />
              </span>
            )}
            <div className="leading-tight">
              <div className="font-heading text-base font-semibold text-background">
                {settings?.churchName ?? "Credo CMS"}
              </div>
              <div className="text-[11px] uppercase tracking-[0.18em] text-background/60">
                Credo Workbench
              </div>
            </div>
          </div>

          {/* Pull-quote */}
          <blockquote className="max-w-md">
            <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-accent">
              This week's pull-quote
            </p>
            <p className="mt-4 font-heading text-[28px] font-semibold leading-tight tracking-tight text-background">
              "{pullQuote.quote}"
            </p>
            <footer className="mt-3 text-sm text-background/70">
              {pullQuote.attribution}
            </footer>
          </blockquote>

          {/* Footer row */}
          <div className="flex items-center justify-between font-mono text-[11px] text-background/60">
            <span>v1.0 · admin</span>
            <span>{settings?.churchName ?? "credo-cms"}</span>
          </div>
        </aside>

        {/* Right column — form */}
        <section className="flex flex-col justify-center bg-panel px-6 py-12">
          <div className="mx-auto w-full max-w-[360px]">
            <MetaLabel>Member sign-in</MetaLabel>
            <h1 className="mt-3 font-heading text-[42px] font-bold leading-none tracking-[-0.025em]">
              Welcome back.
            </h1>
            <p className="mt-3 text-sm text-fg-soft">
              Sign in to manage your church's content. If you don't have an
              account, an administrator will invite you by email.
            </p>

            <form onSubmit={handleSubmit} className="mt-8 space-y-5">
              {errors.length > 0 && (
                <div
                  role="alert"
                  className="border border-danger/30 bg-danger/10 p-3 text-sm text-danger"
                >
                  <ul className="list-disc pl-5">
                    {errors.map((err) => <li key={err}>{err}</li>)}
                  </ul>
                </div>
              )}

              <Field label="Email" htmlFor="email" required>
                <input
                  id="email"
                  name="email"
                  type="email"
                  required
                  autoComplete="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="h-10 w-full border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-accent"
                />
              </Field>

              <Field
                label="Password"
                htmlFor="password"
                required
                hint={
                  <Link
                    to="/forgot-password"
                    className="text-accent hover:underline"
                  >
                    Forgot password?
                  </Link>
                }
              >
                <input
                  id="password"
                  name="password"
                  type="password"
                  required
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="h-10 w-full border border-border bg-background px-3 text-sm focus-visible:border-accent focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-accent"
                />
              </Field>

              <Btn
                type="submit"
                variant="accent"
                size="lg"
                disabled={submitting}
                className="w-full"
              >
                {submitting ? "Signing in…" : "Sign in"}
              </Btn>
            </form>

            {settings?.facebookLoginEnabled && (
              <div className="mt-5 border-t border-border-soft pt-5">
                <a
                  href="/api/auth/facebook/sign-in-challenge"
                  className="flex h-10 w-full items-center justify-center border border-border bg-card px-4 text-sm font-medium hover:bg-panel-alt"
                >
                  Continue with Facebook
                </a>
                <p className="mt-2 text-[11px] text-muted">
                  Only works for accounts already linked to Facebook from your profile.
                </p>
              </div>
            )}

            <p className="mt-8 border-t border-border-soft pt-5 text-xs text-muted">
              Need an account? Administrators invite you by email.
            </p>
          </div>
        </section>
      </main>
    </SystemThemeLayout>
  );
}
