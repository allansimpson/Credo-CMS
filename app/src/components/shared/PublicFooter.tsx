import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { PublicFooter as PublicFooterImpl } from "@/components/public/PublicFooter";

/**
 * Public footer entry point used by pages that render chrome themselves
 * (i.e., not wrapped in <PublicLayout>). Reads the template from
 * SiteSettings and renders the template-aware <PublicFooter>.
 */
export function PublicFooter() {
  const { settings } = useSiteSettings();
  const template = settings?.template ?? 0;
  return <PublicFooterImpl template={template} />;
}
