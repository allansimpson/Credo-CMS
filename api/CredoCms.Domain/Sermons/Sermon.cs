using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Sermons;

public enum TranscriptSource
{
    None = 0,
    YouTubeAuto = 1,
    Uploaded = 2,
}

/// <summary>
/// A sermon — usually imported from YouTube either by manual paste of a
/// URL/video-ID or by the periodic <c>YouTubeSyncService</c>. Versioned;
/// transcript is included so we can audit transcript changes too.
/// </summary>
public sealed class Sermon : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>ProseMirror JSON, nullable.</summary>
    public string? DescriptionJson { get; set; }

    [Required]
    [MaxLength(20)]
    public string YouTubeVideoId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? YouTubeChannelId { get; set; }

    [MaxLength(2000)]
    public string? ThumbnailBlobUrl { get; set; }

    [MaxLength(2000)]
    public string? ThumbnailWebpBlobUrl { get; set; }

    /// <summary>Site-side publish date (defaults to YouTube publish date but Editor can override).</summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>Original YouTube publish date — immutable once captured.</summary>
    public DateTimeOffset YouTubePublishedAt { get; set; }

    public int? DurationSeconds { get; set; }

    /// <summary>Plain text transcript. Versioned with the rest of the row.</summary>
    public string? Transcript { get; set; }

    public TranscriptSource TranscriptSource { get; set; } = TranscriptSource.None;

    public Guid? SpeakerLeaderId { get; set; }

    [MaxLength(200)]
    public string? SpeakerNameFreeText { get; set; }

    public Guid? SermonSeriesId { get; set; }

    public bool IsPublished { get; set; }

    public bool IsMembersOnly { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Many-to-many: sermon ↔ tags (tag autocomplete from
/// <c>Tags</c>; not versioned per project convention).</summary>
public sealed class SermonTag
{
    public Guid SermonId { get; set; }
    public Guid TagId { get; set; }
}

/// <summary>Many-to-many: sermon ↔ documents (PDFs only, must have
/// <c>IsMembersOnly=false</c>; service-level validation).</summary>
public sealed class SermonAttachment
{
    public Guid SermonId { get; set; }
    public Guid DocumentId { get; set; }
    public int DisplayOrder { get; set; }
}
