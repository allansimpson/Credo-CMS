using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Classes;

/// <summary>
/// A persistent class slot (e.g. "Adult Class", "Teen Class") filled by rotating
/// <see cref="ClassOffering"/> records (e.g. "Romans" for 8 weeks, then "Marriage
/// &amp; Family" for 6 weeks). Slot is the public-facing audience handle;
/// offerings are the time-bounded curriculum.
/// </summary>
public sealed class ClassSlot : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AudienceAgeGroup { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? GeneralMeetingTime { get; set; }

    /// <summary>MEMBERS-ONLY field — not surfaced to anonymous viewers.</summary>
    [MaxLength(200)]
    public string? DefaultRoom { get; set; }

    /// <summary>ProseMirror JSON, public.</summary>
    public string? DescriptionJson { get; set; }

    [MaxLength(2000)]
    public string? ImageBlobUrl { get; set; }

    [MaxLength(2000)]
    public string? ImageWebpBlobUrl { get; set; }

    [MaxLength(500)]
    public string? ImageAltText { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
