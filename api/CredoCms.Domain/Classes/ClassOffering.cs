using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Classes;

/// <summary>
/// A time-bounded curriculum filling a <see cref="ClassSlot"/>. Subject (e.g.
/// "Romans"), date range, optional teacher (Leader link or free text), and
/// member-only operational details (detailed schedule, room, materials).
/// </summary>
public sealed class ClassOffering : IVersionedEntity
{
    public Guid Id { get; set; }

    public Guid ClassSlotId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>ProseMirror JSON, public.</summary>
    public string? DescriptionJson { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>MEMBERS-ONLY field. FK to a Leader entity.</summary>
    public Guid? TeacherLeaderId { get; set; }

    /// <summary>MEMBERS-ONLY fallback when no Leader is linked.</summary>
    [MaxLength(200)]
    public string? TeacherFreeText { get; set; }

    /// <summary>MEMBERS-ONLY week-by-week ProseMirror JSON.</summary>
    public string? DetailedScheduleJson { get; set; }

    /// <summary>MEMBERS-ONLY plain-text materials list.</summary>
    [MaxLength(1000)]
    public string? MaterialsNeeded { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
