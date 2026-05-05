using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.News;

/// <summary>
/// A short article presented as part of the church's news feed. Defaults to
/// members-only — anonymous public visitors see only the items where the
/// editor has explicitly cleared <see cref="IsMembersOnly"/>.
///
/// <see cref="ExpiresAt"/> hides the item from listings after the given UTC
/// instant; admin views ignore the cut-off so editors can still find/edit
/// expired items.
/// </summary>
public sealed class NewsItem : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string BodyJson { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Excerpt { get; set; }

    [MaxLength(2000)]
    public string? HeroImageUrl { get; set; }

    [MaxLength(2000)]
    public string? HeroImageWebpUrl { get; set; }

    [MaxLength(300)]
    public string? HeroImageAlt { get; set; }

    [MaxLength(300)]
    public string? MetaDescription { get; set; }

    public bool IsPublished { get; set; }

    public bool IsMembersOnly { get; set; } = true;

    public bool IsDeleted { get; set; }

    /// <summary>Optional cut-off after which the item drops off public listings.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Optional event date if the item is announcing something
    /// scheduled. Surfaced on the detail page.</summary>
    public DateTimeOffset? CalendarDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
