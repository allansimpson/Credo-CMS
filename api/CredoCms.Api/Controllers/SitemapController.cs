using System.Text;
using CredoCms.Application.Leaders;
using CredoCms.Application.News;
using CredoCms.Application.Pages;
using CredoCms.Application.SiteSettingsManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Renders <c>/sitemap.xml</c> for crawlers. Enumerates published Pages,
/// News, and Leaders that aren't members-only. Cached 1 hour by P12's
/// output cache (tagged "sitemap").
/// </summary>
[ApiController]
[Route("/")]
public sealed class SitemapController : ControllerBase
{
    private readonly IPageService _pages;
    private readonly INewsService _news;
    private readonly ILeaderService _leaders;
    private readonly IOptions<CredoCms.Infrastructure.Configuration.PublicSiteOptions> _publicSite;

    public SitemapController(
        IPageService pages,
        INewsService news,
        ILeaderService leaders,
        ISiteSettingsRepository _ /* ensures DI is wired even if not used here */,
        IOptions<CredoCms.Infrastructure.Configuration.PublicSiteOptions> publicSite)
    {
        _pages = pages;
        _news = news;
        _leaders = leaders;
        _publicSite = publicSite;
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> SitemapAsync(CancellationToken ct)
    {
        var baseUrl = _publicSite.Value.BaseUrl?.TrimEnd('/') ?? "";
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Static routes
        AppendUrl(sb, baseUrl, "/", priority: 1.0);
        AppendUrl(sb, baseUrl, "/about", priority: 0.7);
        AppendUrl(sb, baseUrl, "/service-times", priority: 0.7);
        AppendUrl(sb, baseUrl, "/news", priority: 0.6);
        AppendUrl(sb, baseUrl, "/leaders", priority: 0.6);
        AppendUrl(sb, baseUrl, "/documents", priority: 0.5);

        // Pages — only published, non-members-only.
        var pages = await _pages.ListPublicAsync(includeMembersOnly: false, ct).ConfigureAwait(false);
        foreach (var p in pages)
            AppendUrl(sb, baseUrl, "/" + p.Slug, lastmod: p.PublishedAt, priority: 0.7);

        // News — first page of public listings; full crawl beyond that is
        // discouraged for SEO reasons (event-style content has limited
        // long-tail value).
        var news = await _news.ListPublicAsync(includeMembersOnly: false, page: 1, pageSize: 50, category: null, ct: ct).ConfigureAwait(false);
        foreach (var n in news.Items)
            AppendUrl(sb, baseUrl, "/news/" + n.Slug, lastmod: n.PublishedAt, priority: 0.5);

        // Leaders — public list.
        var leaders = await _leaders.ListPublicAsync(ct).ConfigureAwait(false);
        foreach (var l in leaders)
            AppendUrl(sb, baseUrl, "/leaders/" + l.Id, priority: 0.4);

        sb.Append("</urlset>");
        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("robots.txt")]
    public IActionResult RobotsTxt()
    {
        var baseUrl = _publicSite.Value.BaseUrl?.TrimEnd('/') ?? "";
        var body = $$"""
            User-agent: *
            Disallow: /admin/
            Disallow: /api/
            Disallow: /docs/
            Disallow: /profile
            Disallow: /search
            Disallow: /documents/

            Sitemap: {{baseUrl}}/sitemap.xml
            """;
        return Content(body, "text/plain", Encoding.UTF8);
    }

    private static void AppendUrl(StringBuilder sb, string baseUrl, string path,
        DateTimeOffset? lastmod = null, double? priority = null)
    {
        sb.Append("<url><loc>");
        sb.Append(System.Net.WebUtility.HtmlEncode(baseUrl + path));
        sb.Append("</loc>");
        if (lastmod is not null)
        {
            sb.Append("<lastmod>");
            sb.Append(lastmod.Value.UtcDateTime.ToString("yyyy-MM-dd"));
            sb.Append("</lastmod>");
        }
        if (priority is not null)
        {
            sb.Append("<priority>");
            sb.Append(priority.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append("</priority>");
        }
        sb.Append("</url>");
    }
}
