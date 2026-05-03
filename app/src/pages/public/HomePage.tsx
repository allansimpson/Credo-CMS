import { Link } from "react-router-dom";
import { useSiteSettings } from "@/lib/SiteSettingsContext";

export function HomePage() {
  const { settings } = useSiteSettings();
  const churchName = settings?.churchName ?? "Welcome";

  return (
    <main>
      <section className="bg-primary text-primary-foreground">
        <div className="mx-auto max-w-6xl px-4 py-20 text-center md:py-28">
          <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">{churchName}</h1>
          {settings?.tagline && (
            <p className="mx-auto mt-4 max-w-2xl text-lg opacity-90">{settings.tagline}</p>
          )}
          <div className="mt-8">
            <Link
              to="/services"
              className="inline-flex h-11 items-center rounded-md bg-accent px-6 font-semibold text-accent-foreground hover:bg-accent/90"
            >
              Plan your visit
            </Link>
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-3xl px-4 py-12 text-center">
        <h2 className="text-2xl font-semibold">Welcome</h2>
        <p className="mt-3 text-muted-foreground">
          We're so glad you stopped by. Use the menu above to learn about who we are,
          when we meet, and what we believe.
        </p>
      </section>
    </main>
  );
}
