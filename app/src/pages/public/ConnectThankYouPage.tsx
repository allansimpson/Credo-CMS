import { Link } from "react-router-dom";
import { Check } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";

export function ConnectThankYouPage() {
  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-2xl flex-1 px-4 py-10">
          <article className="rounded-lg border bg-card p-8 text-center">
            <span aria-hidden className="mx-auto grid h-12 w-12 place-items-center bg-success text-background">
              <Check className="h-6 w-6" />
            </span>
            <h1 className="mt-4 text-3xl font-bold">Thanks for connecting</h1>
            <p className="mt-3 text-sm text-fg-soft">
              We received your card. Someone from our team will follow up soon. If you
              shared an email, you'll see a confirmation note in your inbox.
            </p>
            <div className="mt-6 flex flex-wrap justify-center gap-3">
              <Link
                to="/"
                className="inline-flex h-10 items-center justify-center bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
              >
                Back home
              </Link>
            </div>
          </article>
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}
