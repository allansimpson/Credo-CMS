import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import {
  ArrowRight,
  Check,
  Church,
  Clock,
  Eye,
  EyeOff,
  Loader2,
  Lock,
  Mail,
  ShieldAlert,
  ShieldCheck,
} from "lucide-react";
import { SystemThemeLayout } from "@/themes/SystemThemeLayout";
import { useAuth } from "@/hooks/useAuth";
import { authApi, type InvitationPreview } from "@/lib/api/auth";

const MIN_LEN = 14;
const SUCCESS_REDIRECT_DELAY_MS = 1100;
const BREACH_CHECK_DEBOUNCE_MS = 350;
const HIBP_TIMEOUT_MS = 4500;

type BreachState = "idle" | "checking" | "safe" | "breached" | "unavailable";

export function AcceptInvitationPage() {
  const [searchParams] = useSearchParams();
  const email = searchParams.get("email") ?? "";
  const token = searchParams.get("token") ?? "";

  // ── Preview load ───────────────────────────────────────────────────────
  const [preview, setPreview] = useState<InvitationPreview | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setPreview(null);
    setPreviewError(null);
    if (!email || !token) {
      // No query params at all → treat as the Invalid state directly.
      setPreview({
        status: "Invalid",
        firstName: null, lastName: null, email: null,
        role: null, invitedBy: null, churchName: null,
        churchInitials: null, credentialNumber: null, expiresAt: null,
      });
      return () => { cancelled = true; };
    }
    authApi
      .invitationPreview(email, token)
      .then((p) => { if (!cancelled) setPreview(p); })
      .catch(() => { if (!cancelled) setPreviewError("Couldn't reach the server. Refresh to retry."); });
    return () => { cancelled = true; };
  }, [email, token]);

  // ── Loading skeleton ───────────────────────────────────────────────────
  if (!preview && !previewError) {
    return (
      <SystemThemeLayout>
        <main className="grid min-h-screen place-items-center bg-background text-foreground">
          <div className="flex items-center gap-3 text-sm text-muted">
            <Loader2 strokeWidth={1.5} className="h-4 w-4 animate-spin" />
            Loading invitation…
          </div>
        </main>
      </SystemThemeLayout>
    );
  }

  if (previewError) {
    return (
      <SystemThemeLayout>
        <main className="grid min-h-screen place-items-center bg-background px-4 text-foreground">
          <div className="max-w-md text-center">
            <p className="font-heading text-2xl font-semibold">Something went wrong.</p>
            <p className="mt-3 text-sm text-muted">{previewError}</p>
            <Link to="/login" className="mt-6 inline-block text-sm font-semibold text-accent hover:underline">
              Go to sign in →
            </Link>
          </div>
        </main>
      </SystemThemeLayout>
    );
  }

  // ── Branch on preview status ───────────────────────────────────────────
  const p = preview!;
  if (p.status === "Invalid") {
    return <InvalidScreen />;
  }

  // Valid + Consumed + Expired all share the two-column ink shell.
  return <CredentialShell preview={p} email={email} token={token} />;
}

// ───────────────────────────────────────────────────────────────────────────
// Shared two-column shell for Valid / Consumed / Expired
// ───────────────────────────────────────────────────────────────────────────

function CredentialShell({
  preview,
  email,
  token,
}: {
  preview: InvitationPreview;
  email: string;
  token: string;
}) {
  return (
    <SystemThemeLayout>
      <main className="grid min-h-screen bg-background text-foreground md:grid-cols-[1.04fr_1fr]">
        <CredentialFace preview={preview} />
        <section className="flex flex-col justify-center bg-panel px-6 py-12 md:px-12 lg:px-16">
          <div className="mx-auto w-full max-w-[400px]">
            {preview.status === "Valid" && (
              <ValidForm preview={preview} email={email} token={token} />
            )}
            {preview.status === "Consumed" && <ConsumedRightPanel preview={preview} />}
            {preview.status === "Expired" && <ExpiredRightPanel preview={preview} />}
          </div>
        </section>
      </main>
    </SystemThemeLayout>
  );
}

// ───────────────────────────────────────────────────────────────────────────
// Left ink credential face (Valid / Consumed / Expired share this)
// ───────────────────────────────────────────────────────────────────────────

