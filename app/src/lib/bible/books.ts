/**
 * Static reference data for the 66 books of the Bible. Mirrors the
 * server-side Domain.Bible.BibleBooks data so the SPA can validate and
 * render references without a round-trip.
 *
 * `book` matches the server's enum value (1..66). Test asserts canonical
 * counts (39 OT, 27 NT, 66 total) so drift here is caught.
 */
export type Testament = "OldTestament" | "NewTestament";

export type BookGenre =
  | "Law" | "History" | "Wisdom" | "MajorProphets" | "MinorProphets"
  | "Gospels" | "NTHistory" | "PaulineLetters" | "GeneralLetters" | "Apocalyptic";

export const GENRE_DISPLAY: Record<BookGenre, string> = {
  Law: "Law",
  History: "History",
  Wisdom: "Wisdom",
  MajorProphets: "Major Prophets",
  MinorProphets: "Minor Prophets",
  Gospels: "Gospels",
  NTHistory: "History",
  PaulineLetters: "Pauline Letters",
  GeneralLetters: "General Letters",
  Apocalyptic: "Apocalyptic",
};

export const OT_GENRE_ORDER: BookGenre[] = ["Law", "History", "Wisdom", "MajorProphets", "MinorProphets"];
export const NT_GENRE_ORDER: BookGenre[] = ["Gospels", "NTHistory", "PaulineLetters", "GeneralLetters", "Apocalyptic"];

export interface BibleBookInfo {
  /** Server-side enum value. */
  book: number;
  name: string;
  abbreviation: string;
  testament: Testament;
  chapterCount: number;
  slug: string;
  genre: BookGenre;
}

