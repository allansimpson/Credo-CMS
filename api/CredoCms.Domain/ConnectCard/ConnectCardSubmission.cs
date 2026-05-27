using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.ConnectCard;

public enum ConnectCardStatus
{
    /// <summary>Newly received; not yet triaged.</summary>
    New = 0,
    /// <summary>Triaged; needs follow-up.</summary>
    FollowUpNeeded = 1,
    /// <summary>Follow-up complete.</summary>
    FollowedUp = 2,
    /// <summary>Closed (no further action needed).</summary>
    Closed = 3,
    /// <summary>Spam / illegitimate; soft archive.</summary>
    NotLegit = 4,
}

/// <summary>
/// A public Connect Card submission. Versioned (status / notes timeline lives in
/// the temporal history). Anonymous submission is allowed; rate-limited by hashed
/// IP. Defended by Cloudflare Turnstile, honeypot, and 5-second time-to-submit.
/// </summary>
public sealed class ConnectCardSubmission : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public bool IsFirstTimeVisitor { get; set; }

    public DateOnly? ServiceDate { get; set; }

    [Required]
    [MaxLength(500)]
    public string HowDidYouHear { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? Comments { get; set; }

    /// <summary>JSON array of strings — selected interests from the configurable list.</summary>
    public string? InterestCheckboxesJson { get; set; }

    public ConnectCardStatus Status { get; set; } = ConnectCardStatus.New;

    public string? AdminNotes { get; set; }

    public Guid? StatusChangedByUserId { get; set; }

    public DateTimeOffset? StatusChangedAt { get; set; }

    public DateTimeOffset? AcknowledgmentEmailSentAt { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }

    /// <summary>SHA-256 hex of the submitter's IP address. Used for rate-limit
    /// observability and abuse pattern detection. Never reverses to a real IP.</summary>
    [MaxLength(64)]
    public string? IpAddressHash { get; set; }

    // IVersionedEntity — initial submission is anonymous so these are null on
    // create; the temporal-table interceptor populates them when an admin
    // mutates the row (status change, notes update).
    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }
}
