using System.ComponentModel.DataAnnotations;

namespace CredoCms.Domain.Auditing;

/// <summary>
/// Append-only audit-log entry. Captures who-did-what-when across the system.
/// Entries are immutable once written and are not soft-deletable.
/// </summary>
public class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The acting user's id. Null for system actions that occurred before the
    /// system user existed (vanishingly rare) or when a user has been hard-deleted
    /// (cascade is configured as SetNull).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// The acting user's display name captured at write time. Survives hard deletion
    /// of the user record so that the audit trail remains identifiable.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string UserDisplayNameSnapshot { get; set; } = string.Empty;

    /// <summary>e.g. "User.Created", "SiteSettings.Updated".</summary>
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>e.g. "ApplicationUser", "SiteSettings".</summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>String to allow various ID types (Guid, int, slug, etc.).</summary>
    [MaxLength(100)]
    public string? EntityId { get; set; }

    /// <summary>JSON-serialised structured data about the change.</summary>
    public string? DetailsJson { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }
}
