namespace CredoCms.Application.Calendar;

public sealed record CalendarFeedTokenInfo(
    Guid UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt);

public interface ICalendarFeedTokenService
{
    /// <summary>Issues a new feed token, revoking any prior tokens for this user. Returns the URL-safe token (only available at issue time).</summary>
    Task<string> IssueAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Resolves a presented token to its user, recording last-used. Returns null if revoked or unknown.</summary>
    Task<Guid?> ResolveAsync(string token, CancellationToken ct = default);

    /// <summary>Returns the active token info for the user, or null if none.</summary>
    Task<CalendarFeedTokenInfo?> GetCurrentAsync(Guid userId, CancellationToken ct = default);

    Task RevokeAllAsync(Guid userId, CancellationToken ct = default);
}
