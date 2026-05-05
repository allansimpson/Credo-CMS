using System.ComponentModel.DataAnnotations;

namespace CredoCms.Domain.Search;

/// <summary>
/// One row per indexable content document. Multiple entity types share the
/// same table; <see cref="EntityType"/> + <see cref="EntityId"/> form the
/// natural key.
/// </summary>
public sealed class SearchIndexEntry
{
    public Guid Id { get; set; }

    /// <summary>One of: <c>Page</c>, <c>NewsItem</c>, <c>Leader</c>, <c>Document</c>.</summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Plain-text body for FTS / LIKE matching. Up to ~10 KB.</summary>
    [Required]
    public string BodyText { get; set; } = string.Empty;

    /// <summary>Public route for this entity, e.g. <c>/about</c> or
    /// <c>/news/summer-camp-2026</c>. Used to render result links.</summary>
    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public bool IsMembersOnly { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset IndexedAt { get; set; }
}
