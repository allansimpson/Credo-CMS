import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { publicConnectCardApi } from "@/lib/api/connectCard";

declare global {
  interface Window {
    turnstile?: {
      render: (
        selector: string | HTMLElement,
        options: {
          sitekey: string;
          callback: (token: string) => void;
          "error-callback"?: () => void;
          "expired-callback"?: () => void;
          theme?: "light" | "dark" | "auto";
        },
      ) => string;
      reset: (widgetId?: string) => void;
    };
    onloadTurnstileCallback?: () => void;
  }
}

const TURNSTILE_SCRIPT_ID = "cf-turnstile-script";

export function ConnectCardPage() {
  const navigate = useNavigate();
  const loadedAtRef = useRef<Date>(new Date());
  const turnstileWidgetRef = useRef<string | null>(null);
  const turnstileContainerRef = useRef<HTMLDivElement>(null);

  // Site key from a global. Real wiring lands in Q16 SiteSettings UI;
  // until then we read from window.__CONNECT_TURNSTILE_SITEKEY__ if set,
  // otherwise the server short-circuits siteverify (dev mode), so the
  // widget is intentionally optional here.
  const turnstileSiteKey = typeof window !== "undefined"
    ? (window as { __CONNECT_TURNSTILE_SITEKEY__?: string }).__CONNECT_TURNSTILE_SITEKEY__
    : undefined;

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [isFirstTimeVisitor, setIsFirstTimeVisitor] = useState(false);
  const [serviceDate, setServiceDate] = useState("");
  const [howDidYouHear, setHowDidYouHear] = useState("");
  const [comments, setComments] = useState("");
  const [honeypot, setHoneypot] = useState("");
  const [turnstileToken, setTurnstileToken] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  // Inject the Turnstile loader once the page mounts. The widget renders
  // into a known container; if there's no site key we skip the script.
  useEffect(() => {
    if (!turnstileSiteKey) return;
    if (document.getElementById(TURNSTILE_SCRIPT_ID)) {
      renderWidget();
      return;
    }
    window.onloadTurnstileCallback = renderWidget;
    const script = document.createElement("script");
    script.id = TURNSTILE_SCRIPT_ID;
    script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?onload=onloadTurnstileCallback";
    script.async = true;
    script.defer = true;
    document.head.appendChild(script);

    function renderWidget() {
      if (!turnstileContainerRef.current || !window.turnstile) return;
      turnstileWidgetRef.current = window.turnstile.render(turnstileContainerRef.current, {
        sitekey: turnstileSiteKey!,
        callback: (token) => setTurnstileToken(token),
        "error-callback": () => setTurnstileToken(null),
        "expired-callback": () => setTurnstileToken(null),
      });
    }
  }, [turnstileSiteKey]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);

    try {
      const result = await publicConnectCardApi.submit({
        name,
        email: email || null,
        phone: phone || null,
        isFirstTimeVisitor,
        serviceDate: serviceDate || null,
        howDidYouHear,
        comments: comments || null,
        interests: null,
        honeypotValue: honeypot,
        clientLoadedAt: loadedAtRef.current.toISOString(),
        turnstileToken,
      });
      if (result.ok) {
        navigate("/connect/thank-you");
      } else {
        setErrors(result.errors ?? ["Submission rejected."]);
        if (turnstileWidgetRef.current && window.turnstile) {
          window.turnstile.reset(turnstileWidgetRef.current);
          setTurnstileToken(null);
        }
      }
    } catch (err) {
      const messages = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Something went wrong. Please try again."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-2xl flex-1 px-4 py-10">
          <header className="border-b pb-6">
            <h1 className="text-3xl font-bold">Connect with us</h1>
            <p className="mt-2 text-sm text-muted">
              Share a few details and someone from our team will follow up.
            </p>
          </header>

          <form onSubmit={handleSubmit} className="mt-6 space-y-5">
            {errors.length > 0 && (
              <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
                <ul className="list-disc pl-5">{errors.map((err) => <li key={err}>{err}</li>)}</ul>
              </div>
            )}

            {/* Honeypot — visually hidden but not display:none (some bots skip
                display:none fields on purpose). */}
            <div aria-hidden="true" style={{ position: "absolute", left: "-9999px", top: "auto", width: "1px", height: "1px", overflow: "hidden" }}>
              <label>
                Website <input
                  type="text"
                  tabIndex={-1}
                  autoComplete="off"
                  value={honeypot}
                  onChange={(e) => setHoneypot(e.target.value)}
                />
              </label>
            </div>

            <Field id="cc-name" label="Your name" required>
              <input
                id="cc-name"
                required
                maxLength={200}
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="input"
              />
            </Field>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="cc-email" label="Email" hint="At least one of email or phone is required.">
                <input
                  id="cc-email"
                  type="email"
                  maxLength={200}
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="input"
                />
              </Field>
              <Field id="cc-phone" label="Phone">
                <input
                  id="cc-phone"
                  type="tel"
                  maxLength={50}
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  className="input"
                />
              </Field>
            </div>

            <label className="flex items-start gap-2 text-sm">
              <input
                type="checkbox"
                checked={isFirstTimeVisitor}
                onChange={(e) => setIsFirstTimeVisitor(e.target.checked)}
                className="mt-1"
              />
              <span>This is my first time visiting</span>
            </label>

            {isFirstTimeVisitor && (
              <Field id="cc-service-date" label="Date you visited">
                <input
                  id="cc-service-date"
                  type="date"
                  value={serviceDate}
                  onChange={(e) => setServiceDate(e.target.value)}
                  className="input"
                />
              </Field>
            )}

            <Field id="cc-how" label="How did you hear about us?" required>
              <input
                id="cc-how"
                required
                maxLength={500}
                value={howDidYouHear}
                onChange={(e) => setHowDidYouHear(e.target.value)}
                className="input"
                placeholder="A friend, a search engine, a sign…"
              />
            </Field>

            <Field id="cc-comments" label="Anything else you'd like us to know?">
              <textarea
                id="cc-comments"
                value={comments}
                maxLength={5000}
                onChange={(e) => setComments(e.target.value)}
                className="input min-h-24 py-2"
              />
            </Field>

            {turnstileSiteKey && (
              <div ref={turnstileContainerRef} aria-label="Turnstile verification" />
            )}

            <div className="flex justify-end border-t pt-4">
              <button
                type="submit"
                disabled={submitting}
                className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-5 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
              >
                {submitting ? "Sending…" : "Send"}
              </button>
            </div>
          </form>

          <Styles />
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function Field({
  id, label, hint, required, children,
}: { id: string; label: string; hint?: string; required?: boolean; children: React.ReactNode }) {
  return (
    <div>
      <label htmlFor={id} className="mb-1 block text-sm font-medium">
        {label}{required && <span className="text-danger"> *</span>}
      </label>
      {children}
      {hint && <p className="mt-1 text-xs text-muted">{hint}</p>}
    </div>
  );
}

function Styles() {
  return (
    <style>{`
      .input {
        height: 2.5rem;
        width: 100%;
        border-radius: 0.375rem;
        border: 1px solid hsl(var(--input));
        background: hsl(var(--background));
        padding: 0 0.75rem;
        font-size: 0.875rem;
      }
      textarea.input { height: auto; }
      .input:focus { outline: none; box-shadow: 0 0 0 2px hsl(var(--ring) / 0.4); }
    `}</style>
  );
}
