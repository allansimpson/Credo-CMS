using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Blog;

/// <summary>
/// A long-form blog post. Author is always linked to an <c>ApplicationUser</c>;
/// optional companion link to a <c>Sermon</c>. Categories are admin-configured
/// strings (Site Settings → <c>BlogCategories</c>); tags are shared with sermons
/// via the unified <c>Tags</c> table.
/// </summary>
public sealed class BlogPost : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>ProseMirror JSON, required.</summary>
    [Required]
    public string BodyJson { get; set; } = string.Empty;

    /// <summary>Optional excerpt; if null, generated from body plain-text on save.</summary>
    [MaxLength(500)]
    public string? Excerpt { get; set; }

    [MaxLength(2000)]
    public string? HeroImageBlobUrl { get; set; }

    [MaxLength(2000)]
    public string? HeroImageWebpBlobUrl { get; set; }

    [MaxLength(500)]
    public string? HeroImageAltText { get; set; }

    public Guid AuthorUserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public Guid? RelatedSermonId { get; set; }

    public bool IsPublished { get; set; }

    public bool IsMembersOnly { get; set; }

    public bool IsPinned { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Captured in Phase 4; the scheduled-publish job activates in Phase 5.</summary>
    public DateTimeOffset? ScheduledPublishAt { get; set; }

    /// <summary>Reading time in minutes, recomputed from body word count on save.
    /// Formula: max(1, ceil(words / 250)).</summary>
    public int ReadingTimeMinutes { get; set; } = 1;

    [MaxLength(300)]
    public string? MetaDescription { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedByUserId { get; set; }
}

/// <summary>Many-to-many: blog post ↔ shared <c>Tags</c> table.
/// Not versioned per project convention.</summary>
public sealed class BlogPostTag
{
    public Guid BlogPostId { get; set; }
    public Guid TagId { get; set; }
}
