using System.ComponentModel.DataAnnotations;

namespace CredoCms.Domain.Events;

public enum EventRegistrationFieldType
{
    ShortText = 0,
    LongText = 1,
    Number = 2,
    Date = 3,
    SingleSelect = 4,
    MultiSelect = 5,
    YesNo = 6,
    Email = 7,
    Phone = 8,
}

public sealed class EventRegistrationField
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public int DisplayOrder { get; set; }

    [Required]
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    public EventRegistrationFieldType FieldType { get; set; }

    public bool Required { get; set; }

    [MaxLength(500)]
    public string? HelpText { get; set; }

    /// <summary>JSON array of strings — used for SingleSelect / MultiSelect.</summary>
    public string? OptionsJson { get; set; }

    public int? TextMaxLength { get; set; }
    public decimal? NumberMin { get; set; }
    public decimal? NumberMax { get; set; }
}

public enum EventRegistrationStatus
{
    Confirmed = 0,
    Waitlisted = 1,
    Canceled = 2,
}

public sealed class EventRegistration
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public DateOnly? OccurrenceDate { get; set; }
    public Guid? UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string SubmitterName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string SubmitterEmail { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SubmitterPhone { get; set; }

    /// <summary>JSON keyed by EventRegistrationField.Id; values are
    /// strings, numbers, ISO dates, booleans, or string arrays per field type.</summary>
    public string? FieldValuesJson { get; set; }

    public EventRegistrationStatus Status { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? CanceledAt { get; set; }

    [MaxLength(500)]
    public string? CancelReason { get; set; }

    public DateTimeOffset? ConfirmationEmailSentAt { get; set; }

    /// <summary>Set when the event-registration reminder service has emailed
    /// this registrant for the upcoming event. Used to enforce
    /// once-per-registration reminder semantics.</summary>
    public DateTimeOffset? ReminderEmailSentAt { get; set; }
}
