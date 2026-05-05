import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { documentsApi, publicDocumentFileUrl } from "@/lib/api/documents";
import { SeoTags } from "@/components/shared/SeoTags";
import { useBreakpoint } from "@/hooks/useBreakpoint";
import type { PublicDocument } from "@/types/api";
import { NotFoundPage } from "@/pages/NotFoundPage";

export function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const breakpoint = useBreakpoint();
  const [item, setItem] = useState<PublicDocument | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    documentsApi.listPublic()
      .then((items) => {
        if (cancelled) return;
        const found = items.find((d) => d.id === id);
        if (!found) { setNotFound(true); setLoading(false); return; }
        setItem(found);
        setLoading(false);
      })
      .catch(() => { if (!cancelled) { setNotFound(true); setLoading(false); } });
    return () => { cancelled = true; };
  }, [id]);

  if (loading) return <p className="mx-auto max-w-3xl p-8 text-muted-foreground">Loading…</p>;
  if (notFound || !item) return <NotFoundPage />;

  const fileUrl = publicDocumentFileUrl(item.id);
  const isMobile = breakpoint === "mobile";

  return (
    <div className="mx-auto max-w-4xl px-4 py-8">
      <SeoTags title={item.title} description={item.description ?? undefined} />
      <h1 className="text-2xl font-bold sm:text-3xl">{item.title}</h1>
      {item.description && <p className="mt-2 text-muted-foreground">{item.description}</p>}
      <p className="mt-1 text-xs text-muted-foreground">
        {item.category} · {Math.round(item.sizeBytes / 1024)} KB
      </p>

      <div className="mt-6">
        {isMobile ? (
          // Many mobile browsers don't render PDF in <embed>; fall back to a download link.
          <a
            href={fileUrl}
            className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
          >
            Download PDF
          </a>
        ) : (
          <embed
            src={fileUrl}
            type="application/pdf"
            className="h-[80vh] w-full rounded-md border"
            aria-label={item.title}
          />
        )}
      </div>
    </div>
  );
}