function CredentialFace({ preview }: { preview: InvitationPreview }) {
  const firstName = preview.firstName ?? "";
  const lastName = preview.lastName ?? "";
  const fullName = `${firstName} ${lastName}`.trim() || "Member";
  const role = preview.role ?? "Member";
  const invitedBy = preview.invitedBy ?? "An administrator";
  const churchName = preview.churchName ?? "Credo CMS";
  const credentialNumber = preview.credentialNumber ?? "CMS-000000";

  return (
    <aside className="flex flex-col justify-between gap-12 bg-sidebar px-8 py-10 text-background md:px-12 md:py-14 lg:px-14 lg:py-16">
      {/* Header: church mark + credential NO. */}
      <header className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <span aria-hidden className="grid h-9 w-9 place-items-center bg-accent text-accent-foreground">
            <Church strokeWidth={1.5} className="h-4 w-4" />
          </span>
          <div className="leading-tight">
            <div className="font-heading text-[14.5px] font-semibold tracking-[-0.01em] text-background">
              {churchName}
            </div>
            <div className="mt-0.5 text-[9.5px] font-semibold uppercase tracking-[0.16em] text-background/55">
              Member portal
            </div>
          </div>
        </div>
        <span className="font-mono text-[10.5px] tracking-[0.08em] text-background/55">
          NO. {credentialNumber}
        </span>
      </header>

      {/* Identity block */}
      <div>
        <Eyebrow>Membership credential</Eyebrow>
        <div className="mt-5 text-[10.5px] font-semibold uppercase tracking-[0.16em] text-background/55">
          Issued to
        </div>
        <div className="mt-1.5 font-heading text-[30px] font-semibold leading-tight tracking-[-0.02em] text-background">
          {fullName}
        </div>
        <div className="mt-1 font-mono text-[12px] text-background/55">
          {preview.email ?? ""}
        </div>
        <div className="mt-6 flex gap-9">
          <CredMeta label="Role">{role}</CredMeta>
          <CredMeta label="Invited by">{invitedBy}</CredMeta>
        </div>
      </div>

      {/* Status / countdown */}
      <div className="border-t border-dashed border-background/25 pt-5">
        {preview.status === "Expired" ? (
          <StatusStamp tone="danger" label="Invitation lapsed" sub="This link has expired." />
        ) : preview.status === "Consumed" ? (
          <StatusStamp tone="success" label="Account activated" sub="Already in good standing." />
        ) : (
          <CountdownBlock expiresAt={preview.expiresAt} />
        )}
      </div>
    </aside>
  );
}

function Eyebrow({ children, tone = "accent" }: { children: React.ReactNode; tone?: "accent" | "muted" | "success" }) {
  const toneClass = tone === "muted"
    ? "text-background/55"
    : tone === "success"
      ? "text-success"
      : "text-accent";
  return (
    <div className={`inline-flex items-center gap-3 text-[11px] font-semibold uppercase tracking-[0.2em] ${toneClass}`}>
      <span aria-hidden className="h-[2px] w-[26px] bg-current" />
      {children}
    </div>
  );
}

function CredMeta({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="text-[9.5px] font-semibold uppercase tracking-[0.16em] text-background/55">{label}</div>
      <div className="mt-1.5 text-[15px] text-background">{children}</div>
    </div>
  );
}

