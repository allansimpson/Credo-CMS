using CredoCms.Application.Email;
using CredoCms.Application.Search;
using CredoCms.Domain.Blog;
using CredoCms.Domain.News;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.BackgroundServices;

/// <summary>
/// Phase 5 R10. Ticks every 60 seconds and publishes News + Blog entries
/// whose <c>ScheduledPublishAt</c> has passed. Per-record errors are
/// logged and don't crash the worker. Triggers email-on-publish via the
/// same service the create/update flows use, so a scheduled publish has
/// identical semantics to a manual one.
///
/// <para>Safe under single-instance deployment (the v1 target). For
/// future multi-instance scale-out, the read-then-flip needs a
/// RowVersion-guarded update — left as a TODO since the current SaveChanges
/// path is a sufficient guard for v1.</para>
/// </summary>
public sealed class ScheduledPublishingService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledPublishingService> _logger;

    public ScheduledPublishingService(IServiceScopeFactory scopeFactory, ILogger<ScheduledPublishingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledPublishingService started, tick={Tick}", TickInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(stoppingToken).ConfigureAwait(false); }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "ScheduledPublishingService tick threw");
            }
            await Task.Delay(TickInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var search = scope.ServiceProvider.GetService<ISearchIndexer>();
        var emailOnPublish = scope.ServiceProvider.GetRequiredService<IEmailOnPublishService>();

        await PublishDueNewsAsync(db, search, emailOnPublish, now, ct).ConfigureAwait(false);
        await PublishDueBlogAsync(db, search, emailOnPublish, now, ct).ConfigureAwait(false);
    }

    private async Task PublishDueNewsAsync(
        ApplicationDbContext db, ISearchIndexer? search, IEmailOnPublishService emailOnPublish,
        DateTimeOffset now, CancellationToken ct)
    {
        var due = await db.News
            .Where(n => !n.IsPublished && n.ScheduledPublishAt != null && n.ScheduledPublishAt <= now)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var n in due)
        {
            try
            {
                n.IsPublished = true;
                n.PublishedAt = now;
                n.ScheduledPublishAt = null;
                n.ModifiedAt = now;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                if (search is not null)
                {
                    await search.UpsertAsync(new SearchUpsertCommand(
                        EntityType: nameof(NewsItem), EntityId: n.Id,
                        Title: n.Title,
                        BodyText: n.Excerpt ?? string.Empty,
                        Url: "/news/" + n.Slug,
                        IsPublished: true, IsMembersOnly: n.IsMembersOnly), ct).ConfigureAwait(false);
                }

                if (n.SendEmailOnPublish)
                {
                    var broadcastId = await emailOnPublish.OnNewsPublishedAsync(n, ct).ConfigureAwait(false);
                    if (broadcastId is not null)
                    {
                        n.SendEmailOnPublish = false;
                        await db.SaveChangesAsync(ct).ConfigureAwait(false);
                    }
                }

                _logger.LogInformation("ScheduledPublishingService published News {Id} ({Slug})", n.Id, n.Slug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScheduledPublishingService failed to publish News {Id}", n.Id);
            }
        }
    }

    private async Task PublishDueBlogAsync(
        ApplicationDbContext db, ISearchIndexer? search, IEmailOnPublishService emailOnPublish,
        DateTimeOffset now, CancellationToken ct)
    {
        var due = await db.BlogPosts
            .Where(p => !p.IsPublished && p.ScheduledPublishAt != null && p.ScheduledPublishAt <= now)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var p in due)
        {
            try
            {
                p.IsPublished = true;
                p.PublishedAt = now;
                p.ScheduledPublishAt = null;
                p.ModifiedAt = now;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                if (search is not null)
                {
                    await search.UpsertAsync(new SearchUpsertCommand(
                        EntityType: nameof(BlogPost), EntityId: p.Id,
                        Title: p.Title,
                        BodyText: p.Excerpt ?? string.Empty,
                        Url: "/blog/" + p.Slug,
                        IsPublished: true, IsMembersOnly: p.IsMembersOnly), ct).ConfigureAwait(false);
                }

                if (p.SendEmailOnPublish)
                {
                    var broadcastId = await emailOnPublish.OnBlogPublishedAsync(p, ct).ConfigureAwait(false);
                    if (broadcastId is not null)
                    {
                        p.SendEmailOnPublish = false;
                        await db.SaveChangesAsync(ct).ConfigureAwait(false);
                    }
                }

                _logger.LogInformation("ScheduledPublishingService published Blog {Id} ({Slug})", p.Id, p.Slug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScheduledPublishingService failed to publish Blog {Id}", p.Id);
            }
        }
    }
}
