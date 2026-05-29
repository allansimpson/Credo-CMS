import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { publicSermonsApi, type BookCount } from "@/lib/api/publicSermons";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { Eyebrow, Headline } from "@/components/public";
import { SeriesViewBar } from "@/components/sermons/SeriesViewBar";
import {
  BIBLE_BOOKS,
  GENRE_DISPLAY,
  OT_GENRE_ORDER,
  NT_GENRE_ORDER,
  type BookGenre,
  type BibleBookInfo,
} from "@/lib/bible/books";

interface BookWithCount extends BibleBookInfo {
  count: number;
}

export function SermonsByBookIndexPage() {
  const { settings } = useSiteSettings();
  const [counts, setCounts] = useState<BookCount[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    publicSermonsApi.byBookIndex()
      .then((bookCounts) => { if (!cancelled) setCounts(bookCounts); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  const countMap = useMemo(() => {
    const m = new Map<number, number>();
    for (const c of counts) m.set(c.bookValue, c.count);
    return m;
  }, [counts]);

  const books: BookWithCount[] = useMemo(
    () => BIBLE_BOOKS.map((b) => ({ ...b, count: countMap.get(b.book) ?? 0 })),
    [countMap],
  );

  const totalSermons = books.reduce((n, b) => n + b.count, 0);
  const preachedCount = books.filter((b) => b.count > 0).length;
  const otBooks = books.filter((b) => b.testament === "OldTestament");
  const ntBooks = books.filter((b) => b.testament === "NewTestament");
  const otPreached = otBooks.filter((b) => b.count > 0).length;
  const ntPreached = ntBooks.filter((b) => b.count > 0).length;

  return (
    <div>
      <SeoTags
        title={`Sermons by Book · ${settings?.churchName ?? ""}`}
        description="Search the canon. Browse sermons organized by book of the Bible."
      />

      {/* ── Header ────────────────────────────────────────────── */}
      <header className="mx-auto px-6 py-10 md:px-14 md:py-12" style={{ maxWidth: 1180 }}>
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <Eyebrow accent>Sermons · By Book</Eyebrow>
            <Headline as="h1" size="display" className="mt-3">
              Search the canon.
            </Headline>
          </div>
          <p className="font-mono text-[11.5px] uppercase tracking-[0.08em] text-muted">
            {totalSermons} sermons · {preachedCount} of 66 books
          </p>
        </div>
      </header>

      {loading && (
        <div className="mx-auto px-6 py-8 md:px-14" style={{ maxWidth: 1180 }}>
          <p className="text-muted">Loading…</p>
        </div>
      )}

      {/* ── Filter bar (shared across all sermon browse tabs) ─── */}
      <SeriesViewBar active="by-book" placeholder="Try 'Luke 14' or 'Psalms 23'" />

      {/* ── Atlas ─────────────────────────────────────────────── */}
      {!loading && (
        <div className="mx-auto px-6 py-10 md:px-14" style={{ maxWidth: 1180 }}>
          <div className="grid gap-16 md:grid-cols-2">
            <TestamentColumn
              title="Old Testament"
              preached={otPreached}
              total={39}
              books={otBooks}
              genreOrder={OT_GENRE_ORDER}
            />
            <TestamentColumn
              title="New Testament"
              preached={ntPreached}
              total={27}
              books={ntBooks}
              genreOrder={NT_GENRE_ORDER}
            />
          </div>
        </div>
      )}
    </div>
  );
}

function TestamentColumn({
  title,
  preached,
  total,
  books,
  genreOrder,
}: {
  title: string;
  preached: number;
  total: number;
  books: BookWithCount[];
  genreOrder: BookGenre[];
}) {
  const byGenre = useMemo(() => {
    const m = new Map<BookGenre, BookWithCount[]>();
    for (const b of books) {
      const arr = m.get(b.genre) ?? [];
      arr.push(b);
      m.set(b.genre, arr);
    }
    return m;
  }, [books]);

  return (
    <div>
      <h2 className="font-heading text-[32px] font-semibold">
        {title}{" "}
        <span className="font-mono text-[13px] font-normal text-muted">
          {preached} of {total}
        </span>
      </h2>
      {genreOrder.map((genre) => {
        const genreBooks = byGenre.get(genre);
        if (!genreBooks?.length) return null;
        return (
          <GenreSection key={genre} genre={genre} books={genreBooks} />
        );
      })}
    </div>
  );
}

function GenreSection({ genre, books }: { genre: BookGenre; books: BookWithCount[] }) {
  return (
    <div className="mt-6">
      <div className="flex items-baseline gap-2 border-b border-border pb-2.5">
        <span className="font-mono text-[11px] font-medium uppercase tracking-[0.18em] text-muted">
          {GENRE_DISPLAY[genre]}
        </span>
        <span className="font-mono text-[11px] text-muted/55">· {books.length}</span>
      </div>
      <div>
        {books.map((b) => (
          <BookIndexRow key={b.book} book={b} />
        ))}
      </div>
    </div>
  );
}

function BookIndexRow({ book }: { book: BookWithCount }) {
  const hasContent = book.count > 0;

  if (!hasContent) {
    return (
      <div
        className="grid items-baseline gap-4 border-b border-border-soft py-3"
        style={{ gridTemplateColumns: "1fr auto", opacity: 0.55 }}
      >
        <span className="font-heading text-[19px] font-medium text-fg-soft" style={{ letterSpacing: "-0.01em" }}>
          {book.name}
        </span>
        <span className="font-mono text-[13px] tabular-nums text-muted/40">—</span>
      </div>
    );
  }

  return (
    <Link
      to={`/sermons/by-book/${book.slug}`}
      className="group grid items-baseline gap-4 border-b border-border-soft py-3 hover:bg-panel-alt/30"
      style={{ gridTemplateColumns: "1fr auto" }}
    >
      <span className="font-heading text-[19px] font-medium group-hover:underline" style={{ letterSpacing: "-0.01em" }}>
        {book.name}
      </span>
      <span className="font-mono text-[13px] font-semibold tabular-nums text-accent">
        {book.count}
      </span>
    </Link>
  );
}