function StatusStamp({ tone, label, sub }: { tone: "danger" | "success"; label: string; sub: string }) {
  const color = tone === "danger" ? "text-danger" : "text-success";
  return (
    <div className="flex gap-3.5">
      <span aria-hidden className={`w-[3px] self-stretch ${tone === "danger" ? "bg-danger" : "bg-success"}`} />
      <div>
        <div className={`text-[13.5px] font-semibold ${color}`}>{label}</div>
        <div className="mt-1 font-mono text-[11.5px] text-background/55">{sub}</div>
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
// Adaptive countdown
// ───────────────────────────────────────────────────────────────────────────

function CountdownBlock({ expiresAt }: { expiresAt: string | null }) {
  const remaining = useCountdown(expiresAt);
  const urgent = remaining.days < 1;
  const label = urgent
    ? "Expires in under a day — activate now"
    : `Valid for ${remaining.days} more day${remaining.days === 1 ? "" : "s"} — activate before it lapses`;

  return (
    <>
      <div
        aria-live="polite"
        className={`mb-4 text-[9.5px] font-semibold uppercase tracking-[0.18em] ${urgent ? "text-accent" : "text-background/55"}`}
      >
        {label}
      </div>
      {urgent ? (
        <div className="flex items-end gap-5">
          <BigUnit value={pad(remaining.hours)} label="Hrs" urgent />
          <Colon urgent />
          <BigUnit value={pad(remaining.minutes)} label="Min" urgent />
          <Colon urgent />
          <BigUnit value={pad(remaining.seconds)} label="Sec" urgent />
        </div>
      ) : (
        <div className="flex items-start gap-7">
          <BigUnit value={String(remaining.days)} label={remaining.days === 1 ? "Day" : "Days"} />
          <span aria-hidden className="mt-1 mb-5 w-px self-stretch bg-background/25" />
          <div className="flex items-end gap-5">
            <BigUnit value={pad(remaining.hours)} label="Hrs" />
            <Colon />
            <BigUnit value={pad(remaining.minutes)} label="Min" />
            <Colon />
            <BigUnit value={pad(remaining.seconds)} label="Sec" />
          </div>
        </div>
      )}
    </>
  );
}

function BigUnit({ value, label, urgent }: { value: string; label: string; urgent?: boolean }) {
  return (
    <div className="flex flex-col items-center gap-1.5">
      <span className={`font-heading text-[52px] font-bold leading-none tracking-[-0.04em] tabular-nums ${urgent ? "text-accent" : "text-background"}`}>
        {value}
      </span>
      <span className="text-[9.5px] font-semibold uppercase tracking-[0.16em] text-background/55">
        {label}
      </span>
    </div>
  );
}

function Colon({ urgent }: { urgent?: boolean }) {
  return (
    <span aria-hidden className={`self-start font-heading text-[42px] font-light leading-none ${urgent ? "text-accent/50" : "text-background/30"}`}>
      :
    </span>
  );
}

function pad(n: number) { return String(n).padStart(2, "0"); }

function useCountdown(expiresAt: string | null) {
  // Anchor target to a single Date on mount; ticking updates the diff.
  const target = useMemo(() => expiresAt ? new Date(expiresAt).getTime() : 0, [expiresAt]);
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    if (!target) return;
    const id = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(id);
  }, [target]);
  const diff = Math.max(0, target - now);
  const days = Math.floor(diff / 86_400_000);
  const hours = Math.floor((diff % 86_400_000) / 3_600_000);
  const minutes = Math.floor((diff % 3_600_000) / 60_000);
  const seconds = Math.floor((diff % 60_000) / 1000);
  return { days, hours, minutes, seconds };
}

// ───────────────────────────────────────────────────────────────────────────
// Valid → set-password form
// ───────────────────────────────────────────────────────────────────────────

