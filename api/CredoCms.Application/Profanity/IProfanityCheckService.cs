namespace CredoCms.Application.Profanity;

/// <summary>
/// Service-layer profanity check used by user-submitted content (prayer
/// requests; connect-card free-text fields when Q11 lands).
/// Implementations merge a built-in wordlist with SiteSettings
/// <c>ProfanityWordlist</c>, then strip <c>ProfanityAllowlist</c> matches
/// (false-positive recovery — useful for biblical names like "Damascus"
/// that share substrings with rude words).
/// </summary>
public interface IProfanityCheckService
{
    /// <summary>
    /// Returns true when <paramref name="text"/> contains any wordlist match
    /// not suppressed by the allowlist. Case-insensitive; whitespace is
    /// preserved but punctuation around matches is tolerated.
    /// </summary>
    Task<bool> ContainsProfanityAsync(string? text, CancellationToken ct = default);
}