export const BIBLE_BOOKS: BibleBookInfo[] = [
  // Old Testament (39)
  { book: 1, name: "Genesis", abbreviation: "Gen", testament: "OldTestament", chapterCount: 50, slug: "genesis", genre: "Law" },
  { book: 2, name: "Exodus", abbreviation: "Exod", testament: "OldTestament", chapterCount: 40, slug: "exodus", genre: "Law" },
  { book: 3, name: "Leviticus", abbreviation: "Lev", testament: "OldTestament", chapterCount: 27, slug: "leviticus", genre: "Law" },
  { book: 4, name: "Numbers", abbreviation: "Num", testament: "OldTestament", chapterCount: 36, slug: "numbers", genre: "Law" },
  { book: 5, name: "Deuteronomy", abbreviation: "Deut", testament: "OldTestament", chapterCount: 34, slug: "deuteronomy", genre: "Law" },
  { book: 6, name: "Joshua", abbreviation: "Josh", testament: "OldTestament", chapterCount: 24, slug: "joshua", genre: "History" },
  { book: 7, name: "Judges", abbreviation: "Judg", testament: "OldTestament", chapterCount: 21, slug: "judges", genre: "History" },
  { book: 8, name: "Ruth", abbreviation: "Ruth", testament: "OldTestament", chapterCount: 4, slug: "ruth", genre: "History" },
  { book: 9, name: "1 Samuel", abbreviation: "1 Sam", testament: "OldTestament", chapterCount: 31, slug: "1-samuel", genre: "History" },
  { book: 10, name: "2 Samuel", abbreviation: "2 Sam", testament: "OldTestament", chapterCount: 24, slug: "2-samuel", genre: "History" },
  { book: 11, name: "1 Kings", abbreviation: "1 Kgs", testament: "OldTestament", chapterCount: 22, slug: "1-kings", genre: "History" },
  { book: 12, name: "2 Kings", abbreviation: "2 Kgs", testament: "OldTestament", chapterCount: 25, slug: "2-kings", genre: "History" },
  { book: 13, name: "1 Chronicles", abbreviation: "1 Chr", testament: "OldTestament", chapterCount: 29, slug: "1-chronicles", genre: "History" },
  { book: 14, name: "2 Chronicles", abbreviation: "2 Chr", testament: "OldTestament", chapterCount: 36, slug: "2-chronicles", genre: "History" },
  { book: 15, name: "Ezra", abbreviation: "Ezra", testament: "OldTestament", chapterCount: 10, slug: "ezra", genre: "History" },
  { book: 16, name: "Nehemiah", abbreviation: "Neh", testament: "OldTestament", chapterCount: 13, slug: "nehemiah", genre: "History" },
  { book: 17, name: "Esther", abbreviation: "Esth", testament: "OldTestament", chapterCount: 10, slug: "esther", genre: "History" },
  { book: 18, name: "Job", abbreviation: "Job", testament: "OldTestament", chapterCount: 42, slug: "job", genre: "Wisdom" },
  { book: 19, name: "Psalms", abbreviation: "Ps", testament: "OldTestament", chapterCount: 150, slug: "psalms", genre: "Wisdom" },
  { book: 20, name: "Proverbs", abbreviation: "Prov", testament: "OldTestament", chapterCount: 31, slug: "proverbs", genre: "Wisdom" },
  { book: 21, name: "Ecclesiastes", abbreviation: "Eccl", testament: "OldTestament", chapterCount: 12, slug: "ecclesiastes", genre: "Wisdom" },
  { book: 22, name: "Song of Solomon", abbreviation: "Song", testament: "OldTestament", chapterCount: 8, slug: "song-of-solomon", genre: "Wisdom" },
  { book: 23, name: "Isaiah", abbreviation: "Isa", testament: "OldTestament", chapterCount: 66, slug: "isaiah", genre: "MajorProphets" },
  { book: 24, name: "Jeremiah", abbreviation: "Jer", testament: "OldTestament", chapterCount: 52, slug: "jeremiah", genre: "MajorProphets" },
  { book: 25, name: "Lamentations", abbreviation: "Lam", testament: "OldTestament", chapterCount: 5, slug: "lamentations", genre: "MajorProphets" },
  { book: 26, name: "Ezekiel", abbreviation: "Ezek", testament: "OldTestament", chapterCount: 48, slug: "ezekiel", genre: "MajorProphets" },
  { book: 27, name: "Daniel", abbreviation: "Dan", testament: "OldTestament", chapterCount: 12, slug: "daniel", genre: "MajorProphets" },
  { book: 28, name: "Hosea", abbreviation: "Hos", testament: "OldTestament", chapterCount: 14, slug: "hosea", genre: "MinorProphets" },
  { book: 29, name: "Joel", abbreviation: "Joel", testament: "OldTestament", chapterCount: 3, slug: "joel", genre: "MinorProphets" },
  { book: 30, name: "Amos", abbreviation: "Amos", testament: "OldTestament", chapterCount: 9, slug: "amos", genre: "MinorProphets" },
  { book: 31, name: "Obadiah", abbreviation: "Obad", testament: "OldTestament", chapterCount: 1, slug: "obadiah", genre: "MinorProphets" },
  { book: 32, name: "Jonah", abbreviation: "Jon", testament: "OldTestament", chapterCount: 4, slug: "jonah", genre: "MinorProphets" },
  { book: 33, name: "Micah", abbreviation: "Mic", testament: "OldTestament", chapterCount: 7, slug: "micah", genre: "MinorProphets" },
  { book: 34, name: "Nahum", abbreviation: "Nah", testament: "OldTestament", chapterCount: 3, slug: "nahum", genre: "MinorProphets" },
  { book: 35, name: "Habakkuk", abbreviation: "Hab", testament: "OldTestament", chapterCount: 3, slug: "habakkuk", genre: "MinorProphets" },
  { book: 36, name: "Zephaniah", abbreviation: "Zeph", testament: "OldTestament", chapterCount: 3, slug: "zephaniah", genre: "MinorProphets" },
  { book: 37, name: "Haggai", abbreviation: "Hag", testament: "OldTestament", chapterCount: 2, slug: "haggai", genre: "MinorProphets" },
  { book: 38, name: "Zechariah", abbreviation: "Zech", testament: "OldTestament", chapterCount: 14, slug: "zechariah", genre: "MinorProphets" },
  { book: 39, name: "Malachi", abbreviation: "Mal", testament: "OldTestament", chapterCount: 4, slug: "malachi", genre: "MinorProphets" },

  // New Testament (27)
  { book: 40, name: "Matthew", abbreviation: "Matt", testament: "NewTestament", chapterCount: 28, slug: "matthew", genre: "Gospels" },
  { book: 41, name: "Mark", abbreviation: "Mark", testament: "NewTestament", chapterCount: 16, slug: "mark", genre: "Gospels" },
  { book: 42, name: "Luke", abbreviation: "Luke", testament: "NewTestament", chapterCount: 24, slug: "luke", genre: "Gospels" },
  { book: 43, name: "John", abbreviation: "John", testament: "NewTestament", chapterCount: 21, slug: "john", genre: "Gospels" },
  { book: 44, name: "Acts", abbreviation: "Acts", testament: "NewTestament", chapterCount: 28, slug: "acts", genre: "NTHistory" },
  { book: 45, name: "Romans", abbreviation: "Rom", testament: "NewTestament", chapterCount: 16, slug: "romans", genre: "PaulineLetters" },
  { book: 46, name: "1 Corinthians", abbreviation: "1 Cor", testament: "NewTestament", chapterCount: 16, slug: "1-corinthians", genre: "PaulineLetters" },
  { book: 47, name: "2 Corinthians", abbreviation: "2 Cor", testament: "NewTestament", chapterCount: 13, slug: "2-corinthians", genre: "PaulineLetters" },
  { book: 48, name: "Galatians", abbreviation: "Gal", testament: "NewTestament", chapterCount: 6, slug: "galatians", genre: "PaulineLetters" },
  { book: 49, name: "Ephesians", abbreviation: "Eph", testament: "NewTestament", chapterCount: 6, slug: "ephesians", genre: "PaulineLetters" },
  { book: 50, name: "Philippians", abbreviation: "Phil", testament: "NewTestament", chapterCount: 4, slug: "philippians", genre: "PaulineLetters" },
  { book: 51, name: "Colossians", abbreviation: "Col", testament: "NewTestament", chapterCount: 4, slug: "colossians", genre: "PaulineLetters" },
  { book: 52, name: "1 Thessalonians", abbreviation: "1 Thess", testament: "NewTestament", chapterCount: 5, slug: "1-thessalonians", genre: "PaulineLetters" },
  { book: 53, name: "2 Thessalonians", abbreviation: "2 Thess", testament: "NewTestament", chapterCount: 3, slug: "2-thessalonians", genre: "PaulineLetters" },
  { book: 54, name: "1 Timothy", abbreviation: "1 Tim", testament: "NewTestament", chapterCount: 6, slug: "1-timothy", genre: "PaulineLetters" },
  { book: 55, name: "2 Timothy", abbreviation: "2 Tim", testament: "NewTestament", chapterCount: 4, slug: "2-timothy", genre: "PaulineLetters" },
  { book: 56, name: "Titus", abbreviation: "Titus", testament: "NewTestament", chapterCount: 3, slug: "titus", genre: "PaulineLetters" },
  { book: 57, name: "Philemon", abbreviation: "Phlm", testament: "NewTestament", chapterCount: 1, slug: "philemon", genre: "PaulineLetters" },
  { book: 58, name: "Hebrews", abbreviation: "Heb", testament: "NewTestament", chapterCount: 13, slug: "hebrews", genre: "PaulineLetters" },
  { book: 59, name: "James", abbreviation: "Jas", testament: "NewTestament", chapterCount: 5, slug: "james", genre: "GeneralLetters" },
  { book: 60, name: "1 Peter", abbreviation: "1 Pet", testament: "NewTestament", chapterCount: 5, slug: "1-peter", genre: "GeneralLetters" },
  { book: 61, name: "2 Peter", abbreviation: "2 Pet", testament: "NewTestament", chapterCount: 3, slug: "2-peter", genre: "GeneralLetters" },
  { book: 62, name: "1 John", abbreviation: "1 Jn", testament: "NewTestament", chapterCount: 5, slug: "1-john", genre: "GeneralLetters" },
  { book: 63, name: "2 John", abbreviation: "2 Jn", testament: "NewTestament", chapterCount: 1, slug: "2-john", genre: "GeneralLetters" },
  { book: 64, name: "3 John", abbreviation: "3 Jn", testament: "NewTestament", chapterCount: 1, slug: "3-john", genre: "GeneralLetters" },
  { book: 65, name: "Jude", abbreviation: "Jude", testament: "NewTestament", chapterCount: 1, slug: "jude", genre: "GeneralLetters" },
  { book: 66, name: "Revelation", abbreviation: "Rev", testament: "NewTestament", chapterCount: 22, slug: "revelation", genre: "Apocalyptic" },
];

const BY_BOOK = new Map(BIBLE_BOOKS.map((b) => [b.book, b]));
const BY_SLUG = new Map(BIBLE_BOOKS.map((b) => [b.slug, b]));

export function getBookInfo(book: number): BibleBookInfo | undefined {
  return BY_BOOK.get(book);
}

export function getBookBySlug(slug: string): BibleBookInfo | undefined {
  return BY_SLUG.get(slug);
}

export const OLD_TESTAMENT = BIBLE_BOOKS.filter((b) => b.testament === "OldTestament");
export const NEW_TESTAMENT = BIBLE_BOOKS.filter((b) => b.testament === "NewTestament");
