using CredoCms.Application.Scripture;
using CredoCms.Application.Tags;
using CredoCms.Domain.Sermons;

namespace CredoCms.Application.Sermons;

public sealed record SermonListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string? ThumbnailBlobUrl,
    string? ThumbnailWebpBlobUrl,
    DateTimeOffset PublishedAt,
    bool IsPublished,
    bool IsMembersOnly,
    bool IsDeleted,
    string? SpeakerName,
    string? SermonSeriesTitle,
    Guid? SermonSeriesId,
    ServiceType ServiceType,
    /// <summary>Carried on every row so the admin table's inline "Watch"
    /// modal can mount the YouTube embed without a per-click round trip.</summary>
    string YouTubeVideoId,
    /// <summary>Video length in seconds (captured during YouTube sync).
    /// Null when the sync didn't return a duration — show "—" in the UI.</summary>
    int? DurationSeconds);

public sealed record SermonAttachmentDto(Guid DocumentId, string Title, int DisplayOrder);

public sealed record SermonDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string? DescriptionJson,
    string YouTubeVideoId,
    string? YouTubeChannelId,
    string? ThumbnailBlobUrl,
    string? ThumbnailWebpBlobUrl,
    DateTimeOffset PublishedAt,
    DateTimeOffset YouTubePublishedAt,
    int? DurationSeconds,
    string? Transcript,
    TranscriptSource TranscriptSource,
    Guid? SpeakerLeaderId,
    string? SpeakerNameFreeText,
    Guid? SermonSeriesId,
    ServiceType ServiceType,
    bool IsPublished,
    bool IsMembersOnly,
    bool IsDeleted,
    IReadOnlyList<TagDto> Tags,
    IReadOnlyList<SermonAttachmentDto> Attachments,
    IReadOnlyList<ScriptureReferenceDto> ScriptureReferences,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    Guid? ModifiedByUserId,
    DateTimeOffset? DeletedAt);

public sealed record PublicSermonDto(
    Guid Id,
    string Slug,
    string Title,
    string? DescriptionJson,
    string YouTubeVideoId,
    string? ThumbnailBlobUrl,
    string? ThumbnailWebpBlobUrl,
    DateTimeOffset PublishedAt,
    int? DurationSeconds,
    string? Transcript,
    Guid? SpeakerLeaderId,
    string? SpeakerName,
    Guid? SermonSeriesId,
    string? SermonSeriesTitle,
    string? SermonSeriesSlug,
    bool IsMembersOnly,
    ServiceType ServiceType,
    IReadOnlyList<TagDto> Tags,
    IReadOnlyList<SermonAttachmentDto> Attachments,
    IReadOnlyList<ScriptureReferenceDto> ScriptureReferences);

public sealed record SermonTagInput(Guid? Id, string Name);

public sealed record CreateSermonRequest(
    string Slug,
    string Title,
    string? DescriptionJson,
    string YouTubeVideoId,
    string? YouTubeChannelId,
    string? ThumbnailBlobUrl,
    string? ThumbnailWebpBlobUrl,
    DateTimeOffset PublishedAt,
    DateTimeOffset YouTubePublishedAt,
    int? DurationSeconds,
    string? Transcript,
    TranscriptSource TranscriptSource,
    Guid? SpeakerLeaderId,
    string? SpeakerNameFreeText,
    Guid? SermonSeriesId,
    ServiceType ServiceType = ServiceType.AmWorship,
    bool IsPublished = false,
    bool IsMembersOnly = false,
    IList<SermonTagInput>? Tags = null,
    IList<Guid>? AttachmentDocumentIds = null,
    IList<ScriptureReferenceInput>? ScriptureReferences = null);

public sealed record UpdateSermonRequest(
    string Slug,
    string Title,
    string? DescriptionJson,
    string? ThumbnailBlobUrl,
    string? ThumbnailWebpBlobUrl,
    DateTimeOffset PublishedAt,
    string? Transcript,
    TranscriptSource TranscriptSource,
    Guid? SpeakerLeaderId,
    string? SpeakerNameFreeText,
    Guid? SermonSeriesId,
    ServiceType ServiceType = ServiceType.AmWorship,
    bool IsPublished = false,
    bool IsMembersOnly = false,
    IList<SermonTagInput>? Tags = null,
    IList<Guid>? AttachmentDocumentIds = null,
    IList<ScriptureReferenceInput>? ScriptureReferences = null);

// ── By-day grouping DTOs ─────────────────────────────────────────────────

public sealed record ServiceDayDto(
    DateOnly Date,
    int DayOfWeek,
    string Kind,
    IReadOnlyList<SermonListItemDto> Sermons);

public sealed record SermonsByDayResponse(
    IReadOnlyList<ServiceDayDto> Days,
    int Page,
    int PageSize,
    int TotalDays,
    int TotalPages,
    /// <summary>Populated only when a filter narrows the archive (search OR
    /// tag). Drives the side-rail's rescoped match counts. Sorted descending
    /// by year. Null in normal browse mode — the unfiltered rail uses the
    /// dedicated /years endpoint instead.</summary>
    IReadOnlyList<YearStatsDto>? YearStats = null);

public sealed record YearStatsDto(
    int Year,
    int Count,
    /// <summary>Lowercase three-letter slug → count. Months with zero entries
    /// are omitted. Slugs match <c>MONTH_SLUGS</c> in the SPA.</summary>
    IReadOnlyDictionary<string, int> MonthCounts);

public sealed record YearsResponse(
    int CurrentYear,
    IReadOnlyList<YearStatsDto> Years);

public sealed record SermonListQuery(
    string? Search = null,
    Guid? SermonSeriesId = null,
    string? TagSlug = null,
    Guid? SpeakerLeaderId = null,
    int? BookFilter = null,
    bool? PublishedOnly = null,
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 25);

public sealed record SermonsByDayQuery(
    string? Search = null,
    string? TagSlug = null,
    ServiceType? ServiceType = null,
    /// <summary>Calendar year filter (e.g. 2024). Pages of a single year are
    /// returned in descending date order. When null, returns the latest
    /// pageSize days regardless of year.</summary>
    int? Year = null,
    int Page = 1,
    int PageSize = 20);

public sealed record SermonsByBookCount(int BookValue, int Count);
