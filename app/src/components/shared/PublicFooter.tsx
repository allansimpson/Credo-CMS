import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { PublicFooter as PublicFooterImpl } from "@/components/public/PublicFooter";

/**
 * Shim around the new template-aware <PublicFooter>. Existing public
 * pages call `<PublicFooter />` with no props; this shim reads the
 * template from SiteSettings and renders the new template-aware
 * footer.
 *
 * Future PRs migrate individual pages to use <PublicPage> directly,
 * at which point those pages stop going through this shim.
 */
export function PublicFooter() {
  const { settings } = useSiteSettings();
  const template = settings?.template ?? 0;
  return <PublicFooterImpl template={template} />;
}
