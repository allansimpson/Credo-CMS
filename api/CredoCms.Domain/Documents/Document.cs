using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Documents;

/// <summary>
/// A PDF document published for download. The metadata row is versioned;
/// the binary blob itself is replaced rather than versioned (per
/// VERSIONING.md §10).
/// </summary>
public sealed class Document : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>The blob URL of the current PDF.</summary>
    [Required]
    [MaxLength(2000)]
    public string BlobUrl { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? OriginalFilename { get; set; }

    public long SizeBytes { get; set; }

    public bool IsPublished { get; set; }

    public bool IsMembersOnly { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
