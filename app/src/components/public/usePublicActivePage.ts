import { useLocation } from "react-router-dom";
import type { PublicActivePage } from "./PublicPage";

/**
 * Derives the active-nav token for the public header from the current
 * route. Returns `null` for routes outside the primary nav (404, search,
 * profile sub-pages other than /members/*, auth flows). Used by the
 * shared shim layer that wraps the new <PublicHeader> for legacy call
 * sites that don't have a way to thread `activePage` down themselves.
 */
export function usePublicActivePage(): PublicActivePage {
  const { pathname } = useLocation();
  const path = pathname.toLowerCase();

  if (path === "/" || path === "") return "home";
  if (path.startsWith("/about")) return "about";
  if (path.startsWith("/im-new")) return "im-new";
  if (path.startsWith("/beliefs") || path.startsWith("/what-we-believe")) return "beliefs";
  if (path.startsWith("/sermons")) return "sermons";
  if (path.startsWith("/events")) return "events";
  if (path.startsWith("/news") || path.startsWith("/blog")) return "news";
  if (path.startsWith("/leaders")) return "leaders";
  if (path.startsWith("/contact")) return "contact";
  if (path.startsWith("/members")) return "members";
  return null;
}
