import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { PublicHeader } from "@/components/public/PublicHeader";
import { usePublicActivePage } from "@/components/public/usePublicActivePage";

/**
 * Shim around the new template-aware <PublicHeader>. Existing public
 * pages call `<PublicNavBar />` with no props; this shim reads the
 * template from SiteSettings and derives the active page from the
 * current route, then renders the new header.
 *
 * The new header includes the announcement bar conditionally
 * (Editorial only). Callers should NOT render <AnnouncementBar />
 * separately when using <PublicNavBar />.
 *
 * Future PRs migrate individual pages to use <PublicPage> directly,
 * at which point those pages stop going through this shim.
 */
export function PublicNavBar() {
  const { settings } = useSiteSettings();
  const activePage = usePublicActivePage();
  const template = settings?.template ?? 0;
  return <PublicHeader template={template} activePage={activePage} />;
}
