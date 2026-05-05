import { useEffect } from "react";

export interface SeoTagsProps {
  title?: string | null;
  description?: string | null;
  ogType?: "website" | "article";
  /** Optional structured-data block; rendered as a JSON-LD <script> tag. */
  jsonLd?: object | null;
  /** Image URL for og:image and twitter:image. */
  imageUrl?: string | null;
}

/**
 * Imperatively writes per-route SEO tags into <head>. A real implementation
 * would render a SSR-aware Helmet/Effect; for the SPA we just patch the
 * document. Phase 11 will replace this with a more thorough setup once the
 * public site is the primary surface.
 */
export function SeoTags({ title, description, ogType, jsonLd, imageUrl }: SeoTagsProps) {
  useEffect(() => {
    if (title) document.title = title;

    setMeta("description", description);
    setMeta("og:title", title, "property");
    setMeta("og:description", description, "property");
    setMeta("og:type", ogType ?? "website", "property");
    setMeta("og:image", imageUrl, "property");

    setMeta("twitter:card", imageUrl ? "summary_large_image" : "summary");
    setMeta("twitter:title", title);
    setMeta("twitter:description", description);
    setMeta("twitter:image", imageUrl);

    let scriptEl: HTMLScriptElement | null = null;
    if (jsonLd) {
      scriptEl = document.createElement("script");
      scriptEl.type = "application/ld+json";
      scriptEl.dataset.seoTagsManaged = "true";
      scriptEl.text = JSON.stringify(jsonLd);
      document.head.appendChild(scriptEl);
    }

    return () => {
      if (scriptEl) scriptEl.remove();
    };
  }, [title, description, ogType, imageUrl, jsonLd]);

  return null;
}

function setMeta(name: string, value: string | null | undefined, attr: "name" | "property" = "name") {
  const selector = `meta[${attr}="${name}"]`;
  let el = document.head.querySelector<HTMLMetaElement>(selector);
  if (!value) {
    if (el && el.dataset.seoTagsManaged === "true") el.remove();
    return;
  }
  if (!el) {
    el = document.createElement("meta");
    el.setAttribute(attr, name);
    el.dataset.seoTagsManaged = "true";
    document.head.appendChild(el);
  }
  el.content = value;
}
