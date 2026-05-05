using System.Text;
using System.Xml.Linq;
using CredoCms.Application.YouTube;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.YouTube;

/// <summary>
/// Best-effort transcript fetch via the unofficial YouTube timedtext
/// endpoint. Failure → null (sermon imports with TranscriptSource.None
/// and editors can paste a transcript manually).
/// </summary>
public sealed class YouTubeTranscriptClient : IYouTubeTranscriptClient
{
    public const string HttpClientName = "youtube-timedtext";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<YouTubeTranscriptClient> _logger;

    public YouTubeTranscriptClient(IHttpClientFactory httpFactory, ILogger<YouTubeTranscriptClient> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<string?> FetchTranscriptAsync(string videoId, CancellationToken ct = default)
    {
        try
        {
            var http = _httpFactory.CreateClient(HttpClientName);
            // Try English first; YouTube returns 200 with empty body or 404 if absent.
            var url = $"https://www.youtube.com/api/timedtext?v={Uri.EscapeDataString(videoId)}&lang=en";
            using var resp = await http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body)) return null;
            return ParseXmlPayload(body);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Transcript fetch failed for video {VideoId}", videoId);
            return null;
        }
    }

    /// <summary>
    /// timedtext returns &lt;transcript&gt;&lt;text start="..."&gt;...&lt;/text&gt;...&lt;/transcript&gt;.
    /// Strip the structure, decode entities, return joined plain text.
    /// </summary>
    internal static string? ParseXmlPayload(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var sb = new StringBuilder();
            foreach (var el in doc.Descendants("text"))
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(System.Net.WebUtility.HtmlDecode(el.Value));
            }
            var text = sb.ToString().Trim();
            return text.Length == 0 ? null : text;
        }
        catch
        {
            return null;
        }
    }
}
