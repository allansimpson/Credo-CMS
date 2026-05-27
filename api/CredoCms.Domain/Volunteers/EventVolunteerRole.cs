using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Volunteers;

/// <summary>
/// A volunteer role attached to an <c>Event</c>. Roles defined here apply
/// across every occurrence of a recurring event (per-occurrence overrides
/// are not in v1). For a single-occurrence event, the role applies to the
/// event's date.
///
/// <para>Versioned: temporal table on metadata changes.</para>
/// </summary>
public sealed class EventVolunteerRole : IVersionedEntity
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    [Required]
    [MaxLength(200)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>How many people are needed for this role at each occurrence.</summary>
    public int SlotsNeeded { get; set; } = 1;

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
