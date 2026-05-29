using CredoCms.Application.Common;
using CredoCms.Application.Scripture;
using CredoCms.Application.Sermons;
using CredoCms.Application.Tags;
using CredoCms.Domain.Bible;
using CredoCms.Domain.Sermons;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Sermons;

public sealed class SermonRepository : ISermonRepository
{
    private const string ScriptureParentEntityType = nameof(Sermon);

    private readonly ApplicationDbContext _db;
    public SermonRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<SermonListItemDto>> ListAsync(SermonListQuery query, CancellationToken ct = default)
    {
        IQueryable<Sermon> q = query.IncludeDeleted ? _db.Sermons.IgnoreQueryFilters() : _db.Sermons;
        if (query.IncludeDeleted) q = q.Where(s => s.IsDeleted);

        if (query.PublishedOnly == true) q = q.Where(s => s.IsPublished);
        if (query.SermonSeriesId is { } seriesId) q = q.Where(s => s.SermonSeriesId == seriesId);
        if (query.SpeakerLeaderId is { } leaderId) q = q.Where(s => s.SpeakerLeaderId == leaderId);

        // Parsed-search bucket captured at the outer scope so both the WHERE
        // clauses and the ORDER BY rank can reference the same date values.
        ParsedDateSearch parsedSearch = default;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var trimmed = query.Search.Trim();
            var pattern = $"%{trimmed}%";

            // Date-shape detection — drives extra OR clauses against
            // PublishedAt so admins can search "10/19/2024", "Oct 2024",
            // "5/24", "2024", etc. and pull dated rows alongside text matches.
            parsedSearch = ParseSearchDate(trimmed);

            // Service-type label match. "AM Worship" → AmWorship. "Worship"
            // → AM + PM Worship. "Wednesday" → WednesdayNight. Lets admins
            // search the Type column by what they see, not the enum name.
            var typeMatches = MatchServiceTypes(trimmed);

            q = q.Where(s =>
                EF.Functions.Like(s.Title, pattern)
                || EF.Functions.Like(s.Slug, pattern)
                || (s.SpeakerNameFreeText != null && EF.Functions.Like(s.SpeakerNameFreeText, pattern))
                || (s.SpeakerLeaderId != null && _db.Leaders.Any(l => l.Id == s.SpeakerLeaderId && EF.Functions.Like(l.FullName, pattern)))
                // Service-type matches — EF translates Contains() on an
                // in-memory array to SQL IN (...).
                || (typeMatches.Length > 0 && typeMatches.Contains(s.ServiceType))
                // Date matches — translated via SQL DATEPART so the index
                // on PublishedAt still gets useful pruning at SQL level.
                || (parsedSearch.FullDate.HasValue
                    && s.PublishedAt.Year == parsedSearch.FullDate.Value.Year
                    && s.PublishedAt.Month == parsedSearch.FullDate.Value.Month
                    && s.PublishedAt.Day == parsedSearch.FullDate.Value.Day)
                || (parsedSearch.MonthYear.HasValue
                    && s.PublishedAt.Year == parsedSearch.MonthYear.Value.Year
                    && s.PublishedAt.Month == parsedSearch.MonthYear.Value.Month)
                || (parsedSearch.MonthDay.HasValue
                    && s.PublishedAt.Month == parsedSearch.MonthDay.Value.month
                    && s.PublishedAt.Day == parsedSearch.MonthDay.Value.day)
                || (parsedSearch.YearOnly.HasValue && s.PublishedAt.Year == parsedSearch.YearOnly.Value)
                || (parsedSearch.MonthOnly.HasValue && s.PublishedAt.Month == parsedSearch.MonthOnly.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.TagSlug))
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Slug == query.TagSlug, ct).ConfigureAwait(false);
            if (tag is null)
                return new PagedResult<SermonListItemDto>(Array.Empty<SermonListItemDto>(), 0, query.Page, query.PageSize);
            var tagId = tag.Id;
            q = q.Where(s => _db.SermonTags.Any(t => t.SermonId == s.Id && t.TagId == tagId));
        }

        if (query.BookFilter is { } bookValue)
        {
            var book = (BibleBook)bookValue;
            q = q.Where(s => _db.ScriptureReferences.Any(r =>
                r.ParentEntityType == ScriptureParentEntityType
                && r.ParentEntityId == s.Id
                && r.Book == book));
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        // When the search parsed as a date, rank rows that match that exact
        // date highest so they're never paginated off the first page by
        // tangential text matches. Within each tier sort by PublishedAt DESC.
        IOrderedQueryable<Sermon> ordered;
        if (parsedSearch.FullDate.HasValue)
        {
            var fd = parsedSearch.FullDate.Value;
            ordered = q.OrderByDescending(s =>
                    s.PublishedAt.Year == fd.Year && s.PublishedAt.Month == fd.Month && s.PublishedAt.Day == fd.Day ? 1 : 0)
                .ThenByDescending(s => s.PublishedAt);
        }
        else if (parsedSearch.MonthYear.HasValue)
        {
            var my = parsedSearch.MonthYear.Value;
            ordered = q.OrderByDescending(s =>
                    s.PublishedAt.Year == my.Year && s.PublishedAt.Month == my.Month ? 1 : 0)
                .ThenByDescending(s => s.PublishedAt);
        }
        else if (parsedSearch.MonthDay.HasValue)
        {
            var md = parsedSearch.MonthDay.Value;
            ordered = q.OrderByDescending(s =>
                    s.PublishedAt.Month == md.month && s.PublishedAt.Day == md.day ? 1 : 0)
                .ThenByDescending(s => s.PublishedAt);
        }
        else if (parsedSearch.YearOnly.HasValue)
        {
            var y = parsedSearch.YearOnly.Value;
            ordered = q.OrderByDescending(s => s.PublishedAt.Year == y ? 1 : 0)
                .ThenByDescending(s => s.PublishedAt);
        }
        else if (parsedSearch.MonthOnly.HasValue)
        {
            var m = parsedSearch.MonthOnly.Value;
            ordered = q.OrderByDescending(s => s.PublishedAt.Month == m ? 1 : 0)
                .ThenByDescending(s => s.PublishedAt);
        }
        else
        {
            ordered = q.OrderByDescending(s => s.PublishedAt);
        }

        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.Slug,
                s.Title,
                s.ThumbnailBlobUrl,
                s.ThumbnailWebpBlobUrl,
                s.PublishedAt,
                s.IsPublished,
                s.IsMembersOnly,
                s.IsDeleted,
                s.SermonSeriesId,
                SeriesTitle = s.SermonSeriesId == null ? null
                    : _db.SermonSeries.Where(ss => ss.Id == s.SermonSeriesId).Select(ss => ss.Title).FirstOrDefault(),
                SpeakerLeaderName = s.SpeakerLeaderId == null ? null
                    : _db.Leaders.Where(l => l.Id == s.SpeakerLeaderId).Select(l => l.FullName).FirstOrDefault(),
                s.SpeakerNameFreeText,
                s.ServiceType,
                s.YouTubeVideoId,
                s.DurationSeconds,
            })
            .ToListAsync(ct).ConfigureAwait(false);

        var items = rows.Select(r => new SermonListItemDto(
            r.Id, r.Slug, r.Title, r.ThumbnailBlobUrl, r.ThumbnailWebpBlobUrl,
            r.PublishedAt, r.IsPublished, r.IsMembersOnly, r.IsDeleted,
            r.SpeakerLeaderName ?? r.SpeakerNameFreeText,
            r.SeriesTitle, r.SermonSeriesId, r.ServiceType, r.YouTubeVideoId, r.DurationSeconds)).ToList();

        return new PagedResult<SermonListItemDto>(items, total, page, pageSize);
    }

    public async Task<SermonsByDayResponse> ListByDayAsync(SermonsByDayQuery query, bool includeMembersOnly, CancellationToken ct = default)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(query.Search);
        var hasTag = !string.IsNullOrWhiteSpace(query.TagSlug);
        // Search replaces year-browse (per Claude Design decision #4). Tag
        // composes with year. So the year filter only applies when search is
        // NOT active.
        var applyYear = query.Year is not null && !hasSearch;

        IQueryable<Sermon> q = _db.Sermons.Where(s => s.IsPublished);
        if (!includeMembersOnly) q = q.Where(s => !s.IsMembersOnly);
        if (query.ServiceType is { } st) q = q.Where(s => s.ServiceType == st);
        if (applyYear) q = q.Where(s => s.PublishedAt.Year == query.Year);
        if (hasSearch)
        {
            var pattern = $"%{query.Search!.Trim()}%";
            q = q.Where(s =>
                EF.Functions.Like(s.Title, pattern)
                || (s.SpeakerNameFreeText != null && EF.Functions.Like(s.SpeakerNameFreeText, pattern))
                || (s.SpeakerLeaderId != null && _db.Leaders.Any(l => l.Id == s.SpeakerLeaderId && EF.Functions.Like(l.FullName, pattern))));
        }
        if (hasTag)
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Slug == query.TagSlug, ct).ConfigureAwait(false);
            if (tag is null) return new([], 1, query.PageSize, 0, 0);
            var tagId = tag.Id;
            q = q.Where(s => _db.SermonTags.Any(t => t.SermonId == s.Id && t.TagId == tagId));
        }

        // Project to flat rows with date and speaker info.
        var allRows = await q
            .OrderByDescending(s => s.PublishedAt)
            .Select(s => new
            {
                s.Id, s.Slug, s.Title, s.ThumbnailBlobUrl, s.ThumbnailWebpBlobUrl,
                s.PublishedAt, s.IsPublished, s.IsMembersOnly, s.IsDeleted,
                s.SermonSeriesId, s.ServiceType, s.YouTubeVideoId, s.DurationSeconds,
                SeriesTitle = s.SermonSeriesId == null ? null
                    : _db.SermonSeries.Where(ss => ss.Id == s.SermonSeriesId).Select(ss => ss.Title).FirstOrDefault(),
                SpeakerLeaderName = s.SpeakerLeaderId == null ? null
                    : _db.Leaders.Where(l => l.Id == s.SpeakerLeaderId).Select(l => l.FullName).FirstOrDefault(),
                s.SpeakerNameFreeText,
            })
            .ToListAsync(ct).ConfigureAwait(false);

        // Group by date (day only).
        var grouped = allRows
            .GroupBy(r => DateOnly.FromDateTime(r.PublishedAt.Date))
            .OrderByDescending(g => g.Key)
            .ToList();

        var totalDays = grouped.Count;
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling((double)totalDays / pageSize);

        var pagedDays = grouped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g =>
            {
                var dow = (int)g.Key.DayOfWeek;
                var kind = dow == 0 ? "sunday" : dow == 3 ? "wednesday" : "other";
                var sermons = g
                    .OrderBy(r => (int)r.ServiceType)
                    .Select(r => new SermonListItemDto(
                        r.Id, r.Slug, r.Title, r.ThumbnailBlobUrl, r.ThumbnailWebpBlobUrl,
                        r.PublishedAt, r.IsPublished, r.IsMembersOnly, r.IsDeleted,
                        r.SpeakerLeaderName ?? r.SpeakerNameFreeText,
                        r.SeriesTitle, r.SermonSeriesId, r.ServiceType, r.YouTubeVideoId, r.DurationSeconds))
                    .ToList();
                return new ServiceDayDto(g.Key, dow, kind, sermons);
            })
            .ToList();

        // Rail rescoping data — populated only when a filter (search or tag)
        // is active. Browse mode uses the dedicated /years endpoint instead.
        IReadOnlyList<YearStatsDto>? yearStats = null;
        if (hasSearch || hasTag)
        {
            yearStats = allRows
                .GroupBy(r => r.PublishedAt.Year)
                .OrderByDescending(g => g.Key)
                .Select(g => new YearStatsDto(
                    Year: g.Key,
                    Count: g.Count(),
                    MonthCounts: g
                        .GroupBy(r => MonthSlug(r.PublishedAt.Month))
                        .ToDictionary(mg => mg.Key, mg => mg.Count())))
                .ToList();
        }

        return new SermonsByDayResponse(pagedDays, page, pageSize, totalDays, totalPages, yearStats);
    }

    public async Task<YearsResponse> ListYearStatsAsync(bool includeMembersOnly, CancellationToken ct = default)
    {
        IQueryable<Sermon> q = _db.Sermons.Where(s => s.IsPublished);
        if (!includeMembersOnly) q = q.Where(s => !s.IsMembersOnly);

        // Pull (year, month) pairs only — cheap aggregate, no body text.
        var rows = await q
            .Select(s => new { Year = s.PublishedAt.Year, Month = s.PublishedAt.Month })
            .ToListAsync(ct).ConfigureAwait(false);

        if (rows.Count == 0)
            return new YearsResponse(DateTimeOffset.UtcNow.Year, Array.Empty<YearStatsDto>());

        var yearStats = rows
            .GroupBy(r => r.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new YearStatsDto(
                Year: g.Key,
                Count: g.Count(),
                MonthCounts: g
                    .GroupBy(r => MonthSlug(r.Month))
                    .ToDictionary(mg => mg.Key, mg => mg.Count())))
            .ToList();

        // currentYear = the year of the most recently published sermon, not
        // calendar year. A church publishing weekly last in November 2024
        // should land on /sermons/2024 even after the calendar flips.
        var currentYear = yearStats[0].Year;

        return new YearsResponse(currentYear, yearStats);
    }

    private static readonly string[] MonthSlugs = ["jan", "feb", "mar", "apr", "may", "jun",
                                                   "jul", "aug", "sep", "oct", "nov", "dec"];

    private static string MonthSlug(int month) =>
        month >= 1 && month <= 12 ? MonthSlugs[month - 1] : "jan";

    private readonly struct ParsedDateSearch
    {
        public DateOnly? FullDate { get; init; }       // "10/19/2024"
        public DateOnly? MonthYear { get; init; }      // "10/2024", "Oct 2024"
        public (int month, int day)? MonthDay { get; init; } // "5/24", "Oct 19"
        public int? YearOnly { get; init; }            // "2024"
        public int? MonthOnly { get; init; }           // "Oct"
    }

    /// <summary>
    /// Try every reasonable date-ish shape an admin might type into the
    /// sermons search box and return whichever interpretation matches. Only
    /// one bucket is populated per call — the most specific shape wins. The
    /// caller ORs each populated bucket against the PublishedAt column.
    ///
    ///   • FullDate   — "10/19/2024", "2024-10-19", "Oct 19, 2024", "10-19-2024"
    ///   • MonthYear  — "10/2024", "Oct 2024", "2024-10"
    ///   • MonthDay   — "5/24", "10/19", "Oct 19" (any year)
    ///   • YearOnly   — "2024"
    ///   • MonthOnly  — "Oct", "October" (any year)
    /// </summary>
    private static ParsedDateSearch ParseSearchDate(string raw)
    {
        var s = raw.Trim();
        if (s.Length == 0) return default;

        var inv = System.Globalization.CultureInfo.InvariantCulture;

        // Year-only — a bare 4-digit number in the plausible range.
        if (s.Length == 4 && int.TryParse(s, out var year) && year >= 1900 && year <= 2200)
            return new ParsedDateSearch { YearOnly = year };

        // Month name only — "Oct" / "October" (no digits).
        var monthOnly = TryParseMonthName(s);
        if (monthOnly.HasValue && !s.Any(char.IsDigit))
            return new ParsedDateSearch { MonthOnly = monthOnly };

        // Full-date formats — must have a 4-digit (or 2-digit yy) year.
        string[] fullFormats = {
            "M/d/yyyy", "MM/dd/yyyy", "M/d/yy", "MM/dd/yy",
            "M-d-yyyy", "MM-dd-yyyy",
            "yyyy-M-d", "yyyy-MM-dd",
            "MMM d, yyyy", "MMMM d, yyyy",
            "MMM d yyyy", "MMMM d yyyy",
            "d MMM yyyy", "d MMMM yyyy",
        };
        if (System.DateTime.TryParseExact(s, fullFormats, inv,
                System.Globalization.DateTimeStyles.None, out var fullDt))
            return new ParsedDateSearch { FullDate = DateOnly.FromDateTime(fullDt) };

        // Month + year only.
        string[] monthYearFormats = {
            "M/yyyy", "MM/yyyy",
            "yyyy-M", "yyyy-MM",
            "MMM yyyy", "MMMM yyyy",
            "MMM, yyyy", "MMMM, yyyy",
        };
        if (System.DateTime.TryParseExact(s, monthYearFormats, inv,
                System.Globalization.DateTimeStyles.None, out var myDt))
            return new ParsedDateSearch { MonthYear = DateOnly.FromDateTime(myDt) };

        // Month + day only — matches across all years. ParseExact with year
        // omitted defaults the year to the current one, which we discard.
        string[] monthDayFormats = {
            "M/d", "MM/dd",
            "M-d", "MM-dd",
            "MMM d", "MMMM d", "d MMM", "d MMMM",
        };
        if (System.DateTime.TryParseExact(s, monthDayFormats, inv,
                System.Globalization.DateTimeStyles.None, out var mdDt))
            return new ParsedDateSearch { MonthDay = (mdDt.Month, mdDt.Day) };

        return default;
    }

    /// <summary>
    /// Map a raw search string to the set of ServiceType enum values whose
    /// human-readable label contains the input (case-insensitive). Empty
    /// array when nothing matches — callers gate the predicate on Length>0.
    /// Examples:
    ///   "worship"        → [AmWorship, PmWorship]
    ///   "AM Worship"     → [AmWorship]
    ///   "wednesday"      → [WednesdayNight]
    ///   "bible class"    → [AmBibleClass]
    ///   "special"        → [Special]
    /// </summary>
    private static readonly (ServiceType type, string label)[] ServiceTypeLabels = {
        (ServiceType.AmBibleClass, "am bible class"),
        (ServiceType.AmWorship, "am worship"),
        (ServiceType.PmWorship, "pm worship"),
        (ServiceType.WednesdayNight, "wednesday night"),
        (ServiceType.Special, "special"),
    };

    private static ServiceType[] MatchServiceTypes(string raw)
    {
        var needle = raw.Trim().ToLowerInvariant();
        if (needle.Length == 0) return Array.Empty<ServiceType>();
        // Require ≥3 chars to avoid super-permissive matches like "am"
        // catching everything when the admin types something unrelated.
        if (needle.Length < 3) return Array.Empty<ServiceType>();
        return ServiceTypeLabels
            .Where(x => x.label.Contains(needle))
            .Select(x => x.type)
            .ToArray();
    }

    private static readonly string[] MonthAbbrev = ["jan", "feb", "mar", "apr", "may", "jun",
                                                    "jul", "aug", "sep", "oct", "nov", "dec"];
    private static readonly string[] MonthFull = ["january", "february", "march", "april", "may", "june",
                                                  "july", "august", "september", "october", "november", "december"];

    private static int? TryParseMonthName(string s)
    {
        var lower = s.ToLowerInvariant();
        for (var i = 0; i < 12; i++)
            if (lower == MonthAbbrev[i] || lower == MonthFull[i]) return i + 1;
        return null;
    }

    public Task<Sermon?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.Sermons.IgnoreQueryFilters() : _db.Sermons;
        return q.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public Task<Sermon?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Sermons.FirstOrDefaultAsync(s => s.Slug == slug, ct);

    public Task<Sermon?> GetByYouTubeVideoIdAsync(string videoId, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.Sermons.IgnoreQueryFilters() : _db.Sermons;
        return q.FirstOrDefaultAsync(s => s.YouTubeVideoId == videoId, ct);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default)
    {
        var q = _db.Sermons.AsQueryable();
        if (excludingId is not null) q = q.Where(s => s.Id != excludingId);
        return q.AnyAsync(s => s.Slug == slug, ct);
    }

    public async Task<List<Guid>> GetTagIdsAsync(Guid sermonId, CancellationToken ct = default)
        => await _db.SermonTags.Where(t => t.SermonId == sermonId).Select(t => t.TagId).ToListAsync(ct).ConfigureAwait(false);

    public async Task ReplaceTagsAsync(Guid sermonId, IEnumerable<Guid> tagIds, CancellationToken ct = default)
    {
        var nextSet = tagIds.Distinct().ToHashSet();
        var existing = await _db.SermonTags.Where(t => t.SermonId == sermonId).ToListAsync(ct).ConfigureAwait(false);

        var existingSet = existing.Select(t => t.TagId).ToHashSet();
        var toRemove = existing.Where(t => !nextSet.Contains(t.TagId)).ToList();
        var toAdd = nextSet.Where(id => !existingSet.Contains(id)).ToList();

        if (toRemove.Count > 0)
        {
            _db.SermonTags.RemoveRange(toRemove);
            // Decrement usage counts for removed tag links.
            foreach (var t in toRemove)
            {
                await _db.Tags.Where(tag => tag.Id == t.TagId)
                    .ExecuteUpdateAsync(s => s.SetProperty(tag => tag.UsageCount, tag => tag.UsageCount - 1), ct)
                    .ConfigureAwait(false);
            }
        }
        if (toAdd.Count > 0)
        {
            _db.SermonTags.AddRange(toAdd.Select(tagId => new SermonTag { SermonId = sermonId, TagId = tagId }));
            foreach (var tagId in toAdd)
            {
                await _db.Tags.Where(tag => tag.Id == tagId)
                    .ExecuteUpdateAsync(s => s.SetProperty(tag => tag.UsageCount, tag => tag.UsageCount + 1), ct)
                    .ConfigureAwait(false);
            }
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<Guid>> GetAttachmentIdsAsync(Guid sermonId, CancellationToken ct = default)
        => await _db.SermonAttachments.Where(a => a.SermonId == sermonId)
            .OrderBy(a => a.DisplayOrder).Select(a => a.DocumentId).ToListAsync(ct).ConfigureAwait(false);

    public async Task ReplaceAttachmentsAsync(Guid sermonId, IEnumerable<Guid> documentIds, CancellationToken ct = default)
    {
        await _db.SermonAttachments.Where(a => a.SermonId == sermonId)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);

        var ids = documentIds.ToList();
        for (int i = 0; i < ids.Count; i++)
        {
            _db.SermonAttachments.Add(new SermonAttachment
            {
                SermonId = sermonId,
                DocumentId = ids[i],
                DisplayOrder = i,
            });
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> AreAllPublicPdfsAsync(IEnumerable<Guid> documentIds, CancellationToken ct = default)
    {
        var ids = documentIds.Distinct().ToList();
        if (ids.Count == 0) return true;
        var matchCount = await _db.Documents
            .Where(d => ids.Contains(d.Id) && !d.IsMembersOnly && d.IsPublished)
            .CountAsync(ct).ConfigureAwait(false);
        return matchCount == ids.Count;
    }

    public async Task AddAsync(Sermon sermon, CancellationToken ct = default)
    {
        _db.Sermons.Add(sermon);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Sermon sermon, CancellationToken ct = default)
    {
        _db.Sermons.Update(sermon);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sermon = await _db.Sermons.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        if (sermon is null) return false;
        _db.Sermons.Remove(sermon);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<List<SermonsByBookCount>> CountByBookAsync(bool includeMembersOnly, CancellationToken ct = default)
    {
        // Inner-join sermons + scripture refs; count distinct sermons per book.
        var q =
            from r in _db.ScriptureReferences
            where r.ParentEntityType == ScriptureParentEntityType
            join s in _db.Sermons on r.ParentEntityId equals s.Id
            where s.IsPublished && (includeMembersOnly || !s.IsMembersOnly)
            group r by r.Book into g
            select new SermonsByBookCount((int)g.Key, g.Select(x => x.ParentEntityId).Distinct().Count());

        return await q.ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<SermonDetailDto?> ToDetailAsync(Sermon sermon, CancellationToken ct = default)
    {
        var tagIds = await GetTagIdsAsync(sermon.Id, ct).ConfigureAwait(false);
        var tags = await _db.Tags.Where(t => tagIds.Contains(t.Id))
            .Select(t => new TagDto(t.Id, t.Name, t.Slug, t.UsageCount))
            .ToListAsync(ct).ConfigureAwait(false);

        var attachmentDocs = await (from a in _db.SermonAttachments
                                    join d in _db.Documents on a.DocumentId equals d.Id
                                    where a.SermonId == sermon.Id
                                    orderby a.DisplayOrder
                                    select new SermonAttachmentDto(d.Id, d.Title, a.DisplayOrder))
            .ToListAsync(ct).ConfigureAwait(false);

        var refs = await _db.ScriptureReferences
            .Where(r => r.ParentEntityType == ScriptureParentEntityType && r.ParentEntityId == sermon.Id)
            .OrderBy(r => r.DisplayOrder)
            .Select(r => new ScriptureReferenceDto(r.Id, r.Book, r.ChapterStart, r.VerseStart, r.ChapterEnd, r.VerseEnd, r.DisplayOrder))
            .ToListAsync(ct).ConfigureAwait(false);

        return new SermonDetailDto(
            sermon.Id, sermon.Slug, sermon.Title, sermon.DescriptionJson,
            sermon.YouTubeVideoId, sermon.YouTubeChannelId,
            sermon.ThumbnailBlobUrl, sermon.ThumbnailWebpBlobUrl,
            sermon.PublishedAt, sermon.YouTubePublishedAt, sermon.DurationSeconds,
            sermon.Transcript, sermon.TranscriptSource,
            sermon.SpeakerLeaderId, sermon.SpeakerNameFreeText,
            sermon.SermonSeriesId, sermon.ServiceType,
            sermon.IsPublished, sermon.IsMembersOnly, sermon.IsDeleted,
            tags, attachmentDocs, refs,
            sermon.CreatedAt, sermon.ModifiedAt, sermon.ModifiedByUserId, sermon.DeletedAt);
    }

    public async Task<PublicSermonDto?> ToPublicAsync(Sermon sermon, CancellationToken ct = default)
    {
        var detail = await ToDetailAsync(sermon, ct).ConfigureAwait(false);
        if (detail is null) return null;

        string? speakerName = null;
        if (sermon.SpeakerLeaderId is { } leaderId)
        {
            speakerName = await _db.Leaders.Where(l => l.Id == leaderId)
                .Select(l => l.FullName).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }
        speakerName ??= sermon.SpeakerNameFreeText;

        string? seriesTitle = null, seriesSlug = null;
        if (sermon.SermonSeriesId is { } seriesId)
        {
            var s = await _db.SermonSeries.Where(x => x.Id == seriesId)
                .Select(x => new { x.Title, x.Slug })
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
            seriesTitle = s?.Title;
            seriesSlug = s?.Slug;
        }

        return new PublicSermonDto(
            sermon.Id, sermon.Slug, sermon.Title, sermon.DescriptionJson,
            sermon.YouTubeVideoId, sermon.ThumbnailBlobUrl, sermon.ThumbnailWebpBlobUrl,
            sermon.PublishedAt, sermon.DurationSeconds,
            sermon.Transcript,
            sermon.SpeakerLeaderId, speakerName,
            sermon.SermonSeriesId, seriesTitle, seriesSlug,
            sermon.IsMembersOnly, sermon.ServiceType,
            detail.Tags, detail.Attachments, detail.ScriptureReferences);
    }
}
