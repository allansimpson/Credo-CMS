using System.ComponentModel.DataAnnotations;

namespace CredoCms.Domain.Leaders;

/// <summary>
/// A staff member or leader. Per VERSIONING.md §2 explicitly NOT versioned —
/// the Leaders list is curated rather than historical. Hard-delete only;
/// no soft-delete. Editors can create/edit; only Administrators can delete.
/// </summary>
public sealed class Leader
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Title { get; set; }

    /// <summary>Must match one of the categories in
    /// <c>SiteSettings.LeaderCategoriesJson</c>; the API enforces this.</summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>ProseMirror JSON for the bio.</summary>
    public string? BioJson { get; set; }

    [MaxLength(254)]
    public string? Email { get; set; }

    [MaxLength(2000)]
    public string? PhotoUrl { get; set; }

    [MaxLength(2000)]
    public string? PhotoWebpUrl { get; set; }

    [MaxLength(300)]
    public string? PhotoAlt { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Optional link to an <c>ApplicationUser</c>. When set, the leader's
    /// <see cref="Title"/> is surfaced as the byline tag on any Blog post or
    /// News item authored by that user. One user can be at most one leader —
    /// enforced by a filtered unique index in
    /// <c>LeaderConfiguration</c>.</summary>
    public Guid? UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }
}
