using System.ComponentModel.DataAnnotations;

namespace CredoCms.Domain.Tags;

/// <summary>
/// Unified tag table shared across content types (sermons, blog posts, etc.).
/// Tags normalize on insert: case-insensitive match against existing rows;
/// new tags get title-cased canonical names.
/// </summary>
public sealed class Tag
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Denormalized count of content items currently linked to this
    /// tag. Maintained by content services as content is tagged/untagged.
    /// Used for cheap "popular tags" and admin-UI-side tidy-up of orphans.</summary>
    public int UsageCount { get; set; }
}
