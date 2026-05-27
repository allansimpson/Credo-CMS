import { Link } from "react-router-dom";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { useSiteSettings } from "@/lib/SiteSettingsContext";

/**
 * Church-themed 404 page. Used both for legitimate 404s (unmatched routes) and
 * for covert 404s (admin/docs URLs viewed without sufficient role). Both look
 * identical so callers cannot distinguish.
 */
export function NotFoundPage() {
  const { settings } = useSiteSettings();
  const churchName = settings?.churchName ?? "Credo CMS";

  return (
    <ChurchThemeLayout>
      <main className="mx-auto flex min-h-screen max-w-2xl flex-col items-center justify-center px-4 py-16 text-center">
        <p className="font-medium text-primary">404</p>
        <h1 className="mt-2 text-3xl font-bold tracking-tight sm:text-4xl">
          Page not found
        </h1>
        <p className="mt-3 text-base text-muted">
          The page you were looking for doesn't exist on the {churchName} site.
        </p>
        <Link
          to="/"
          className="mt-8 inline-flex items-center rounded-md bg-primary px-4 py-2 text-sm font-semibold text-primary-foreground shadow-sm hover:bg-primary/90"
        >
          Go home
        </Link>
      </main>
    </ChurchThemeLayout>
  );
}
