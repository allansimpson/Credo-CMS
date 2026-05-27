using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Prayer;

public enum PrayerRequestStatus
{
    Active = 0,
    Answered = 1,
    Archived = 2,
}

/// <summary>
/// A member-submitted prayer request. The system always knows the submitter;
/// <see cref="IsAnonymous"/> only governs public display. Profanity filter is
/// run at the service layer before persistence.
/// </summary>
public sealed class PrayerRequest : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>ProseMirror JSON.</summary>
    [Required]
    public string BodyJson { get; set; } = string.Empty;

    public Guid SubmittedByUserId { get; set; }

    public bool IsAnonymous { get; set; }

    public PrayerRequestStatus Status { get; set; } = PrayerRequestStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
