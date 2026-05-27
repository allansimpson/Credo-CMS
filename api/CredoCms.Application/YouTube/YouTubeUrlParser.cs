using System.Text.RegularExpressions;

namespace CredoCms.Application.YouTube;

/// <summary>
/// Recognizes YouTube video IDs in any of these forms:
///   https://www.youtube.com/watch?v=VIDEOID
///   https://youtu.be/VIDEOID
///   https://www.youtube.com/shorts/VIDEOID
///   https://www.youtube.com/embed/VIDEOID
///   VIDEOID  (bare ID — 11 chars, alphanumeric/_/- )
/// Returns null if no match.
/// </summary>
public static partial class YouTubeUrlParser
{
    [GeneratedRegex(@"^[A-Za-z0-9_-]{6,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex BareIdRegex();

    [GeneratedRegex(@"(?:youtube\.com/(?:watch\?(?:[^&]*&)*v=|shorts/|embed/)|youtu\.be/)([A-Za-z0-9_-]{6,20})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    public static string? TryParse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var trimmed = input.Trim();

        // Bare ID — fastest path.
        if (BareIdRegex().IsMatch(trimmed)) return trimmed;

        var match = UrlRegex().Match(trimmed);
        return match.Success ? match.Groups[1].Value : null;
    }
}
