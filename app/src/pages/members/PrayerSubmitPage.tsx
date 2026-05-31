import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { memberPrayerApi } from "@/lib/api/prayerRequests";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import {
  Banner,
  Content,
  InlineError,
  PageHead,
} from "@/components/members/portal-primitives";
import { Info } from "lucide-react";

/**
 * Submit a prayer request. Fields per SubmitPrayerRequestRequest:
 * Title, BodyJson, IsAnonymous. NO members-only toggle (the entity
 * carries no IsMembersOnly flag; every Active request is member-visible).
 */
export function PrayerSubmitPage() {
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [bodyJson, setBodyJson] = useState<string | null>(null);
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) { setError("Title is required."); return; }
    if (!bodyJson) { setError("Body is required."); return; }
    setSubmitting(true);
    setError(null);
    try {
      const result = await memberPrayerApi.submit({
        title: title.trim(),
        bodyJson,
        isAnonymous,
      });
      navigate(`/members/prayer/${result.id}`);
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Couldn't submit. Try again."];
      setError(messages[0] ?? "Couldn't submit. Try again.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Content maxWidth="max-w-[680px]">
      <PageHead
        title="Submit a prayer request"
        onBack={() => navigate("/members/prayer")}
      />

      <Banner
        tone="info"
        icon={<Info strokeWidth={1.75} className="h-4 w-4" />}
      >
        Goes live after a quick automated check.
      </Banner>

      {error && (
        <div className="mb-4">
          <InlineError message={error} />
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-5">
        <div>
          <label htmlFor="prayer-title" className="mb-1.5 block text-sm font-medium">
            Title <span className="text-danger">*</span>
          </label>
          <input
            id="prayer-title"
            type="text"
            required
            maxLength={200}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            className="h-10 w-full border border-border bg-panel px-3 text-sm focus-visible:border-accent focus-visible:outline-none"
            placeholder="e.g. Healing for my mother"
          />
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-medium">
            Request <span className="text-danger">*</span>
          </label>
          <TipTapEditor
            ariaLabel="Prayer request body"
            valueJson={bodyJson}
            onChangeJson={setBodyJson}
            placeholder="Share what you'd like the community to pray about…"
          />
        </div>

        <label className="flex items-start gap-3 border border-border bg-panel p-3 text-sm">
          <input
            type="checkbox"
            checked={isAnonymous}
            onChange={(e) => setIsAnonymous(e.target.checked)}
            className="mt-0.5"
          />
          <span>
            <span className="block font-medium">Submit anonymously</span>
            <span className="block text-xs text-muted">
              Your name won't appear on the request. Other members will see "Anonymous".
            </span>
          </span>
        </label>

        <div className="flex flex-wrap items-center gap-2">
          <button
            type="submit"
            disabled={submitting}
            className="inline-flex h-10 items-center gap-2 bg-accent px-5 text-sm font-semibold text-accent-foreground hover:bg-accent/90 disabled:opacity-50"
          >
            {submitting ? "Submitting…" : "Submit prayer request"}
          </button>
          <button
            type="button"
            onClick={() => navigate("/members/prayer")}
            className="inline-flex h-10 items-center border border-border bg-panel px-4 text-sm font-medium hover:bg-panel-alt"
          >
            Cancel
          </button>
        </div>
      </form>
    </Content>
  );
}