function ValidForm({
  preview,
  email,
  token,
}: {
  preview: InvitationPreview;
  email: string;
  token: string;
}) {
  const navigate = useNavigate();
  const { refresh } = useAuth();
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const lenOk = password.length >= MIN_LEN;
  const match = confirm.length > 0 && confirm === password;
  const breach = useBreachCheck(password);
  const strength = useMemo(() => strengthScore(password), [password]);

  // Submit gating per the handoff: length + match + NOT positively breached
  // + NOT mid-check. A pending check would block; a timed-out / unavailable
  // check must NOT block (server is canonical).
  const canSubmit = lenOk && match && breach !== "breached" && breach !== "checking";

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit || submitting) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      await authApi.acceptInvitation({ email, token, newPassword: password });
      // Backend signed us in. Refresh client auth state so the portal sees us.
      await refresh();
      setDone(true);
      window.setTimeout(() => navigate("/members", { replace: true }), SUCCESS_REDIRECT_DELAY_MS);
    } catch (err) {
      const messages = extractApiErrors(err);
      setSubmitError(messages[0] ?? "We couldn't save your password. Try again.");
      setSubmitting(false);
    }
  };

  if (done) {
    return <SuccessRedirectPanel firstName={preview.firstName} churchName={preview.churchName} email={preview.email} />;
  }

  return (
    <form onSubmit={onSubmit} noValidate>
      <Eyebrow tone="accent">Claim your account</Eyebrow>
      <h1 className="mt-3.5 font-heading text-[34px] font-bold leading-[1.05] tracking-[-0.025em] text-foreground">
        Set your password.
      </h1>
      <p className="mt-2 text-[13.5px] leading-[1.55] text-muted">
        This is a single-use invitation. Choosing a password confirms the credential and signs you in.
      </p>

      <div className="mt-6 flex flex-col gap-4">
        {submitError && (
          <div role="alert" className="flex gap-3 border border-danger/40 bg-danger/10 px-3.5 py-3">
            <ShieldAlert strokeWidth={1.5} className="mt-0.5 h-4 w-4 shrink-0 text-danger" />
            <div className="text-[12.5px] leading-[1.5] text-foreground">
              <strong className="text-danger">Couldn't save your password.</strong>{" "}
              {submitError === "We couldn't save your password. Try again."
                ? "Check your connection and try again — what you typed is still here."
                : submitError}
            </div>
          </div>
        )}

        <PwField
          id="password"
          label="Password"
          value={password}
          onChange={setPassword}
          placeholder="Use a long passphrase"
          autoFocus
        />
        <StrengthMeter score={strength} length={password.length} />

        <PwField
          id="confirm"
          label="Confirm password"
          value={confirm}
          onChange={setConfirm}
          placeholder="Re-enter your password"
          invalid={confirm.length > 0 && !match}
        />
        <MatchHint confirm={confirm} match={match} />

        <PolicyChecks length={password.length} breach={breach} />

        <button
          type="submit"
          disabled={!canSubmit || submitting}
          className="mt-1 flex h-12 w-full items-center justify-center gap-2.5 bg-accent text-[14.5px] font-semibold tracking-[0.01em] text-accent-foreground transition-colors hover:bg-accent/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-panel disabled:cursor-not-allowed disabled:bg-border-soft disabled:text-muted"
        >
          {submitting ? (
            <>
              <Loader2 strokeWidth={1.5} className="h-4 w-4 animate-spin" />
              Confirming…
            </>
          ) : (
            <>
              {submitError ? "Try again" : "Confirm credential & continue"}
              <ArrowRight strokeWidth={1.75} className="h-4 w-4" />
            </>
          )}
        </button>

        <div className="text-[13px] text-muted">
          Already activated?{" "}
          <Link to="/login" className="font-semibold text-accent hover:underline">
            Sign in instead
          </Link>
        </div>
      </div>
    </form>
  );
}

// ───────────────────────────────────────────────────────────────────────────
// Password field + show/hide + strength + checklist
// ───────────────────────────────────────────────────────────────────────────

function PwField({
  id, label, value, onChange, placeholder, autoFocus, invalid,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  autoFocus?: boolean;
  invalid?: boolean;
}) {
  const [show, setShow] = useState(false);
  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-[10.5px] font-semibold uppercase tracking-[0.14em] text-muted">
        {label}
      </label>
      <div className="relative flex items-center">
        <Lock aria-hidden strokeWidth={1.5} className="pointer-events-none absolute left-3.5 h-4 w-4 text-muted" />
        <input
          id={id}
          type={show ? "text" : "password"}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          autoFocus={autoFocus}
          autoComplete="new-password"
          className={`h-12 w-full border bg-panel pl-10 pr-11 text-[15px] text-foreground placeholder:text-muted/70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${invalid ? "border-danger" : "border-border"}`}
        />
        <button
          type="button"
          onClick={() => setShow((s) => !s)}
          aria-label={show ? "Hide password" : "Show password"}
          className="absolute right-2 grid h-8 w-8 place-items-center text-muted transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-accent"
        >
          {show
            ? <EyeOff strokeWidth={1.5} className="h-4 w-4 text-accent" />
            : <Eye strokeWidth={1.5} className="h-4 w-4" />}
        </button>
      </div>
    </div>
  );
}

function StrengthMeter({ score, length }: { score: number; length: number }) {
  const labels = ["Weak", "Fair", "Fair", "Good", "Strong"];
  const filled = length === 0 ? 0 : Math.min(5, score + 1);
  const labelText = length === 0 ? "Enter a password" : labels[Math.min(4, score)];
  const tone = length === 0
    ? "text-muted"
    : score >= 4 ? "text-success"
      : score >= 2 ? "text-foreground"
        : "text-accent";
  return (
    <div className="flex flex-col gap-2">
      <div className="flex gap-1">
        {[0, 1, 2, 3, 4].map((i) => (
          <span
            key={i}
            className={`h-1 flex-1 transition-colors ${i < filled
              ? score >= 4 ? "bg-success"
                : score >= 2 ? "bg-foreground/60"
                  : "bg-accent"
              : "bg-border-soft"}`}
          />
        ))}
      </div>
      <div className="flex items-baseline justify-between">
        <span className={`text-[11px] font-semibold tracking-[0.04em] ${tone}`}>{labelText}</span>
        <span className="text-[10.5px] text-muted">Strength · advisory</span>
      </div>
    </div>
  );
}

