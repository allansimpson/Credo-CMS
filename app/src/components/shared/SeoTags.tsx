import { useEffect } from "react";

export interface SeoTagsProps {
  title?: string | null;
  description?: string | null;
  ogType?: "website" | "article";
  /** Optional structured-data block; rendered as a JSON-LD <script> tag. */
  jsonLd?: object | null;
  /** Image URL for og:image and twitter:image. */
  imageUrl?: string | null;
  /** Phase 6 — RSS auto-discovery. Adds `<link rel="alternate" type="application/rss+xml">`. */
  rssFeedUrl?: string | null;
  rssFeedTitle?: string | null;
  /** Canonical URL for `<link rel="canonical">`. */
  canonicalUrl?: string | null;
}

/**
 * Imperatively writes per-route SEO tags into <head>. A real implementation
 * would render a SSR-aware Helmet/Effect; for the SPA we just patch the
 * document. Phase 11 will replace this with a more thorough setup once the
 * public site is the primary surface.
 */
export function SeoTags({
  title, description, ogType, jsonLd, imageUrl,
  rssFeedUrl, rssFeedTitle, canonicalUrl,
}: SeoTagsProps) {
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

    setLink("canonical", canonicalUrl);
    const rssLink = setRssLink(rssFeedUrl, rssFeedTitle);

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
      if (rssLink) rssLink.remove();
    };
  }, [title, description, ogType, imageUrl, jsonLd, rssFeedUrl, rssFeedTitle, canonicalUrl]);

  return null;
}

function setLink(rel: string, href: string | null | undefined) {
  const selector = `link[rel="${rel}"]`;
  let el = document.head.querySelector<HTMLLinkElement>(selector);
  if (!href) {
    if (el && el.dataset.seoTagsManaged === "true") el.remove();
    return;
  }
  if (!el) {
    el = document.createElement("link");
    el.rel = rel;
    el.dataset.seoTagsManaged = "true";
    document.head.appendChild(el);
  }
  el.href = href;
}

function setRssLink(href: string | null | undefined, title: string | null | undefined) {
  if (!href) return null;
  const el = document.createElement("link");
  el.rel = "alternate";
  el.type = "application/rss+xml";
  el.href = href;
  if (title) el.title = title;
  el.dataset.seoTagsManaged = "true";
  document.head.appendChild(el);
  return el;
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
