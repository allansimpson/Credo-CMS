import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { publicSermonsApi, type BookCount } from "@/lib/api/publicSermons";
import type { SermonListItem } from "@/lib/api/sermons";
import type { PagedResult } from "@/types/api";
import { SeoTags } from "@/components/shared/SeoTags";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { Eyebrow, Headline } from "@/components/public";
import { SeriesViewBar } from "@/components/sermons/SeriesViewBar";
import {
  BIBLE_BOOKS,
  GENRE_DISPLAY,
  OT_GENRE_ORDER,
  NT_GENRE_ORDER,
  getBookBySlug,
  type BookGenre,
  type BibleBookInfo,
} from "@/lib/bible/books";

interface BookWithCount extends BibleBookInfo {
  count: number;
}

export function SermonsByBookIndexPage() {
  const { settings } = useSiteSettings();
  const [searchParams, setSearchParams] = useSearchParams();
  const bookFilterSlug = searchParams.get("book");
  const bookFilter = bookFilterSlug ? getBookBySlug(bookFilterSlug) : undefined;
  const inBookView = !!bookFilter;

  const [counts, setCounts] = useState<BookCount[]>([]);
  const [loadingAtlas, setLoadingAtlas] = useState(true);
  const [searchText, setSearchText] = useState("");

  useEffect(() => {
    let cancelled = false;
    publicSermonsApi.byBookIndex()
      .then((bookCounts) => { if (!cancelled) setCounts(bookCounts); })
      .finally(() => { if (!cancelled) setLoadingAtlas(false); });
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

  // Live in-page search filters the atlas to books whose name matches.
  // Stays on this tab — the by-book surface searches BOOKS, not the
  // full sermon corpus (that's the Latest tab's job).
  const needle = searchText.trim().toLowerCase();
  const hasTextFilter = needle.length > 0;
  const nameMatches = hasTextFilter
    ? books.filter((b) => b.name.toLowerCase().includes(needle))
    : books;
  // Three result states distinguished by what kind of match we have:
  // (1) matched books that have sermons — render the atlas;
  // (2) matched books but every match has zero sermons — surface the
  //     "no sermons yet on …" panel listing the matched names;
  // (3) no name match at all — fall through to the fuzzy fallback /
  //     default "no matches" message.
  const matchedWithSermons = nameMatches.filter((b) => b.count > 0);
  const fuzzyMatches = useMemo(
    () => (hasTextFilter && nameMatches.length === 0 ? findCloseBookMatches(needle, books) : []),
    [hasTextFilter, nameMatches.length, needle, books],
  );
  const filteredOtBooks = hasTextFilter ? nameMatches.filter((b) => b.testament === "OldTestament") : otBooks;
  const filteredNtBooks = hasTextFilter ? nameMatches.filter((b) => b.testament === "NewTestament") : ntBooks;

  // Surface the active filter(s) via the search input's clear X.
  // Clearing returns to the unfiltered atlas: drops both the typed text
  // and the `?book=…` param. Mirrors the Latest tab's q-clears-filter
  // behavior so the bar feels consistent across the three browse tabs.
  const clearBookFilter = () => {
    const next = new URLSearchParams(searchParams);
    next.delete("book");
    setSearchParams(next);
  };
  const clearAllFilters = () => {
    setSearchText("");
    if (inBookView) clearBookFilter();
  };

  // Typing in the search returns the user to the atlas, regardless of
  // whether they're currently viewing a specific book's sermons. Without
  // this, a typed query is shadowed by the active book filter and the
  // search appears not to work from inside a book view.
  const handleSearchInput = (next: string) => {
    setSearchText(next);
    if (next.length > 0 && inBookView) clearBookFilter();
  };

  return (
    <div>
      <SeoTags
        title={inBookView
          ? `Sermons in ${bookFilter.name} · ${settings?.churchName ?? ""}`
          : `Sermons by Book · ${settings?.churchName ?? ""}`}
        description={inBookView
          ? `Sermons referencing ${bookFilter.name}.`
          : "Search the canon. Browse sermons organized by book of the Bible."}
      />

      {/* ── Header ────────────────────────────────────────────── */}
      <header className="mx-auto max-w-[1180px] px-6 py-10 md:px-14 md:py-12">
        <Eyebrow accent>Sermons · By Book</Eyebrow>
        <Headline as="h1" size="display" className="mt-3">
          Search the canon.
        </Headline>
        <p className="mt-4 hidden font-mono text-[11px] uppercase tracking-[0.14em] text-muted md:block">
          {inBookView
            ? `Showing sermons in ${bookFilter.name}`
            : `${totalSermons} sermons · ${preachedCount} of 66 books`}
        </p>
      </header>

      {/* ── Filter bar (shared across all sermon browse tabs) ───
          On By Book the search filters the atlas to books whose name
          matches — keeps the user on this tab instead of bouncing to
          Latest's full-text search. `onSubmit` is a no-op because the
          filter applies as you type; pressing Enter just prevents the
          default form behavior. */}
      <SeriesViewBar
        active="by-book"
        placeholder="Search books — 'John', 'Psalms', 'Romans'"
        value={searchText}
        onChange={handleSearchInput}
        onSubmit={() => { /* filtering is real-time */ }}
        hasAppliedQuery={inBookView || hasTextFilter}
        onClear={clearAllFilters}
      />

      {/* ── Body ──────────────────────────────────────────────── */}
      {inBookView ? (
        <BookSermonsList book={bookFilter} />
      ) : hasTextFilter && nameMatches.length === 0 ? (
        <NoBookMatches
          needle={searchText.trim()}
          suggestions={fuzzyMatches}
          onClear={clearAllFilters}
        />
      ) : hasTextFilter && matchedWithSermons.length === 0 ? (
        <NoSermonsYet
          books={nameMatches}
          onClear={clearAllFilters}
        />
      ) : (
        <Atlas
          loading={loadingAtlas}
          otBooks={filteredOtBooks}
          ntBooks={filteredNtBooks}
          otPreached={otPreached}
          ntPreached={ntPreached}
          hideEmptyBooks={hasTextFilter}
        />
      )}
    </div>
  );
}

// ─── Atlas (book index grid) ─────────────────────────────────────────────

function Atlas({
  loading,
  otBooks,
  ntBooks,
  otPreached,
  ntPreached,
  hideEmptyBooks = false,
}: {
  loading: boolean;
  otBooks: BookWithCount[];
  ntBooks: BookWithCount[];
  otPreached: number;
  ntPreached: number;
  /** When the user is filtering by name, omit books with no sermons
   * (and any testament column that ends up empty) so the result reads
   * as a focused match list, not a sparse table of dashes. */
  hideEmptyBooks?: boolean;
}) {
  if (loading) {
    return (
      <div className="mx-auto max-w-[1180px] px-6 py-8 md:px-14">
        <p className="text-muted">Loading…</p>
      </div>
    );
  }
  const otVisible = hideEmptyBooks ? otBooks.filter((b) => b.count > 0) : otBooks;
  const ntVisible = hideEmptyBooks ? ntBooks.filter((b) => b.count > 0) : ntBooks;
  return (
    <div className="mx-auto max-w-[1180px] px-6 py-10 md:px-14">
      <div className="grid gap-16 md:grid-cols-2">
        {otVisible.length > 0 && (
          <TestamentColumn
            title="Old Testament"
            preached={otPreached}
            total={39}
            books={otVisible}
            genreOrder={OT_GENRE_ORDER}
          />
        )}
        {ntVisible.length > 0 && (
          <TestamentColumn
            title="New Testament"
            preached={ntPreached}
            total={27}
            books={ntVisible}
            genreOrder={NT_GENRE_ORDER}
          />
        )}
      </div>
    </div>
  );
}

function NoBookMatches({
  needle,
  suggestions,
  onClear,
}: {
  needle: string;
  suggestions: BookWithCount[];
  onClear: () => void;
}) {
  const hasSuggestions = suggestions.length > 0;
  return (
    <div className="mx-auto max-w-[1180px] px-6 py-14 md:px-14">
      <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">No matches</p>
      <p className="mt-3 text-fg-soft">
        There are no books matching that search.
      </p>
      {hasSuggestions && (
        <p className="mt-2 text-sm text-muted">
          Did you mean{" "}
          {suggestions.map((b, i) => (
            <span key={b.book}>
              <Link
                to={`/sermons/by-book?book=${encodeURIComponent(b.slug)}`}
                className="text-accent hover:underline"
              >
                {b.name}
              </Link>
              {i < suggestions.length - 1 ? ", " : ""}
            </span>
          ))}
          ?
        </p>
      )}
      <button
        type="button"
        onClick={onClear}
        className="mt-4 inline-flex items-center border border-border-soft bg-background px-4 py-2 text-sm font-medium hover:bg-panel-alt"
        aria-label={`Clear Search for ${needle}`}
      >
        Clear Search
      </button>
    </div>
  );
}

/** Surface for the "matched a real book but it has zero sermons yet"
 * case. Listing the matched names answers the user's "I searched for
 * Habakkuk — where is it?" without dropping them on an empty atlas. */
function NoSermonsYet({
  books,
  onClear,
}: {
  books: BookWithCount[];
  onClear: () => void;
}) {
  const names = books.map((b) => b.name);
  const list =
    names.length === 1
      ? names[0]
      : names.length === 2
      ? `${names[0]} or ${names[1]}`
      : `${names.slice(0, -1).join(", ")}, or ${names[names.length - 1]}`;
  return (
    <div className="mx-auto max-w-[1180px] px-6 py-14 md:px-14">
      <p className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
        Not preached yet
      </p>
      <p className="mt-3 text-fg-soft">
        There {names.length === 1 ? "are" : "are"} currently no sermons on {list}. Check back later.
      </p>
      <button
        type="button"
        onClick={onClear}
        className="mt-4 inline-flex items-center border border-border-soft bg-background px-4 py-2 text-sm font-medium hover:bg-panel-alt"
      >
        Clear Search
      </button>
    </div>
  );
}

// ─── Fuzzy match helpers (typo-tolerant book-name suggestions) ──────────

/** Find up to 3 books whose name is within a small Levenshtein distance
 * of the needle. Only used when the strict substring filter returned
 * nothing — gives users a "did you mean Habakkuk?" hint when they
 * misspell. Threshold scales with needle length so single-letter typos
 * trigger on short book names without false-positives on long ones. */
function findCloseBookMatches(needle: string, books: BookWithCount[]): BookWithCount[] {
  if (needle.length < 3) return [];
  const threshold = needle.length <= 4 ? 1 : 2;
  const scored = books
    .map((b) => ({ book: b, dist: levenshtein(needle, b.name.toLowerCase()) }))
    .filter((x) => x.dist <= threshold)
    .sort((a, b) => a.dist - b.dist)
    .slice(0, 3);
  return scored.map((x) => x.book);
}

function levenshtein(a: string, b: string): number {
  if (a === b) return 0;
  if (a.length === 0) return b.length;
  if (b.length === 0) return a.length;
  // Two-row dynamic programming — O(min(m, n)) memory; fine for the
  // 66-item canon × short book names.
  const m = a.length;
  const n = b.length;
  let prev = new Array(n + 1);
  let curr = new Array(n + 1);
  for (let j = 0; j <= n; j++) prev[j] = j;
  for (let i = 1; i <= m; i++) {
    curr[0] = i;
    for (let j = 1; j <= n; j++) {
      const cost = a[i - 1] === b[j - 1] ? 0 : 1;
      curr[j] = Math.min(prev[j] + 1, curr[j - 1] + 1, prev[j - 1] + cost);
    }
    [prev, curr] = [curr, prev];
  }
  return prev[n];
}

// ─── Book sermons list (inline replacement for the old standalone page) ─

function BookSermonsList({ book }: { book: BibleBookInfo }) {
  const [data, setData] = useState<PagedResult<SermonListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    publicSermonsApi.byBook(book.slug, page)
      .then((d) => {
        if (cancelled) return;
        // Accumulate pages when the user hits Load more, so the list
        // grows instead of replacing on each page bump.
        setData((prev) =>
          prev && page > 1
            ? { ...d, items: [...prev.items, ...d.items] }
            : d,
        );
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [book.slug, page]);

  // Reset the accumulator when the book changes via the URL.
  useEffect(() => { setData(null); setPage(1); }, [book.slug]);

  return (
    <section className="mx-auto max-w-[1180px] px-6 py-10 md:px-14">
      <header className="mb-6 flex flex-wrap items-baseline justify-between gap-3">
        <div>
          <h2 className="font-heading text-3xl font-semibold tracking-tight">
            {book.name}
          </h2>
          <p className="mt-1 font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
            {book.testament === "OldTestament" ? "Old Testament" : "New Testament"} ·{" "}
            {book.chapterCount} chapters
            {data?.totalCount !== undefined && ` · ${data.totalCount} sermon${data.totalCount === 1 ? "" : "s"}`}
          </p>
        </div>
      </header>

      {loading && (!data || data.items.length === 0) && (
        <p className="text-muted">Loading…</p>
      )}
      {!loading && data && data.items.length === 0 && (
        <p className="text-muted">No sermons reference {book.name} yet.</p>
      )}

      {data && data.items.length > 0 && (
        <ul className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {data.items.map((s) => <SermonCard key={s.id} sermon={s} />)}
        </ul>
      )}

      {data && data.totalPages > page && (
        <div className="mt-8 flex justify-center">
          <button
            type="button"
            disabled={loading}
            onClick={() => setPage((p) => p + 1)}
            className="inline-flex h-10 items-center border border-border-soft px-5 text-sm font-medium hover:bg-panel-alt disabled:opacity-50"
          >
            {loading ? "Loading…" : "Load more"}
          </button>
        </div>
      )}
    </section>
  );
}

function SermonCard({ sermon }: { sermon: SermonListItem }) {
  return (
    <li className="border border-border bg-panel">
      <Link to={`/sermons/${encodeURIComponent(sermon.slug)}`} className="block group">
        {sermon.thumbnailBlobUrl ? (
          <picture>
            {sermon.thumbnailWebpBlobUrl && (
              <source srcSet={sermon.thumbnailWebpBlobUrl} type="image/webp" />
            )}
            <img
              src={sermon.thumbnailBlobUrl}
              alt=""
              className="aspect-video w-full object-cover"
            />
          </picture>
        ) : (
          <div className="aspect-video w-full bg-panel-alt" />
        )}
        <div className="p-4">
          <h3 className="line-clamp-2 font-heading text-base font-semibold group-hover:underline">
            {sermon.title}
          </h3>
          <p className="mt-2 font-mono text-[10.5px] uppercase tracking-[0.10em] text-muted">
            {sermon.speakerName ?? "—"} ·{" "}
            {new Date(sermon.publishedAt).toLocaleDateString("en-US", {
              month: "short", day: "numeric", year: "numeric",
            })}
          </p>
        </div>
      </Link>
    </li>
  );
}

// ─── Atlas internals (unchanged from previous) ────────────────────────────

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
      <div className="grid grid-cols-[1fr_auto] items-baseline gap-4 border-b border-border-soft py-3 opacity-55">
        <span className="font-heading text-[19px] font-medium tracking-[-0.01em] text-fg-soft">
          {book.name}
        </span>
        <span className="font-mono text-[13px] tabular-nums text-muted/40">—</span>
      </div>
    );
  }

  return (
    <Link
      to={`/sermons/by-book?book=${encodeURIComponent(book.slug)}`}
      className="group grid grid-cols-[1fr_auto] items-baseline gap-4 border-b border-border-soft py-3 hover:bg-panel-alt/30"
    >
      <span className="font-heading text-[19px] font-medium tracking-[-0.01em] group-hover:underline">
        {book.name}
      </span>
      <span className="font-mono text-[13px] font-semibold tabular-nums text-accent">
        {book.count}
      </span>
    </Link>
  );
}
