using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Events;

public enum EventVisibility
{
    Public = 0,
    MembersOnly = 1,
}

public enum EventRegistrationMode
{
    None = 0,
    RsvpOptional = 1,
    RegistrationRequired = 2,
}

/// <summary>
/// A calendar event — single occurrence or recurring (RRULE in iCal
/// format). Versioned. Visibility is nullable so a draft can exist
/// without one; FluentValidation enforces "must be set before publish."
/// </summary>
public sealed class Event : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional category for filtering (e.g., "Worship", "Formation"). Must match one configured in SiteSettings.EventCategoriesJson.</summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    public string? DescriptionJson { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset? EndsAt { get; set; }

    public bool AllDay { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    [MaxLength(2000)]
    public string? HeroImageUrl { get; set; }

    [MaxLength(2000)]
    public string? HeroImageWebpUrl { get; set; }

    [MaxLength(500)]
    public string? HeroImageAlt { get; set; }

    /// <summary>Nullable until the editor sets it; required-on-publish.</summary>
    public EventVisibility? Visibility { get; set; }

    // Recurrence (iCal RRULE)
    [MaxLength(500)]
    public string? RecurrenceRule { get; set; }

    public DateTimeOffset? RecurrenceEndDate { get; set; }

    public int? RecurrenceCount { get; set; }

    // Registration
    public EventRegistrationMode RegistrationMode { get; set; } = EventRegistrationMode.None;

    public int? Capacity { get; set; }

    public bool WaitlistEnabled { get; set; }

    public DateTimeOffset? RegistrationOpensAt { get; set; }

    public DateTimeOffset? RegistrationClosesAt { get; set; }

    public string? RegistrationConfirmationMessageJson { get; set; }

    [MaxLength(2000)]
    public string? ExternalRegistrationUrl { get; set; }

    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>"Skip this occurrence" — emitted as EXDATE in iCal.
/// Domain terminology, not a System.Exception.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711",
    Justification = "iCal/RFC 5545 calls these recurrence exceptions; the term is the established calendar-domain name.")]
public sealed class EventRecurrenceException
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public DateOnly OccurrenceDate { get; set; }
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>"Edit this occurrence" — emitted as a separate VEVENT
/// with RECURRENCE-ID in iCal.</summary>
public sealed class EventOccurrenceOverride
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public DateOnly OriginalOccurrenceDate { get; set; }
    public DateTimeOffset? OverrideStartsAt { get; set; }
    public DateTimeOffset? OverrideEndsAt { get; set; }
    [MaxLength(500)]
    public string? OverrideLocation { get; set; }
    public string? OverrideDescriptionJson { get; set; }
    public bool IsCanceled { get; set; }
}