function MatchHint({ confirm, match }: { confirm: string; match: boolean }) {
  if (confirm.length === 0) return null;
  return (
    <div className={`-mt-2 flex items-center gap-2 text-[12px] ${match ? "text-success" : "text-danger"}`}>
      {match ? <Check strokeWidth={1.75} className="h-3.5 w-3.5" /> : <span aria-hidden className="text-[14px] leading-none">×</span>}
      {match ? "Passwords match" : "Passwords don't match yet"}
    </div>
  );
}

function PolicyChecks({ length, breach }: { length: number; breach: BreachState }) {
  const lenState: "ok" | "no" | "idle" = length === 0 ? "idle" : length >= MIN_LEN ? "ok" : "no";

  let breachState: "ok" | "no" | "pending" | "idle";
  let breachLabel: string;
  switch (breach) {
    case "safe":
      breachState = "ok";
      breachLabel = "Not found in known data breaches";
      break;
    case "breached":
      breachState = "no";
      breachLabel = "Found in a data breach — choose another";
      break;
    case "checking":
      breachState = "pending";
      breachLabel = "Checking known data breaches…";
      break;
    case "unavailable":
      breachState = "idle";
      breachLabel = "Breach check unavailable — server will verify";
      break;
    case "idle":
    default:
      breachState = "idle";
      breachLabel = "Not found in known data breaches";
  }

  return (
    <div className="flex flex-col gap-2.5">
      <PolicyRow state={lenState} label={`At least ${MIN_LEN} characters`} />
      <div aria-live="polite">
        <PolicyRow state={breachState} label={breachLabel} />
      </div>
    </div>
  );
}

