namespace CredoCms.Domain.Bible;

public enum Testament
{
    OldTestament = 0,
    NewTestament = 1,
}

/// <summary>
/// Stable identifier for each of the 66 books. Used as the persisted enum
/// value in <c>ScriptureReference.Book</c>; the canonical numeric ordering
/// also drives display order on the Browse-by-Book grid.
/// </summary>
public enum BibleBook
{
    // Old Testament (39)
    Genesis = 1, Exodus = 2, Leviticus = 3, Numbers = 4, Deuteronomy = 5,
    Joshua = 6, Judges = 7, Ruth = 8,
    FirstSamuel = 9, SecondSamuel = 10, FirstKings = 11, SecondKings = 12,
    FirstChronicles = 13, SecondChronicles = 14,
    Ezra = 15, Nehemiah = 16, Esther = 17,
    Job = 18, Psalms = 19, Proverbs = 20, Ecclesiastes = 21, SongOfSolomon = 22,
    Isaiah = 23, Jeremiah = 24, Lamentations = 25, Ezekiel = 26, Daniel = 27,
    Hosea = 28, Joel = 29, Amos = 30, Obadiah = 31, Jonah = 32,
    Micah = 33, Nahum = 34, Habakkuk = 35, Zephaniah = 36,
    Haggai = 37, Zechariah = 38, Malachi = 39,

    // New Testament (27)
    Matthew = 40, Mark = 41, Luke = 42, John = 43, Acts = 44,
    Romans = 45, FirstCorinthians = 46, SecondCorinthians = 47,
    Galatians = 48, Ephesians = 49, Philippians = 50, Colossians = 51,
    FirstThessalonians = 52, SecondThessalonians = 53,
    FirstTimothy = 54, SecondTimothy = 55, Titus = 56, Philemon = 57,
    Hebrews = 58, James = 59,
    FirstPeter = 60, SecondPeter = 61,
    FirstJohn = 62, SecondJohn = 63, ThirdJohn = 64,
    Jude = 65, Revelation = 66,
}

public sealed record BibleBookInfo(
    BibleBook Book,
    string Name,
    string Abbreviation,
    Testament Testament,
    int ChapterCount,
    string Slug);

