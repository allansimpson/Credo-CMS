import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { PublicHeader } from "@/components/public/PublicHeader";
import { usePublicActivePage } from "@/components/public/usePublicActivePage";

/**
 * Public navigation entry point used by pages that render chrome
 * themselves (i.e., not wrapped in <PublicLayout>). Reads the template
 * from SiteSettings and derives the active page from the current
 * route, then renders the template-aware <PublicHeader>.
 *
 * The header includes the announcement bar conditionally (Editorial
 * only). Callers should NOT render <AnnouncementBar /> separately.
 */
export function PublicNavBar() {
  const { settings } = useSiteSettings();
  const activePage = usePublicActivePage();
  const template = settings?.template ?? 0;
  return <PublicHeader template={template} activePage={activePage} />;
}
