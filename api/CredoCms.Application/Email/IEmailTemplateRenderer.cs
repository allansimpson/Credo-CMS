using CredoCms.Application.SiteSettingsManagement;

namespace CredoCms.Application.Email;

/// <summary>
/// Resolves a template by key, runs <c>{{variable}}</c> substitution
/// against the supplied context, and returns the rendered subject + bodies.
/// Strict mode (the default) throws <see cref="TemplateRenderException"/>
/// when a referenced variable isn't present in the context — silent
/// fallthrough is the worst case for transactional email (a token like
/// <c>{{resetLink}}</c> in the recipient's inbox is the kind of bug that
/// erodes trust).
/// </summary>
public interface IEmailTemplateRenderer
{
    Task<RenderedEmail> RenderAsync(
        string templateKey,
        IReadOnlyDictionary<string, string> context,
        CancellationToken ct = default);
}

public sealed record RenderedEmail(string Subject, string HtmlBody, string PlainTextBody);

public sealed class TemplateRenderException : Exception
{
    public TemplateRenderException(string templateKey, string missingVariable)
        : base($"Template '{templateKey}' references {{{{{missingVariable}}}}} but the context does not provide it.") { }

    public TemplateRenderException(string message) : base(message) { }
}

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    /// <summary>Reserved keys auto-injected on every render.</summary>
    public static readonly IReadOnlyCollection<string> CommonMergeFields =
        new[] { "churchName", "currentYear", "unsubscribeLink" };

    private readonly IEmailTemplateRepository _repo;
    private readonly ISiteSettingsRepository _settings;

    public EmailTemplateRenderer(IEmailTemplateRepository repo, ISiteSettingsRepository settings)
    {
        _repo = repo;
        _settings = settings;
    }

    public async Task<RenderedEmail> RenderAsync(
        string templateKey,
        IReadOnlyDictionary<string, string> context,
        CancellationToken ct = default)
    {
        var template = await _repo.GetByKeyAsync(templateKey, ct).ConfigureAwait(false)
            ?? throw new TemplateRenderException($"Template '{templateKey}' not found.");

        var settings = await _settings.GetAsync(ct).ConfigureAwait(false);

        // Build the merged context: caller-provided + auto-injected commons.
        // Caller wins on collisions so a per-template churchName override is
        // possible (rare but supported).
        var merged = new Dictionary<string, string>(context, StringComparer.OrdinalIgnoreCase)
        {
            ["churchName"] = context.TryGetValue("churchName", out var cn) ? cn : settings.ChurchName,
            ["currentYear"] = context.TryGetValue("currentYear", out var cy) ? cy : DateTime.UtcNow.Year.ToString(),
            ["unsubscribeLink"] = context.TryGetValue("unsubscribeLink", out var ul) ? ul : string.Empty,
        };

        var subject = Substitute(templateKey, template.Subject, merged);
        var html = Substitute(templateKey, template.HtmlBody, merged);
        var text = template.PlainTextBody is not null
            ? Substitute(templateKey, template.PlainTextBody, merged)
            : DerivePlainText(html);

        return new RenderedEmail(subject, html, text);
    }

    /// <summary>Replaces every <c>{{key}}</c> token in <paramref name="input"/>
    /// with the matching context value. Throws when a token has no
    /// corresponding key — the caller's fault, surfaced loudly.</summary>
    internal static string Substitute(string templateKey, string input, IReadOnlyDictionary<string, string> context)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder(input.Length);
        var i = 0;
        while (i < input.Length)
        {
            var open = input.IndexOf("{{", i, StringComparison.Ordinal);
            if (open < 0) { sb.Append(input, i, input.Length - i); break; }
            sb.Append(input, i, open - i);
            var close = input.IndexOf("}}", open + 2, StringComparison.Ordinal);
            if (close < 0)
            {
                // Unterminated — append raw and stop.
                sb.Append(input, open, input.Length - open);
                break;
            }
            var key = input.Substring(open + 2, close - (open + 2)).Trim();
            if (!context.TryGetValue(key, out var value))
            {
                throw new TemplateRenderException(templateKey, key);
            }
            sb.Append(value);
            i = close + 2;
        }
        return sb.ToString();
    }

    /// <summary>Derives a plain-text fallback from HTML by stripping tags.
    /// Crude but adequate for transactional templates whose authors don't
    /// always supply a manual override.</summary>
    private static string DerivePlainText(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        // Replace common block-level tags with newlines first so the
        // stripped text retains some structure.
        var withNewlines = System.Text.RegularExpressions.Regex.Replace(
            html, "</?(p|br|div|li|h[1-6])[^>]*>", "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var stripped = System.Text.RegularExpressions.Regex.Replace(withNewlines, "<[^>]+>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(stripped).Trim();
    }
}
