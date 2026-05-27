import { getBookInfo } from "@/lib/bible/books";

export interface ScriptureReference {
  book: number;
  chapterStart: number;
  /** Null = whole chapter. */
  verseStart: number | null;
  /** Null = same as chapterStart. */
  chapterEnd: number | null;
  /** Null = end of chapterEnd. */
  verseEnd: number | null;
}

/**
 * Canonical-form formatter:
 *   "Romans 8"               whole chapter
 *   "Psalm 23"               whole chapter; book name special-cased ("Psalms" → "Psalm" when single chapter)
 *   "Matthew 5:1–7:29"       cross-chapter range with verses
 *   "1 John 2:15–17"         within-chapter range
 *   "Genesis 1:1"            single verse
 * Always uses en-dash (–), never hyphen.
 */
export function formatScriptureReference(ref: ScriptureReference): string {
  const info = getBookInfo(ref.book);
  if (!info) return "";

  const bookName = info.name === "Psalms" && (ref.chapterEnd === null || ref.chapterEnd === ref.chapterStart)
    ? "Psalm"
    : info.name;

  const startChapter = ref.chapterStart;
  const endChapter = ref.chapterEnd ?? ref.chapterStart;
  const sameChapter = startChapter === endChapter;

  // Whole-chapter (or whole-chapter-range)
  if (ref.verseStart === null) {
    return sameChapter ? `${bookName} ${startChapter}` : `${bookName} ${startChapter}–${endChapter}`;
  }

  // Single verse
  if (sameChapter && (ref.verseEnd === null || ref.verseEnd === ref.verseStart)) {
    return `${bookName} ${startChapter}:${ref.verseStart}`;
  }

  // Same chapter, verse range
  if (sameChapter) {
    return `${bookName} ${startChapter}:${ref.verseStart}–${ref.verseEnd ?? ""}`;
  }

  // Cross-chapter
  const endPart = ref.verseEnd === null ? `${endChapter}` : `${endChapter}:${ref.verseEnd}`;
  return `${bookName} ${startChapter}:${ref.verseStart}–${endPart}`;
}

/**
 * Validates a reference against book metadata.
 * Returns error message on failure, null on success.
 */
export function validateScriptureReference(ref: ScriptureReference): string | null {
  const info = getBookInfo(ref.book);
  if (!info) return "Unknown book.";
  if (ref.chapterStart < 1 || ref.chapterStart > info.chapterCount)
    return `Chapter ${ref.chapterStart} is out of range for ${info.name} (1–${info.chapterCount}).`;
  if (ref.verseStart !== null && ref.verseStart < 1)
    return "Starting verse must be ≥ 1.";

  const endChapter = ref.chapterEnd ?? ref.chapterStart;
  if (endChapter < ref.chapterStart) return "Ending chapter must be ≥ starting chapter.";
  if (endChapter > info.chapterCount)
    return `Chapter ${endChapter} is out of range for ${info.name} (1–${info.chapterCount}).`;

  if (ref.verseEnd !== null) {
    if (ref.verseEnd < 1) return "Ending verse must be ≥ 1.";
    if (ref.chapterStart === endChapter
        && ref.verseStart !== null
        && ref.verseEnd < ref.verseStart) {
      return "Ending verse must be ≥ starting verse within the same chapter.";
    }
  }
  return null;
}
