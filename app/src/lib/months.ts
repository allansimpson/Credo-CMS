/**
 * Shared month constants for the sermons side-rail and any other date-grouped
 * archives. The slugs match the lowercase three-letter form the backend uses
 * in <c>YearStatsDto.MonthCounts</c> so the dictionary keys round-trip
 * untouched between API and UI.
 */

export const MONTH_SLUGS = [
  "jan", "feb", "mar", "apr", "may", "jun",
  "jul", "aug", "sep", "oct", "nov", "dec",
] as const;

export type MonthSlug = (typeof MONTH_SLUGS)[number];

/** Display labels (3-letter title-case), aligned by index with MONTH_SLUGS. */
export const MONTH_NAMES: readonly string[] = [
  "Jan", "Feb", "Mar", "Apr", "May", "Jun",
  "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
] as const;

/** 1-12 (calendar month) → slug. Returns "jan" for out-of-range input. */
export function monthSlugFor(month: number): MonthSlug {
  if (month < 1 || month > 12) return "jan";
  return MONTH_SLUGS[month - 1];
}

/** Slug → 0-based index, or -1 if unknown. */
export function monthIndexOf(slug: string): number {
  const i = MONTH_SLUGS.indexOf(slug as MonthSlug);
  return i;
}
