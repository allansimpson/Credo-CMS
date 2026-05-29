using System.Text;
using System.Text.Json;

namespace CredoCms.Application.Common;

/// <summary>
/// Helpers for pulling plain text out of ProseMirror / TipTap JSON. Used
/// when a surface needs an unformatted snippet — admin list previews, RSS
/// descriptions, email plain-text fallbacks, or the truncated description
/// shown on the public Sermon Series hero card.
/// </summary>
public static class ProseMirrorText
{
    /// <summary>
    /// Walks every <c>text</c> leaf in the document, concatenated with a
    /// single space between siblings. Returns an empty string if the input
    /// is null, empty, or unparseable — never throws.
    /// </summary>
    public static string ExtractText(string? proseMirrorJson)
    {
        if (string.IsNullOrWhiteSpace(proseMirrorJson)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(proseMirrorJson);
            var sb = new StringBuilder();
            Walk(doc.RootElement, sb);
            return sb.ToString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Plain-text excerpt of at most <paramref name="maxLength"/> characters,
    /// trimmed at a word boundary, with an ellipsis if truncated. Returns an
    /// empty string when the input has no text.
    /// </summary>
    public static string Excerpt(string? proseMirrorJson, int maxLength = 280)
    {
        var text = ExtractText(proseMirrorJson);
        if (text.Length == 0) return string.Empty;
        if (text.Length <= maxLength) return text;

        var cut = text.AsSpan(0, maxLength).ToString();
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > maxLength / 2) cut = cut[..lastSpace];
        return cut.TrimEnd(' ', ',', '.', ';', ':', '-') + "…";
    }

    private static void Walk(JsonElement el, StringBuilder sb)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                if (el.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                {
                    sb.Append(t.GetString()).Append(' ');
                }
                if (el.TryGetProperty("content", out var c))
                {
                    Walk(c, sb);
                }
                break;
            case JsonValueKind.Array:
                foreach (var child in el.EnumerateArray()) Walk(child, sb);
                break;
        }
    }
}
