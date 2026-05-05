import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { publicSermonsApi, type BookCount } from "@/lib/api/publicSermons";
import { SeoTags } from "@/components/shared/SeoTags";

export function SermonsByBookIndexPage() {
  const [items, setItems] = useState<BookCount[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    publicSermonsApi.byBookIndex()
      .then((d) => { if (!cancelled) setItems(d); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  const ot = items.filter((b) => b.testament === "OldTestament");
  const nt = items.filter((b) => b.testament === "NewTestament");

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <SeoTags title="Sermons by Book" description="Browse sermons by book of the Bible." />
      <h1 className="text-3xl font-bold sm:text-4xl">Sermons by Book</h1>

      {loading && <p className="mt-6 text-muted-foreground">Loading…</p>}

      <div className="mt-6 grid gap-8 sm:grid-cols-2">
        <BookList title="Old Testament" books={ot} />
        <BookList title="New Testament" books={nt} />
      </div>
    </div>
  );
}

function BookList({ title, books }: { title: string; books: BookCount[] }) {
  return (
    <section>
      <h2 className="text-xl font-semibold">{title}</h2>
      <ul className="mt-3 space-y-1">
        {books.map((b) => {
          const hasContent = b.count > 0;
          return (
            <li key={b.bookValue}>
              {hasContent ? (
                <Link to={`/sermons/by-book/${b.slug}`}
                  className="flex items-center justify-between py-1 text-sm hover:text-primary">
                  <span>{b.name}</span>
                  <span className="text-xs text-muted-foreground">{b.count}</span>
                </Link>
              ) : (
                <div className="flex items-center justify-between py-1 text-sm text-muted-foreground/60">
                  <span>{b.name}</span>
                  <span className="text-xs">—</span>
                </div>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
