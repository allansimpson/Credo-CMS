using CredoCms.Application.Blog;
using CredoCms.Application.News;
using CredoCms.Application.Rss;
using CredoCms.Application.Sermons;
using CredoCms.Application.SiteSettingsManagement;
using ISermonService = CredoCms.Application.Sermons.ISermonService;
using CredoCms.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace CredoCms.Api.Controllers.Public;

/// <summary>RSS 2.0 feeds for Blog, News, and Sermons. Public-only;
/// members-only items excluded by the underlying public list calls.
/// 50-item cap per feed; 15-min output cache, evicted on relevant entity
/// writes via the existing tag-based invalidator.</summary>
[ApiController]
[AllowAnonymous]
public sealed class RssFeedsController : ControllerBase
{
    private const int MaxItems = 50;
    private const string ContentType = "application/rss+xml; charset=utf-8";
    private const string Language = "en-us";

    private readonly IRssFeedBuilder _builder;
    private readonly ISiteSettingsService _settings;
    private readonly PublicSiteOptions _siteOptions;

    public RssFeedsController(
        IRssFeedBuilder builder,
        ISiteSettingsService settings,
        IOptions<PublicSiteOptions> siteOptions)
    {
        _builder = builder;
        _settings = settings;
        _siteOptions = siteOptions.Value;
    }

    [HttpGet("/blog/rss.xml")]
    [OutputCache(Duration = 900, Tags = new[] { "blog" })]
    public async Task<IActionResult> BlogAsync(
        [FromServices] IBlogService blog,
        CancellationToken ct)
    {
        var s = await _settings.GetPublicAsync(ct).ConfigureAwait(false);
        var posts = await blog.ListPublicAsync(category: null, page: 1, pageSize: MaxItems, ct: ct).ConfigureAwait(false);
        var baseUrl = _siteOptions.BaseUrl.TrimEnd('/');

        var channel = new RssChannelInfo(
            Title: $"{s.ChurchName} — Blog",
            Link: baseUrl + "/blog",
            Description: $"Latest blog posts from {s.ChurchName}",
            Language: Language,
            SelfLink: baseUrl + "/blog/rss.xml");

        var items = posts.Items.Select(p => new RssItem(
            Title: p.Title,
            Link: $"{baseUrl}/blog/{p.Slug}",
            Description: p.Excerpt ?? string.Empty,
            Author: p.AuthorDisplayName ?? string.Empty,
            Category: p.Category ?? string.Empty,
            PubDate: p.PublishedAt ?? p.ModifiedAt,
            PermalinkGuid: $"{baseUrl}/blog/{p.Slug}",
            EnclosureUrl: p.HeroImageBlobUrl,
            EnclosureType: p.HeroImageBlobUrl is not null ? "image/jpeg" : null)).ToList();

        var bytes = _builder.Build(channel, items);
        return File(bytes, ContentType);
    }

    [HttpGet("/news/rss.xml")]
    [OutputCache(Duration = 900, Tags = new[] { "news" })]
    public async Task<IActionResult> NewsAsync(
        [FromServices] INewsService news,
        CancellationToken ct)
    {
        var s = await _settings.GetPublicAsync(ct).ConfigureAwait(false);
        var page = await news.ListPublicAsync(includeMembersOnly: false, page: 1, pageSize: MaxItems, ct).ConfigureAwait(false);
        var baseUrl = _siteOptions.BaseUrl.TrimEnd('/');

        var channel = new RssChannelInfo(
            Title: $"{s.ChurchName} — News",
            Link: baseUrl + "/news",
            Description: $"Latest news from {s.ChurchName}",
            Language: Language,
            SelfLink: baseUrl + "/news/rss.xml");

        var items = page.Items.Select(n => new RssItem(
            Title: n.Title,
            Link: $"{baseUrl}/news/{n.Slug}",
            Description: n.Excerpt ?? string.Empty,
            Author: string.Empty,
            Category: string.Empty,
            PubDate: n.PublishedAt,
            PermalinkGuid: $"{baseUrl}/news/{n.Slug}",
            EnclosureUrl: n.HeroImageUrl,
            EnclosureType: n.HeroImageUrl is not null ? "image/jpeg" : null)).ToList();

        var bytes = _builder.Build(channel, items);
        return File(bytes, ContentType);
    }

    [HttpGet("/sermons/rss.xml")]
    [OutputCache(Duration = 900, Tags = new[] { "sermons" })]
    public async Task<IActionResult> SermonsAsync(
        [FromServices] ISermonService sermons,
        CancellationToken ct)
    {
        var s = await _settings.GetPublicAsync(ct).ConfigureAwait(false);
        var query = new SermonListQuery(PublishedOnly: true, Page: 1, PageSize: MaxItems);
        var page = await sermons.ListPublicAsync(query, includeMembersOnly: false, ct).ConfigureAwait(false);
        var baseUrl = _siteOptions.BaseUrl.TrimEnd('/');

        var channel = new RssChannelInfo(
            Title: $"{s.ChurchName} — Sermons",
            Link: baseUrl + "/sermons",
            Description: $"Latest sermons from {s.ChurchName}",
            Language: Language,
            SelfLink: baseUrl + "/sermons/rss.xml");

        var items = page.Items.Select(srm => new RssItem(
            Title: srm.Title,
            Link: $"{baseUrl}/sermons/{srm.Slug}",
            Description: srm.SermonSeriesTitle ?? string.Empty,
            Author: srm.SpeakerName ?? string.Empty,
            Category: srm.SermonSeriesTitle ?? string.Empty,
            PubDate: srm.PublishedAt,
            PermalinkGuid: $"{baseUrl}/sermons/{srm.Slug}",
            EnclosureUrl: srm.ThumbnailBlobUrl,
            EnclosureType: srm.ThumbnailBlobUrl is not null ? "image/jpeg" : null)).ToList();

        var bytes = _builder.Build(channel, items);
        return File(bytes, ContentType);
    }
}
