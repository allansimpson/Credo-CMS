using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Blog;
using CredoCms.Domain.Email;
using CredoCms.Domain.News;
using CredoCms.Domain.Settings;

namespace CredoCms.Application.Email;

/// <summary>
/// Auto-creates an <see cref="EmailBroadcast"/> when a News or Blog post
/// is published with <c>SendEmailOnPublish=true</c>. The broadcast is
/// queued in <see cref="BroadcastStatus.Sending"/> so the worker picks it
/// up on the next tick. Caller must clear the entity's
/// <c>SendEmailOnPublish</c> flag in the same DB transaction to prevent
/// duplicate sends on re-publish — the service is intentionally side-
/// effect-only on its own broadcast row, leaving entity mutation to the
/// caller.
/// </summary>
public interface IEmailOnPublishService
{
    Task<Guid?> OnNewsPublishedAsync(NewsItem item, CancellationToken ct = default);
    Task<Guid?> OnBlogPublishedAsync(BlogPost post, CancellationToken ct = default);
}

public sealed class EmailOnPublishService : IEmailOnPublishService
{
    private readonly IEmailBroadcastRepository _broadcasts;
    private readonly ISiteSettingsRepository _settings;

    public EmailOnPublishService(IEmailBroadcastRepository broadcasts, ISiteSettingsRepository settings)
    {
        _broadcasts = broadcasts;
        _settings = settings;
    }

    public async Task<Guid?> OnNewsPublishedAsync(NewsItem item, CancellationToken ct = default)
    {
        if (!item.SendEmailOnPublish || !item.IsPublished) return null;
        var s = await _settings.GetAsync(ct).ConfigureAwait(false);
        var groupIds = ParseGuids(s.NewsEmailTargetGroupIdsJson);
        var b = BuildBroadcast(
            subject: $"{s.EmailSubjectPrefixNews} {item.Title}".Trim(),
            body: BuildPreviewHtml(item.Title, item.Excerpt, item.HeroImageUrl, $"/news/{item.Slug}"),
            text: BuildPreviewText(item.Title, item.Excerpt, $"/news/{item.Slug}"),
            mode: s.NewsEmailTargetMode,
            groupIds: groupIds,
            category: EmailCategory.News,
            sourceId: item.Id);
        await _broadcasts.AddAsync(b, ct).ConfigureAwait(false);
        return b.Id;
    }

    public async Task<Guid?> OnBlogPublishedAsync(BlogPost post, CancellationToken ct = default)
    {
        if (!post.SendEmailOnPublish || !post.IsPublished) return null;
        var s = await _settings.GetAsync(ct).ConfigureAwait(false);
        var groupIds = ParseGuids(s.BlogEmailTargetGroupIdsJson);
        var b = BuildBroadcast(
            subject: $"{s.EmailSubjectPrefixBlog} {post.Title}".Trim(),
            body: BuildPreviewHtml(post.Title, post.Excerpt, post.HeroImageBlobUrl, $"/blog/{post.Slug}"),
            text: BuildPreviewText(post.Title, post.Excerpt, $"/blog/{post.Slug}"),
            mode: s.BlogEmailTargetMode,
            groupIds: groupIds,
            category: EmailCategory.Blog,
            sourceId: post.Id);
        await _broadcasts.AddAsync(b, ct).ConfigureAwait(false);
        return b.Id;
    }

    private static EmailBroadcast BuildBroadcast(
        string subject, string body, string text,
        BroadcastTargetMode mode, IReadOnlyCollection<Guid>? groupIds,
        EmailCategory category, Guid sourceId)
    {
        var now = DateTimeOffset.UtcNow;
        return new EmailBroadcast
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Body = body,
            PlainTextBody = text,
            TargetMode = mode,
            TargetGroupIdsJson = SerializeGuids(groupIds),
            SendMode = BroadcastSendMode.SendNow,
            Status = BroadcastStatus.Sending,
            Category = category,
            SourceEntityId = sourceId,
            CreatedAt = now,
            ModifiedAt = now,
        };
    }

    /// <summary>Minimal preview HTML — hero image (if any), title, excerpt,
    /// "Read more" link. No fancy templating; the broadcast worker's send
    /// path will substitute {{firstName}} etc. as merge fields.</summary>
    private static string BuildPreviewHtml(string title, string? excerpt, string? heroUrl, string detailPath)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(heroUrl))
            sb.Append($"<p><img src=\"{System.Web.HttpUtility.HtmlAttributeEncode(heroUrl)}\" alt=\"\" style=\"max-width:100%\"/></p>");
        sb.Append($"<h2>{System.Web.HttpUtility.HtmlEncode(title)}</h2>");
        if (!string.IsNullOrWhiteSpace(excerpt))
            sb.Append($"<p>{System.Web.HttpUtility.HtmlEncode(excerpt)}</p>");
        sb.Append($"<p><a href=\"{detailPath}\">Read more</a></p>");
        return sb.ToString();
    }

    private static string BuildPreviewText(string title, string? excerpt, string detailPath)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(title);
        if (!string.IsNullOrWhiteSpace(excerpt))
        {
            sb.AppendLine();
            sb.AppendLine(excerpt);
        }
        sb.AppendLine();
        sb.AppendLine($"Read more: {detailPath}");
        return sb.ToString();
    }

    private static List<Guid>? ParseGuids(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json); }
        catch (System.Text.Json.JsonException) { return null; }
    }

    private static string? SerializeGuids(IReadOnlyCollection<Guid>? ids)
    {
        if (ids is null || ids.Count == 0) return null;
        return System.Text.Json.JsonSerializer.Serialize(ids);
    }
}
