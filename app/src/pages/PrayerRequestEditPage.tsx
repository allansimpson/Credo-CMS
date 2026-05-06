import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import { memberPrayerApi } from "@/lib/api/prayerRequests";

export function PrayerRequestEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [bodyJson, setBodyJson] = useState<string | null>(null);
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    memberPrayerApi.get(id)
      .then((d) => {
        setTitle(d.title);
        setBodyJson(d.bodyJson);
        setIsAnonymous(d.isAnonymous);
      })
      .finally(() => setLoading(false));
  }, [id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;
    setSubmitting(true); setErrors([]);
    try {
      await memberPrayerApi.edit(id, { title, bodyJson: bodyJson ?? "", isAnonymous });
      navigate(`/prayer-requests/${id}`);
    } catch (err) {
      const messages = typeof err === "object" && err !== null && "getMessages" in err
        ? (err as { getMessages: () => string[] }).getMessages()
        : ["Save failed."];
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
            to={id ? `/prayer-requests/${id}` : "/prayer-requests"}
            className="mb-6 inline-flex items-center gap-1 text-sm text-muted hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" /> Back
          </Link>

          <h1 className="text-2xl font-bold">Edit prayer request</h1>

          {loading && <p className="mt-4 text-muted">Loading…</p>}
          {!loading && (
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
                  required
                  maxLength={200}
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
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
                />
              </div>

              <label className="flex items-start gap-3 text-sm">
                <input
                  type="checkbox"
                  checked={isAnonymous}
                  onChange={(e) => setIsAnonymous(e.target.checked)}
                  className="mt-1"
                />
                <span>Submit anonymously</span>
              </label>

              <div className="flex justify-end gap-2 border-t pt-4">
                <Link
                  to={id ? `/prayer-requests/${id}` : "/prayer-requests"}
                  className="inline-flex h-10 items-center justify-center rounded-md border bg-background px-4 text-sm"
                >
                  Cancel
                </Link>
                <button
                  type="submit"
                  disabled={submitting}
                  className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                >
                  {submitting ? "Saving…" : "Save changes"}
                </button>
              </div>
            </form>
          )}
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}
