/**
 * Auto-generates a URL-safe slug from a free-text title. Mirrors the
 * server-side validator's expectations: lower-case, dash-separated,
 * alphanumeric only.
 */
export function slugify(text: string): string {
  return text
    .toLowerCase()
    .normalize("NFKD")
    .replace(/[̀-ͯ]/g, "")     // strip diacritics
    .replace(/[^a-z0-9]+/g, "-")          // non-alnum → dash
    .replace(/^-+|-+$/g, "")              // trim leading/trailing dashes
    .replace(/-{2,}/g, "-")               // collapse multiple dashes
    .slice(0, 200);
}
