using CredoCms.Application.Profanity;
using CredoCms.Application.SiteSettingsManagement;
using PF = ProfanityFilter.ProfanityFilter;

namespace CredoCms.Infrastructure.Profanity;

/// <summary>
/// Profanity check backed by the <c>Profanity.Detector</c> NuGet package
/// (Stephen Haunts). The package ships a canonical English wordlist; we
/// layer <c>SiteSettings.ProfanityWordlist</c> additions on top and let
/// <c>SiteSettings.ProfanityAllowlist</c> suppress false positives.
///
/// We deliberately use <c>DetectAllProfanities(text, removePartialMatches:
/// true)</c> rather than the package's <c>ContainsProfanity</c>: in v0.1.8
/// the latter performs naive lowercase substring matching, which trips on
/// the Scunthorpe problem (e.g. "hello" matches "hell", "classic" matches
/// "ass"). DetectAllProfanities with partial-match elimination does
/// word-boundary scanning and is the API the package's own tests use.
/// </summary>
public sealed class ProfanityCheckService : IProfanityCheckService
{
    private readonly ISiteSettingsRepository _settings;

    public ProfanityCheckService(ISiteSettingsRepository settings) => _settings = settings;

    public async Task<bool> ContainsProfanityAsync(string? text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var siteSettings = await _settings.GetAsync(ct).ConfigureAwait(false);
        var custom = SplitLines(siteSettings.ProfanityWordlist);
        var allow = SplitLines(siteSettings.ProfanityAllowlist);

        var filter = new PF();
        foreach (var w in custom) filter.AddProfanity(w);
        foreach (var w in allow)
        {
            // RemoveProfanity drops baseline words from the scan list;
            // AllowList.Add additionally suppresses compound false positives.
            filter.RemoveProfanity(w);
            filter.AllowList.Add(w);
        }

        return filter.DetectAllProfanities(text, removePartialMatches: true).Count > 0;
    }

    private static string[] SplitLines(string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? Array.Empty<string>()
            : input.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
