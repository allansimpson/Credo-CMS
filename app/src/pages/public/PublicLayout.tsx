import { useEffect } from "react";
import { Outlet } from "react-router-dom";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { useSiteSettings } from "@/lib/SiteSettingsContext";

export function PublicLayout() {
  const { settings } = useSiteSettings();

  // Site-wide Organization JSON-LD. Per-route components add Article /
  // Person / Event / Schedule on top.
  useEffect(() => {
    if (!settings?.churchName) return;
    const existing = document.head.querySelector('script[data-credo-org="1"]');
    if (existing) existing.remove();
    const script = document.createElement("script");
    script.type = "application/ld+json";
    script.dataset.credoOrg = "1";
    script.text = JSON.stringify({
      "@context": "https://schema.org",
      "@type": "Church",
      name: settings.churchName,
      url: window.location.origin,
      logo: settings.logoUrl ?? undefined,
      email: settings.contactEmail ?? undefined,
      telephone: settings.contactPhone ?? undefined,
      address: settings.contactAddress
        ? { "@type": "PostalAddress", streetAddress: settings.contactAddress }
        : undefined,
      sameAs: [
        settings.facebookUrl, settings.instagramUrl, settings.youTubeUrl,
        settings.xUrl, settings.tikTokUrl, settings.otherSocialUrl,
      ].filter(Boolean),
    });
    document.head.appendChild(script);
    return () => { script.remove(); };
  }, [settings]);

  // PublicNavBar (shim) renders the AnnouncementBar conditionally per
  // template — no need to render it again here.
  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <div className="flex-1">
          <Outlet />
        </div>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}