function PolicyRow({ state, label }: { state: "ok" | "no" | "pending" | "idle"; label: string }) {
  return (
    <div className="flex items-center gap-2.5">
      <span
        aria-hidden
        className={`grid h-[17px] w-[17px] shrink-0 place-items-center transition-colors ${state === "ok" ? "bg-success" : state === "no" ? "bg-danger" : "border-[1.5px] border-border"}`}
      >
        {state === "ok" && <Check strokeWidth={2} className="h-2.5 w-2.5 text-background" />}
        {state === "no" && <span className="h-[1.6px] w-[7px] bg-background" />}
        {state === "pending" && <Loader2 strokeWidth={2} className="h-3 w-3 animate-spin text-accent" />}
      </span>
      <span className={`text-[12.5px] ${state === "no" ? "text-danger" : state === "idle" ? "text-muted" : "text-foreground"}`}>
        {label}
      </span>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
// HIBP k-anonymity browser-side check
// SubtleCrypto SHA-1 → first 5 hex chars sent to api.pwnedpasswords.com → match suffix locally.
// The password itself NEVER leaves the browser.
// ───────────────────────────────────────────────────────────────────────────

function useBreachCheck(password: string): BreachState {
  const [state, setState] = useState<BreachState>("idle");
  const requestId = useRef(0);

  useEffect(() => {
    // Empty / too-short → idle (the length rule handles its own gating).
    if (password.length === 0 || password.length < MIN_LEN) {
      setState("idle");
      return;
    }

    const myRequest = ++requestId.current;
    setState("checking");

    const timer = window.setTimeout(async () => {
      try {
        const sha1Hex = await sha1Uppercase(password);
        const prefix = sha1Hex.slice(0, 5);
        const suffix = sha1Hex.slice(5);

        const ctrl = new AbortController();
        const timeout = window.setTimeout(() => ctrl.abort(), HIBP_TIMEOUT_MS);

        const resp = await fetch(`https://api.pwnedpasswords.com/range/${prefix}`, {
          method: "GET",
          headers: { "Add-Padding": "true" },
          signal: ctrl.signal,
        });
        window.clearTimeout(timeout);

        if (myRequest !== requestId.current) return;

        if (!resp.ok) {
          setState("unavailable");
          return;
        }

        const body = await resp.text();
        const breached = body.split("\n").some((line) => {
          const [s] = line.split(":");
          return s?.trim().toUpperCase() === suffix;
        });
        if (myRequest !== requestId.current) return;
        setState(breached ? "breached" : "safe");
      } catch {
        if (myRequest !== requestId.current) return;
        setState("unavailable");
      }
    }, BREACH_CHECK_DEBOUNCE_MS);

    return () => window.clearTimeout(timer);
  }, [password]);

  return state;
}

async function sha1Uppercase(input: string): Promise<string> {
  const bytes = new TextEncoder().encode(input);
  const hashBuf = await crypto.subtle.digest("SHA-1", bytes);
  return Array.from(new Uint8Array(hashBuf))
    .map((b) => b.toString(16).padStart(2, "0").toUpperCase())
    .join("");
}

// Length-weighted advisory strength (0–4) per the handoff. NOT a gate.
function strengthScore(pw: string): number {
  if (!pw) return 0;
  let s = 0;
  if (pw.length >= MIN_LEN) s++;
  if (pw.length >= 18) s++;
  if (pw.length >= 24) s++;
  const variety = [/[a-z]/, /[A-Z]/, /[0-9]/, /[^A-Za-z0-9]/].filter((re) => re.test(pw)).length;
  if (variety >= 3 || /\s/.test(pw)) s++;
  return Math.min(4, s);
}

// ───────────────────────────────────────────────────────────────────────────
// Success → progress → /members
// ───────────────────────────────────────────────────────────────────────────

function SuccessRedirectPanel({
  firstName,
  churchName,
  email,
}: {
  firstName: string | null;
  churchName: string | null;
  email: string | null;
}) {
  // Two static classes (w-0 → w-full) swap on the next tick so Tailwind's
  // transition driver animates the bar fill. No inline style needed.
  const [full, setFull] = useState(false);
  useEffect(() => {
    const id = window.setTimeout(() => setFull(true), 120);
    return () => window.clearTimeout(id);
  }, []);
  return (
    <div>
      <div className="grid h-14 w-14 place-items-center bg-success text-background">
        <Check strokeWidth={1.75} className="h-7 w-7" />
      </div>
      <div className="mt-7">
        <Eyebrow tone="success">Account activated</Eyebrow>
      </div>
      <h1 className="mt-3.5 font-heading text-[38px] font-bold leading-[1.05] tracking-[-0.025em] text-foreground">
        You're all set{firstName ? `, ${firstName}` : ""}.
      </h1>
      <p className="mt-4 max-w-[380px] text-[14.5px] leading-[1.6] text-muted">
        Your password is saved and your membership to {churchName ?? "the portal"} is now active.
      </p>
      <div className="mt-7 w-full max-w-[380px]">
        <div className="flex items-center gap-2.5 text-[13px] text-foreground">
          <Loader2 strokeWidth={2} className="h-4 w-4 animate-spin text-accent" />
          Taking you to your portal…
        </div>
        <div className="mt-3 h-[3px] bg-border-soft">
          <div
            className={`h-full bg-accent transition-[width] duration-[1100ms] ease-out ${full ? "w-full" : "w-0"}`}
          />
        </div>
      </div>
      {email && (
        <div className="mt-4 flex items-center gap-2 text-[12px] text-muted/80">
          <Mail strokeWidth={1.5} className="h-3.5 w-3.5" />
          A confirmation has been sent to {email}
        </div>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
// Right-panel content for Consumed / Expired / Invalid
// ───────────────────────────────────────────────────────────────────────────

function ConsumedRightPanel({ preview }: { preview: InvitationPreview }) {
  return (
    <div>
      <div className="grid h-14 w-14 place-items-center bg-success/15">
        <ShieldCheck strokeWidth={1.5} className="h-7 w-7 text-success" />
      </div>
      <div className="mt-7">
        <Eyebrow tone="success">Already activated</Eyebrow>
      </div>
      <h1 className="mt-3.5 font-heading text-[32px] font-bold leading-[1.1] tracking-[-0.025em] text-foreground">
        You've already set this up.
      </h1>
      <p className="mt-4 max-w-[384px] text-[14px] leading-[1.6] text-muted">
        This invitation has already been claimed{preview.firstName ? `, ${preview.firstName}` : ""}.
        Sign in with your password — or reset it if you've forgotten.
      </p>
      <div className="mt-7 flex flex-col gap-3">
        <Link
          to="/login"
          className="flex h-12 w-full items-center justify-center gap-2.5 bg-accent text-[14.5px] font-semibold tracking-[0.01em] text-accent-foreground transition-colors hover:bg-accent/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-panel"
        >
          Sign in
          <ArrowRight strokeWidth={1.75} className="h-4 w-4" />
        </Link>
        <Link to="/forgot-password" className="text-center text-[13px] text-muted hover:text-accent">
          Forgot your password?
        </Link>
      </div>
    </div>
  );
}

function ExpiredRightPanel({ preview }: { preview: InvitationPreview }) {
  // Best-effort contact mailto. If we don't know an admin email, omit the link
  // so it isn't a dead "mailto:?subject=…".
  const supportEmail = preview.email; // placeholder — preview doesn't currently carry SiteSettings.ContactEmail
  return (
    <div>
      <div className="grid h-14 w-14 place-items-center bg-danger/15">
        <Clock strokeWidth={1.5} className="h-7 w-7 text-danger" />
      </div>
      <div className="mt-7">
        <Eyebrow tone="muted">Invitation expired</Eyebrow>
      </div>
      <h1 className="mt-3.5 font-heading text-[32px] font-bold leading-[1.1] tracking-[-0.025em] text-foreground">
        This invitation has expired.
      </h1>
      <p className="mt-4 max-w-[384px] text-[14px] leading-[1.6] text-muted">
        Invitations are time-limited for security. Ask an administrator at {preview.churchName ?? "your church"} to send a fresh one — no harm done.
      </p>
      <div className="mt-7 flex flex-col gap-3">
        {supportEmail && (
          <a
            href={`mailto:?subject=${encodeURIComponent("Member portal invitation has expired")}`}
            className="flex h-12 w-full items-center justify-center gap-2.5 bg-accent text-[14.5px] font-semibold tracking-[0.01em] text-accent-foreground transition-colors hover:bg-accent/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-panel"
          >
            Request a new invitation
            <ArrowRight strokeWidth={1.75} className="h-4 w-4" />
          </a>
        )}
        <Link to="/login" className="text-center text-[13px] text-muted hover:text-accent">
          Go to sign in
        </Link>
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
// Invalid → neutral single card (no credential face — don't imply the link is legit)
// ───────────────────────────────────────────────────────────────────────────

function InvalidScreen() {
  return (
    <SystemThemeLayout>
      <main className="grid min-h-screen place-items-center bg-background px-4 text-foreground">
        <div className="w-full max-w-[440px] border border-border bg-panel px-8 py-10">
          <div className="grid h-14 w-14 place-items-center bg-foreground/10">
            <ShieldAlert strokeWidth={1.5} className="h-7 w-7 text-foreground/70" />
          </div>
          <div className="mt-7">
            <Eyebrow tone="muted">Link not recognised</Eyebrow>
          </div>
          <h1 className="mt-3.5 font-heading text-[28px] font-bold leading-[1.1] tracking-[-0.025em] text-foreground">
            This link isn't valid.
          </h1>
          <p className="mt-4 text-[14px] leading-[1.6] text-muted">
            The invitation URL may be incomplete, mistyped, or already replaced by a newer one. Ask an administrator to send a fresh invitation if you need one.
          </p>
          <Link
            to="/login"
            className="mt-7 inline-flex items-center gap-2 text-[13px] font-semibold text-accent hover:underline"
          >
            Go to sign in
            <ArrowRight strokeWidth={1.75} className="h-3.5 w-3.5" />
          </Link>
        </div>
      </main>
    </SystemThemeLayout>
  );
}

// ───────────────────────────────────────────────────────────────────────────
// Helpers
// ───────────────────────────────────────────────────────────────────────────

interface ApiErrorLike {
  getMessages?: () => string[];
}

function extractApiErrors(err: unknown): string[] {
  if (typeof err === "object" && err !== null && "getMessages" in err) {
    const fn = (err as ApiErrorLike).getMessages;
    if (typeof fn === "function") return fn();
  }
  return [];
}