/// <summary>
/// Static reference data for all 66 canonical books. Source of truth for
/// chapter counts, abbreviations, slugs, and testament classification.
/// </summary>
public static class BibleBooks
{
    public static readonly IReadOnlyList<BibleBookInfo> All = new[]
    {
        // Old Testament
        new BibleBookInfo(BibleBook.Genesis,        "Genesis",         "Gen",  Testament.OldTestament, 50,  "genesis"),
        new BibleBookInfo(BibleBook.Exodus,         "Exodus",          "Exod", Testament.OldTestament, 40,  "exodus"),
        new BibleBookInfo(BibleBook.Leviticus,      "Leviticus",       "Lev",  Testament.OldTestament, 27,  "leviticus"),
        new BibleBookInfo(BibleBook.Numbers,        "Numbers",         "Num",  Testament.OldTestament, 36,  "numbers"),
        new BibleBookInfo(BibleBook.Deuteronomy,    "Deuteronomy",     "Deut", Testament.OldTestament, 34,  "deuteronomy"),
        new BibleBookInfo(BibleBook.Joshua,         "Joshua",          "Josh", Testament.OldTestament, 24,  "joshua"),
        new BibleBookInfo(BibleBook.Judges,         "Judges",          "Judg", Testament.OldTestament, 21,  "judges"),
        new BibleBookInfo(BibleBook.Ruth,           "Ruth",            "Ruth", Testament.OldTestament, 4,   "ruth"),
        new BibleBookInfo(BibleBook.FirstSamuel,    "1 Samuel",        "1 Sam",Testament.OldTestament, 31,  "1-samuel"),
        new BibleBookInfo(BibleBook.SecondSamuel,   "2 Samuel",        "2 Sam",Testament.OldTestament, 24,  "2-samuel"),
        new BibleBookInfo(BibleBook.FirstKings,     "1 Kings",         "1 Kgs",Testament.OldTestament, 22,  "1-kings"),
        new BibleBookInfo(BibleBook.SecondKings,    "2 Kings",         "2 Kgs",Testament.OldTestament, 25,  "2-kings"),
        new BibleBookInfo(BibleBook.FirstChronicles, "1 Chronicles",   "1 Chr",Testament.OldTestament, 29,  "1-chronicles"),
        new BibleBookInfo(BibleBook.SecondChronicles,"2 Chronicles",   "2 Chr",Testament.OldTestament, 36,  "2-chronicles"),
        new BibleBookInfo(BibleBook.Ezra,           "Ezra",            "Ezra", Testament.OldTestament, 10,  "ezra"),
        new BibleBookInfo(BibleBook.Nehemiah,       "Nehemiah",        "Neh",  Testament.OldTestament, 13,  "nehemiah"),
        new BibleBookInfo(BibleBook.Esther,         "Esther",          "Esth", Testament.OldTestament, 10,  "esther"),
        new BibleBookInfo(BibleBook.Job,            "Job",             "Job",  Testament.OldTestament, 42,  "job"),
        new BibleBookInfo(BibleBook.Psalms,         "Psalms",          "Ps",   Testament.OldTestament, 150, "psalms"),
        new BibleBookInfo(BibleBook.Proverbs,       "Proverbs",        "Prov", Testament.OldTestament, 31,  "proverbs"),
        new BibleBookInfo(BibleBook.Ecclesiastes,   "Ecclesiastes",    "Eccl", Testament.OldTestament, 12,  "ecclesiastes"),
        new BibleBookInfo(BibleBook.SongOfSolomon,  "Song of Solomon", "Song", Testament.OldTestament, 8,   "song-of-solomon"),
        new BibleBookInfo(BibleBook.Isaiah,         "Isaiah",          "Isa",  Testament.OldTestament, 66,  "isaiah"),
        new BibleBookInfo(BibleBook.Jeremiah,       "Jeremiah",        "Jer",  Testament.OldTestament, 52,  "jeremiah"),
        new BibleBookInfo(BibleBook.Lamentations,   "Lamentations",    "Lam",  Testament.OldTestament, 5,   "lamentations"),
        new BibleBookInfo(BibleBook.Ezekiel,        "Ezekiel",         "Ezek", Testament.OldTestament, 48,  "ezekiel"),
        new BibleBookInfo(BibleBook.Daniel,         "Daniel",          "Dan",  Testament.OldTestament, 12,  "daniel"),
        new BibleBookInfo(BibleBook.Hosea,          "Hosea",           "Hos",  Testament.OldTestament, 14,  "hosea"),
        new BibleBookInfo(BibleBook.Joel,           "Joel",            "Joel", Testament.OldTestament, 3,   "joel"),
        new BibleBookInfo(BibleBook.Amos,           "Amos",            "Amos", Testament.OldTestament, 9,   "amos"),
        new BibleBookInfo(BibleBook.Obadiah,        "Obadiah",         "Obad", Testament.OldTestament, 1,   "obadiah"),
        new BibleBookInfo(BibleBook.Jonah,          "Jonah",           "Jon",  Testament.OldTestament, 4,   "jonah"),
        new BibleBookInfo(BibleBook.Micah,          "Micah",           "Mic",  Testament.OldTestament, 7,   "micah"),
        new BibleBookInfo(BibleBook.Nahum,          "Nahum",           "Nah",  Testament.OldTestament, 3,   "nahum"),
        new BibleBookInfo(BibleBook.Habakkuk,       "Habakkuk",        "Hab",  Testament.OldTestament, 3,   "habakkuk"),
        new BibleBookInfo(BibleBook.Zephaniah,      "Zephaniah",       "Zeph", Testament.OldTestament, 3,   "zephaniah"),
        new BibleBookInfo(BibleBook.Haggai,         "Haggai",          "Hag",  Testament.OldTestament, 2,   "haggai"),
        new BibleBookInfo(BibleBook.Zechariah,      "Zechariah",       "Zech", Testament.OldTestament, 14,  "zechariah"),
        new BibleBookInfo(BibleBook.Malachi,        "Malachi",         "Mal",  Testament.OldTestament, 4,   "malachi"),

        // New Testament
        new BibleBookInfo(BibleBook.Matthew,        "Matthew",         "Matt", Testament.NewTestament, 28,  "matthew"),
        new BibleBookInfo(BibleBook.Mark,           "Mark",            "Mark", Testament.NewTestament, 16,  "mark"),
        new BibleBookInfo(BibleBook.Luke,           "Luke",            "Luke", Testament.NewTestament, 24,  "luke"),
        new BibleBookInfo(BibleBook.John,           "John",            "John", Testament.NewTestament, 21,  "john"),
        new BibleBookInfo(BibleBook.Acts,           "Acts",            "Acts", Testament.NewTestament, 28,  "acts"),
        new BibleBookInfo(BibleBook.Romans,         "Romans",          "Rom",  Testament.NewTestament, 16,  "romans"),
        new BibleBookInfo(BibleBook.FirstCorinthians,"1 Corinthians",  "1 Cor",Testament.NewTestament, 16,  "1-corinthians"),
        new BibleBookInfo(BibleBook.SecondCorinthians,"2 Corinthians", "2 Cor",Testament.NewTestament, 13,  "2-corinthians"),
        new BibleBookInfo(BibleBook.Galatians,      "Galatians",       "Gal",  Testament.NewTestament, 6,   "galatians"),
        new BibleBookInfo(BibleBook.Ephesians,      "Ephesians",       "Eph",  Testament.NewTestament, 6,   "ephesians"),
        new BibleBookInfo(BibleBook.Philippians,    "Philippians",     "Phil", Testament.NewTestament, 4,   "philippians"),
        new BibleBookInfo(BibleBook.Colossians,     "Colossians",      "Col",  Testament.NewTestament, 4,   "colossians"),
        new BibleBookInfo(BibleBook.FirstThessalonians, "1 Thessalonians", "1 Thess", Testament.NewTestament, 5,   "1-thessalonians"),
        new BibleBookInfo(BibleBook.SecondThessalonians,"2 Thessalonians", "2 Thess", Testament.NewTestament, 3,   "2-thessalonians"),
        new BibleBookInfo(BibleBook.FirstTimothy,   "1 Timothy",       "1 Tim",Testament.NewTestament, 6,   "1-timothy"),
        new BibleBookInfo(BibleBook.SecondTimothy,  "2 Timothy",       "2 Tim",Testament.NewTestament, 4,   "2-timothy"),
        new BibleBookInfo(BibleBook.Titus,          "Titus",           "Titus",Testament.NewTestament, 3,   "titus"),
        new BibleBookInfo(BibleBook.Philemon,       "Philemon",        "Phlm", Testament.NewTestament, 1,   "philemon"),
        new BibleBookInfo(BibleBook.Hebrews,        "Hebrews",         "Heb",  Testament.NewTestament, 13,  "hebrews"),
        new BibleBookInfo(BibleBook.James,          "James",           "Jas",  Testament.NewTestament, 5,   "james"),
        new BibleBookInfo(BibleBook.FirstPeter,     "1 Peter",         "1 Pet",Testament.NewTestament, 5,   "1-peter"),
        new BibleBookInfo(BibleBook.SecondPeter,    "2 Peter",         "2 Pet",Testament.NewTestament, 3,   "2-peter"),
        new BibleBookInfo(BibleBook.FirstJohn,      "1 John",          "1 Jn", Testament.NewTestament, 5,   "1-john"),
        new BibleBookInfo(BibleBook.SecondJohn,     "2 John",          "2 Jn", Testament.NewTestament, 1,   "2-john"),
        new BibleBookInfo(BibleBook.ThirdJohn,      "3 John",          "3 Jn", Testament.NewTestament, 1,   "3-john"),
        new BibleBookInfo(BibleBook.Jude,           "Jude",            "Jude", Testament.NewTestament, 1,   "jude"),
        new BibleBookInfo(BibleBook.Revelation,     "Revelation",      "Rev",  Testament.NewTestament, 22,  "revelation"),
    };

    private static readonly Dictionary<BibleBook, BibleBookInfo> ByEnum =
        All.ToDictionary(b => b.Book);

    private static readonly Dictionary<string, BibleBookInfo> BySlug =
        All.ToDictionary(b => b.Slug, StringComparer.OrdinalIgnoreCase);

    public static BibleBookInfo Get(BibleBook book) => ByEnum[book];

    public static BibleBookInfo? FindBySlug(string slug) =>
        BySlug.TryGetValue(slug ?? string.Empty, out var info) ? info : null;
}
