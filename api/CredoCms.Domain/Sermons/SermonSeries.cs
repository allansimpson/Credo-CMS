using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Sermons;

/// <summary>
/// A grouping of sermons (e.g. "Foundations of Faith"). Versioned because
/// the public list is a curated record. Optional structured Scripture
/// reference is stored via the polymorphic ScriptureReferences table
/// (ParentEntityType = "SermonSeries").
/// </summary>
public sealed class SermonSeries : IVersionedEntity
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

    [MaxLength(2000)]
    public string? BannerImageUrl { get; set; }

    [MaxLength(2000)]
    public string? BannerImageWebpUrl { get; set; }

    [MaxLength(500)]
    public string? BannerImageAlt { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Null = ongoing / open-ended.</summary>
    public DateOnly? EndDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
