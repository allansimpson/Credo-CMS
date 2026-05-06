using System.Text.RegularExpressions;
using CredoCms.Application.Profanity;
using CredoCms.Application.SiteSettingsManagement;

namespace CredoCms.Infrastructure.Profanity;

/// <summary>
/// In-process profanity check. The Phase 4 build plan calls for the
/// <c>ProfanityFilter</c> NuGet package; in environments where NuGet is
/// reachable that package can be wired here without changing the public
/// surface (<see cref="IProfanityCheckService"/> stays the same).
///
/// Each call reads SiteSettings, merges the built-in list with
/// <c>ProfanityWordlist</c> (newline-delimited), then suppresses any
/// <c>ProfanityAllowlist</c> matches. Matching is case-insensitive on word
/// boundaries — punctuation immediately around the word is tolerated, but
/// substrings inside larger words are NOT flagged (e.g. "scunthorpe"
/// problem avoided).
/// </summary>
public sealed class ProfanityCheckService : IProfanityCheckService
{
    private readonly ISiteSettingsRepository _settings;

    /// <summary>Built-in baseline. Intentionally short — the canonical list
    /// would come from the NuGet package; this baseline guards the most
    /// common abuse without needing a configured custom list.</summary>
    private static readonly IReadOnlyCollection<string> BuiltIn = new[]
    {
        // A small, deliberately-conservative starter set. Customize via
        // SiteSettings.ProfanityWordlist in production.
        "fuck", "shit", "bitch", "asshole", "bastard", "cunt", "piss",
        "dick", "cock", "pussy", "slut", "whore",
    };

    public ProfanityCheckService(ISiteSettingsRepository settings) => _settings = settings;

    public async Task<bool> ContainsProfanityAsync(string? text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var siteSettings = await _settings.GetAsync(ct).ConfigureAwait(false);
        var custom = SplitLines(siteSettings.ProfanityWordlist);
        var allow = SplitLines(siteSettings.ProfanityAllowlist);

        // Merged set is case-insensitive. Allowlist subtracts from the merged
        // set so a customer can suppress a built-in false positive without
        // editing the package.
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in BuiltIn) words.Add(w);
        foreach (var w in custom) words.Add(w);
        foreach (var w in allow) words.Remove(w);
        if (words.Count == 0) return false;

        // Word-boundary regex per term. Compiled per call is fine for the
        // expected input volume (member-typed prayer requests, hand-typed
        // connect-card messages); if hot, pre-compile and cache by
        // (siteSettings.RowVersion, set hash).
        var pattern = "\\b(" + string.Join("|", words.Select(Regex.Escape)) + ")\\b";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string[] SplitLines(string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? Array.Empty<string>()
            : input.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
