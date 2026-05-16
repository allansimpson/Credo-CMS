namespace CredoCms.Application.SiteSettingsManagement;

/// <summary>
/// WCAG relative-luminance + contrast-ratio helpers for the Public Site
/// design handoff's tenant-color warning. AAA targets 7:1 (large text 4.5:1);
/// AA targets 4.5:1 (large text 3:1). We warn (don't block) when a tenant's
/// chosen primary/accent drops below AA against the template background —
/// some churches have inherited brand colors they must use even at the
/// edge.
/// </summary>
public static class ColorContrast
{
    /// <summary>WCAG 2.1 AA threshold for normal text.</summary>
    public const double WcagAaNormal = 4.5;

    /// <summary>WCAG 2.1 AAA threshold for normal text.</summary>
    public const double WcagAaaNormal = 7.0;

    /// <summary>Parse a "#rrggbb" hex string. Returns null when the input
    /// is malformed.</summary>
    public static (byte R, byte G, byte B)? ParseHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        var s = hex.Trim();
        if (s.Length == 7 && s[0] == '#'
            && byte.TryParse(s.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out var r)
            && byte.TryParse(s.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)
            && byte.TryParse(s.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return (r, g, b);
        }
        return null;
    }

    /// <summary>WCAG relative luminance (0.0–1.0).</summary>
    public static double RelativeLuminance(byte r, byte g, byte b)
    {
        static double Linearize(byte channel)
        {
            var s = channel / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linearize(r) + 0.7152 * Linearize(g) + 0.0722 * Linearize(b);
    }

    /// <summary>WCAG contrast ratio between two colors (1.0 = same color,
    /// 21.0 = black-on-white).</summary>
    public static double ContrastRatio(string fg, string bg)
    {
        var f = ParseHex(fg);
        var b = ParseHex(bg);
        if (f is null || b is null) return 1.0;
        var lf = RelativeLuminance(f.Value.R, f.Value.G, f.Value.B);
        var lb = RelativeLuminance(b.Value.R, b.Value.G, b.Value.B);
        var lighter = Math.Max(lf, lb);
        var darker = Math.Min(lf, lb);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>Template-fixed background hex used for the contrast check.
    /// Matches <c>--background</c> in <c>church-theme.css</c> per template.</summary>
    public static string TemplateBackground(CredoCms.Domain.Settings.PublicTemplate template) =>
        template switch
        {
            CredoCms.Domain.Settings.PublicTemplate.Quiet => "#fbfaf7",
            _ => "#f6f4ef",
        };
}
