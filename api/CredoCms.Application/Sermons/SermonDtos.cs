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
    Guid? SermonSeriesId);

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
    bool IsPublished,
    bool IsMembersOnly,
    IList<SermonTagInput> Tags,
    IList<Guid> AttachmentDocumentIds,
    IList<ScriptureReferenceInput> ScriptureReferences);

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
    bool IsPublished,
    bool IsMembersOnly,
    IList<SermonTagInput> Tags,
    IList<Guid> AttachmentDocumentIds,
    IList<ScriptureReferenceInput> ScriptureReferences);

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

public sealed record SermonsByBookCount(int BookValue, int Count);
