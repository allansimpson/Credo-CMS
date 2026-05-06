using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Prayer;

/// <summary>
/// A pastoral update on a <see cref="PrayerRequest"/>. Only Editors and
/// Administrators may post updates (members do not comment).
/// </summary>
public sealed class PrayerRequestUpdate : IVersionedEntity
{
    public Guid Id { get; set; }

    public Guid PrayerRequestId { get; set; }

    /// <summary>ProseMirror JSON.</summary>
    [Required]
    public string BodyJson { get; set; } = string.Empty;

    public Guid PostedByUserId { get; set; }

    /// <summary>Denormalized poster display name captured at write time
    /// (e.g. "Pastor Smith") so renaming a user doesn't rewrite history.</summary>
    [Required]
    [MaxLength(200)]
    public string PostedByLabel { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
