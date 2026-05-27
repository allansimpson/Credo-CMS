import { useEffect, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { eventRegistrationApi } from "@/lib/api/eventRegistration";
import { ApiError } from "@/lib/apiClient";
import { SeoTags } from "@/components/shared/SeoTags";

type Stage = "validating" | "valid" | "invalid" | "cancelling" | "done" | "error";

export function EventCancelRegistrationPage() {
  const { slug } = useParams<{ slug: string }>();
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";
  const [stage, setStage] = useState<Stage>("validating");
  const [errors, setErrors] = useState<string[]>([]);
  const [reason, setReason] = useState("");

  useEffect(() => {
    if (!slug || !token) {
      setStage("invalid");
      setErrors(["Missing or invalid cancel link."]);
      return;
    }
    let cancelled = false;
    eventRegistrationApi.validateCancelToken(slug, token)
      .then(() => { if (!cancelled) setStage("valid"); })
      .catch((err) => {
        if (cancelled) return;
        setStage("invalid");
        setErrors(err instanceof ApiError ? err.getMessages() : ["Invalid or expired cancel link."]);
      });
    return () => { cancelled = true; };
  }, [slug, token]);

  async function onConfirm() {
    if (!slug || !token) return;
    setStage("cancelling");
    setErrors([]);
    try {
      await eventRegistrationApi.cancel(slug, token, reason || undefined);
      setStage("done");
    } catch (err) {
      setStage("error");
      setErrors(err instanceof ApiError ? err.getMessages() : ["Could not cancel registration."]);
    }
  }

  return (
    <article className="mx-auto max-w-xl px-4 py-8">
      <SeoTags title="Cancel registration" description="Cancel your event registration" />
      <h1 className="text-2xl font-bold sm:text-3xl">Cancel registration</h1>

      {stage === "validating" && (
        <p className="mt-4 text-muted">Validating link…</p>
      )}

      {(stage === "invalid" || stage === "error") && (
        <>
          <div className="mt-4 border border-danger/40 bg-danger/10 p-3 text-sm text-danger">
            <ul className="list-inside list-disc">
              {errors.map((er, i) => <li key={i}>{er}</li>)}
            </ul>
          </div>
          <Link to={slug ? `/events/${slug}` : "/events"}
            className="mt-4 inline-block text-sm text-primary hover:underline">
            ← Back to event
          </Link>
        </>
      )}

      {(stage === "valid" || stage === "cancelling") && (
        <>
          <p className="mt-4 text-sm text-muted">
            Please confirm you'd like to cancel your registration. You may add an optional
            note below to let the organisers know why.
          </p>
          <label className="mt-4 block">
            <span className="text-sm font-medium">Reason (optional)</span>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={3}
              maxLength={500}
              className="mt-1 w-full border bg-background px-3 py-2 text-sm"
            />
          </label>
          <div className="mt-4 flex flex-wrap gap-3">
            <button
              type="button" onClick={onConfirm} disabled={stage === "cancelling"}
              className="inline-flex h-11 items-center justify-center bg-danger px-6 text-sm font-semibold text-danger-foreground hover:bg-danger/90 disabled:opacity-50"
            >
              {stage === "cancelling" ? "Cancelling…" : "Confirm cancellation"}
            </button>
            <Link to={slug ? `/events/${slug}` : "/events"}
              className="inline-flex h-11 items-center justify-center border bg-card px-6 text-sm hover:bg-panel-alt">
              Keep registration
            </Link>
          </div>
        </>
      )}

      {stage === "done" && (
        <>
          <p className="mt-4 text-sm">Your registration has been cancelled.</p>
          <Link to={slug ? `/events/${slug}` : "/events"}
            className="mt-4 inline-block text-sm text-primary hover:underline">
            ← Back to event
          </Link>
        </>
      )}
    </article>
  );
}
