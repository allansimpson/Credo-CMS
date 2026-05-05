using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Announcements;

public enum AnnouncementSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2,
}

/// <summary>
/// Singleton row at <see cref="SystemConstants.AnnouncementBannerId"/>.
/// Versioned so the message history is auditable.
/// </summary>
public sealed class AnnouncementBanner : IVersionedEntity
{
    public Guid Id { get; set; }

    public bool IsActive { get; set; }

    public AnnouncementSeverity Severity { get; set; } = AnnouncementSeverity.Info;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? LinkUrl { get; set; }

    [MaxLength(100)]
    public string? LinkLabel { get; set; }

    /// <summary>Optional schedule. If both null, the banner is shown
    /// whenever <see cref="IsActive"/> is true.</summary>
    public DateTimeOffset? StartsAt { get; set; }

    public DateTimeOffset? EndsAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }
}
