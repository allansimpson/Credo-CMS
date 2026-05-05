namespace CredoCms.Domain.Events;

/// <summary>
/// Per-member iCal feed token. The public token (sent in the URL) is
/// random; the database stores only its SHA-256 hash so a leaked token
/// row cannot itself be used to subscribe.
/// </summary>
public sealed class CalendarFeedToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The Identity user this token belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>SHA-256 of the URL-safe random token, hex-encoded.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}
