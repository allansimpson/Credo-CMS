using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Services;

/// <summary>
/// A recurring weekly service slot (e.g. Sunday Worship at 9am, Wednesday
/// Bible Study at 7pm). Versioned because the published list reflects
/// pastoral decisions that historically need an audit trail.
/// </summary>
public sealed class ServiceTime : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Numeric ordering within a day, ascending. Drag-and-drop reorder
    /// is deferred per BUILD_PLAN P5.4.</summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
