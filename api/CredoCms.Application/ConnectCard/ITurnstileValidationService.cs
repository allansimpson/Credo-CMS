namespace CredoCms.Application.ConnectCard;

/// <summary>
/// Cloudflare Turnstile <c>siteverify</c> wrapper. Implementation is HTTP-bound
/// and lives in Infrastructure. Returns <c>true</c> when the token is valid OR
/// when Turnstile isn't configured (dev mode), so local dev doesn't require a
/// Turnstile site key.
/// </summary>
public interface ITurnstileValidationService
{
    /// <summary>
    /// Validates <paramref name="token"/> against Cloudflare's siteverify
    /// endpoint. <paramref name="remoteIp"/> is optional but recommended —
    /// Cloudflare uses it for additional heuristics.
    /// </summary>
    Task<bool> ValidateAsync(string? token, string? remoteIp, CancellationToken ct = default);
}
