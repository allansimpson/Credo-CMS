import type { ReactNode } from "react";
import type { PublicTemplate } from "@/types/api";
import { PublicHeader } from "./PublicHeader";
import { PublicFooter } from "./PublicFooter";

/** Page identifiers used by the header for active-nav styling. */
export type PublicActivePage =
  | "home"
  | "about"
  | "im-new"
  | "beliefs"
  | "sermons"
  | "events"
  | "news"
  | "leaders"
  | "contact"
  | "members"
  | null;

export interface PublicPageProps {
  /** Template — read from siteSettings.template; passed in by the
   * parent layout so this component stays presentational. */
  template: PublicTemplate;
  /** Used by the header for active-nav highlighting. Pass `null` for
   * pages with no active nav (e.g., 404). */
  activePage: PublicActivePage;
  children: ReactNode;
}

/**
 * Top-level wrapper for every public-facing page. Mounts the header
 * (varies per template) + main + footer (varies per template). Per the
 * Public Site design handoff: same content shape across both templates;
 * only visual treatment differs.
 */
export function PublicPage({ template, activePage, children }: PublicPageProps) {
  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground font-body">
      <PublicHeader template={template} activePage={activePage} />
      <main className="flex-1">{children}</main>
      <PublicFooter template={template} />
    </div>
  );
}
