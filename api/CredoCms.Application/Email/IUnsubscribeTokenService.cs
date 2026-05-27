using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

/// <summary>
/// HMAC-SHA256 signed unsubscribe tokens. Token format:
/// <c>base64url(userId|category|timestampUnixSeconds|hmac)</c>. Tokens are
/// not single-use (RFC 8058 permits multiple clicks); a 30-day expiry
/// limits replay risk. The HMAC key (<c>SiteSettings.UnsubscribeSigningKey</c>)
/// is auto-generated on first read if blank.
///
/// <para>The token's payload identifies the recipient by design — the
/// unsubscribe page must know which user's preferences to update. HMAC
/// integrity prevents tampering.</para>
/// </summary>
public interface IUnsubscribeTokenService
{
    Task<string> GenerateAsync(Guid userId, EmailCategory category, CancellationToken ct = default);
    Task<UnsubscribeTokenResult> ValidateAsync(string token, CancellationToken ct = default);
}

public sealed record UnsubscribeTokenResult(bool IsValid, Guid UserId, EmailCategory Category, string? FailureReason = null);
