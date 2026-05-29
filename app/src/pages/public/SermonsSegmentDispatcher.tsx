import { lazy, Suspense } from "react";
import { useParams } from "react-router-dom";

const SermonDetailPage = lazy(() =>
  import("@/pages/public/SermonDetailPage").then((m) => ({ default: m.SermonDetailPage })),
);
const SermonsArchivePage = lazy(() =>
  import("@/pages/public/SermonsArchivePage").then((m) => ({ default: m.SermonsArchivePage })),
);

/**
 * Branches the `/sermons/:slug` route between the year-archive and the
 * sermon-detail page based on the segment shape. Year-archive URLs are
 * /sermons/2024; sermon detail pages use lowercased slugs that include
 * letters / dashes, so a 4-digit segment is unambiguously a year.
 */
export function SermonsSegmentDispatcher() {
  const { slug } = useParams<{ slug: string }>();
  const isYear = !!slug && /^\d{4}$/.test(slug);

  return (
    <Suspense fallback={<p className="mx-auto max-w-4xl p-8 text-muted">Loading…</p>}>
      {isYear ? <SermonsArchivePage yearParam={Number(slug)} /> : <SermonDetailPage />}
    </Suspense>
  );
}
