import { useEffect, useState } from "react";

/**
 * Scroll-spy that returns the slug of the month section currently active in
 * the viewport. Powers the active dot in the sermons side-rail and the URL-
 * hash sync as the user scrolls.
 *
 * Expects month boundaries in the listing to be marked with:
 *
 *   <span id="oct" aria-hidden data-month="oct" />
 *
 * Algorithm — chosen over IntersectionObserver because IO's rootMargin
 * "active band" misses the last section when the page can't scroll far
 * enough to push it into the band (the document simply ends first). The
 * scroll-position approach handles both cases uniformly:
 *
 *  1. Walk the anchors top-to-bottom. The active month is the LAST anchor
 *     whose top edge has scrolled at or above a threshold line (just below
 *     the sticky header/filter bars).
 *  2. When the user is at the very bottom of the document, force-pick the
 *     final anchor in document order — fixes the "click oldest month, dot
 *     stuck on previous month" bug at the year-archive's tail end.
 *  3. Above the first anchor (page top), default to the first anchor.
 *
 * @param anchorSelector CSS selector matching the month-anchor spans.
 * @param syncHash       When true, `history.replaceState` updates the URL
 *                       hash as the active month changes — bookmark-friendly
 *                       without polluting back/forward history.
 * @param deps           Re-subscribe trigger — pass a value that changes when
 *                       the underlying anchor set might have changed (e.g.
 *                       listing length, year, search query).
 */
export function useActiveMonth(
  anchorSelector: string = "[data-month]",
  syncHash: boolean = true,
  deps: unknown[] = [],
): string | null {
  const [active, setActive] = useState<string | null>(null);

  useEffect(() => {
    const compute = () => {
      const anchors = Array.from(document.querySelectorAll<HTMLElement>(anchorSelector));
      if (anchors.length === 0) return;

      // Threshold = the visual top of the listing column, just below the
      // sticky public-nav + sermons-filter bars. The CSS variables are
      // published by PublicHeader and SermonsArchivePage; fall back to 0 if
      // either is absent (e.g. hook used outside the sermons context).
      const root = document.documentElement;
      const headerH = parseFloat(getComputedStyle(root).getPropertyValue("--public-header-offset")) || 0;
      const filterH = parseFloat(getComputedStyle(root).getPropertyValue("--sermons-filter-offset")) || 0;
      const threshold = headerH + filterH + 32;

      // Pick the last anchor whose top has crossed the threshold — that's
      // the section the viewer is currently inside.
      let bestIdx = -1;
      for (let i = 0; i < anchors.length; i++) {
        const top = anchors[i].getBoundingClientRect().top;
        if (top <= threshold) bestIdx = i;
        else break;
      }

      // End-of-document override: when the viewport bottom touches the page
      // bottom, the document can't scroll any further, so the last anchor
      // may never reach the threshold. Force-pick it.
      const atBottom = Math.ceil(window.innerHeight + window.scrollY) >= root.scrollHeight - 4;
      if (atBottom) bestIdx = anchors.length - 1;

      // Above the first anchor — default to it.
      if (bestIdx < 0) bestIdx = 0;

      const slug = anchors[bestIdx].dataset.month;
      if (!slug) return;

      setActive((prev) => {
        if (prev === slug) return prev;
        if (syncHash) {
          // Use replaceState so each scrolled month doesn't add a history
          // entry — bookmark-friendly without polluting back/fwd.
          const url = new URL(window.location.href);
          url.hash = slug;
          window.history.replaceState(window.history.state, "", url.toString());
        }
        return slug;
      });
    };

    compute();
    const onScroll = () => compute();
    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll);
    return () => {
      window.removeEventListener("scroll", onScroll);
      window.removeEventListener("resize", onScroll);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [anchorSelector, syncHash, ...deps]);

  return active;
}
