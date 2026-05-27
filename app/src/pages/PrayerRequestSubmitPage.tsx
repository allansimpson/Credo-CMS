import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import { memberPrayerApi } from "@/lib/api/prayerRequests";

export function PrayerRequestSubmitPage() {
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [bodyJson, setBodyJson] = useState<string | null>(null);
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    try {
      await memberPrayerApi.submit({
        title,
        bodyJson: bodyJson ?? "",
        isAnonymous,
      });
      navigate("/prayer-requests");
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Could not submit prayer request."];
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
          <Link
            to="/prayer-requests"
            className="mb-6 inline-flex items-center gap-1 text-sm text-muted hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" /> Back to list
          </Link>

          <header className="border-b pb-6">
            <h1 className="text-2xl font-bold">New prayer request</h1>
            <p className="mt-1 text-sm text-muted">
              Members will see your request and can mark "I prayed for this".
              You can edit it later until an editor posts an update or marks it answered.
            </p>
          </header>

          <form onSubmit={handleSubmit} className="mt-6 space-y-5">
            {errors.length > 0 && (
              <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
                <ul className="list-disc pl-5">{errors.map((err) => <li key={err}>{err}</li>)}</ul>
              </div>
            )}

            <div>
              <label htmlFor="title" className="mb-1 block text-sm font-medium">
                Title <span className="text-danger">*</span>
              </label>
              <input
                id="title"
                type="text"
                required
                maxLength={200}
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                placeholder="A short summary"
              />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium">
                Body <span className="text-danger">*</span>
              </label>
              <TipTapEditor
                ariaLabel="Prayer request body"
                valueJson={bodyJson}
                onChangeJson={setBodyJson}
                placeholder="Share what you'd like the church to pray about…"
              />
            </div>

            <label className="flex items-start gap-3 text-sm">
              <input
                type="checkbox"
                checked={isAnonymous}
                onChange={(e) => setIsAnonymous(e.target.checked)}
                className="mt-1"
              />
              <span>
                <span className="block font-medium">Submit anonymously</span>
                <span className="text-xs text-muted">
                  Your name is hidden from other members. Editors and administrators still
                  see who submitted, for moderation.
                </span>
              </span>
            </label>

            <div className="flex justify-end gap-2 border-t pt-4">
              <Link
                to="/prayer-requests"
                className="inline-flex h-10 items-center justify-center rounded-md border bg-background px-4 text-sm"
              >
                Cancel
              </Link>
              <button
                type="submit"
                disabled={submitting}
                className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
              >
                {submitting ? "Submitting…" : "Submit request"}
              </button>
            </div>
          </form>
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}
