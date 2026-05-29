using System.Collections.Concurrent;
using System.Text.Json;
using CredoCms.Application.Common;
using CredoCms.Application.RealTime;
using CredoCms.Application.Scripture;
using CredoCms.Application.Sermons;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.Storage;
using CredoCms.Application.YouTube;
using CredoCms.Domain.Common;
using CredoCms.Domain.Sermons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.YouTube;

/// <summary>
/// Periodic YouTube channel sync. Reads <see cref="Application.SiteSettingsManagement.ISiteSettingsRepository"/>
/// for the channel ID + interval, dedupes against existing sermons by
/// YouTube video ID, copies thumbnails to blob storage, attempts
/// transcript via <see cref="IYouTubeTranscriptClient"/> (best-effort),
/// and creates draft sermons. On error: logs, updates LastSyncStatus,
/// does not crash.
/// </summary>
public sealed class YouTubeSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<YouTubeSyncService> _logger;
    private readonly ConcurrentQueue<TaskCompletionSource> _manualTriggers = new();

    public YouTubeSyncService(IServiceScopeFactory scopes, ILogger<YouTubeSyncService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    /// <summary>Public hook for the admin "Run Sync Now" endpoint.</summary>
    public Task TriggerNowAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _manualTriggers.Enqueue(tcs);
        return tcs.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger the first run by 30s so the app finishes startup first.
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            // Drain manual triggers first (each gets the next run's outcome).
            var manualWaiters = new List<TaskCompletionSource>();
            while (_manualTriggers.TryDequeue(out var t)) manualWaiters.Add(t);

            int interval;
            try
            {
                interval = await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YouTube sync run threw unhandled.");
                interval = 360;
            }

            foreach (var w in manualWaiters) w.TrySetResult();

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(Math.Max(15, interval)), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return; }
        }
    }

    /// <returns>Interval in minutes to wait before the next run.</returns>
    private async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<ISiteSettingsRepository>();
        var settings = await settingsRepo.GetAsync(ct).ConfigureAwait(false);

        if (!settings.YouTubeSyncEnabled || string.IsNullOrWhiteSpace(settings.YouTubeChannelId))
            return settings.YouTubeSyncIntervalMinutes;

        var youtube = scope.ServiceProvider.GetRequiredService<IYouTubeApiClient>();
        var transcripts = scope.ServiceProvider.GetRequiredService<IYouTubeTranscriptClient>();
        var sermonService = scope.ServiceProvider.GetRequiredService<ISermonService>();
        var sermonRepo = scope.ServiceProvider.GetRequiredService<ISermonRepository>();
        var blobs = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
        var notifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditLogger>();

        try
        {
            var channelId = settings.YouTubeChannelId;
            var since = settings.YouTubeLastSuccessfulSyncAt;
            _logger.LogInformation("YouTube sync starting. ChannelId={ChannelId}, Since={Since}",
                channelId, since?.ToString("o") ?? "(all time)");

            var videos = await youtube.SearchChannelAsync(channelId, since, ct).ConfigureAwait(false);
            _logger.LogInformation("YouTube API returned {Count} videos from channel search.", videos.Count);

            int imported = 0;
            int skippedDuplicate = 0;
            foreach (var video in videos)
            {
                var existing = await sermonRepo.GetByYouTubeVideoIdAsync(video.VideoId, includeDeleted: true, ct).ConfigureAwait(false);
                if (existing is not null)
                {
                    skippedDuplicate++;
                    _logger.LogDebug("Skipping duplicate: {VideoId} - {Title}", video.VideoId, video.Title);
                    continue;
                }
                _logger.LogInformation("Importing: {VideoId} - {Title} (published {PublishedAt})",
                    video.VideoId, video.Title, video.PublishedAt.ToString("o"));

                string? thumbnailBlobUrl = null;
                if (!string.IsNullOrWhiteSpace(video.ThumbnailUrl))
                {
                    try
                    {
                        thumbnailBlobUrl = await CopyThumbnailAsync(blobs, video.VideoId, video.ThumbnailUrl, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to copy thumbnail for {VideoId}", video.VideoId);
                    }
                }

                var transcript = await transcripts.FetchTranscriptAsync(video.VideoId, ct).ConfigureAwait(false);

                var defaultTags = ParseDefaultTags(settings.YouTubeDefaultTagsJson);
                var allTags = video.Tags
                    .Concat(defaultTags)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(t => new SermonTagInput(null, t))
                    .ToList();

                var slug = MakeUniqueSlug(video.Title, video.VideoId);

                var request = new CreateSermonRequest(
                    Slug: slug,
                    Title: video.Title,
                    DescriptionJson: WrapDescriptionAsTipTap(video.Description),
                    YouTubeVideoId: video.VideoId,
                    YouTubeChannelId: video.ChannelId,
                    ThumbnailBlobUrl: thumbnailBlobUrl,
                    ThumbnailWebpBlobUrl: null,
                    PublishedAt: video.PublishedAt,
                    YouTubePublishedAt: video.PublishedAt,
                    DurationSeconds: video.DurationSeconds,
                    Transcript: transcript,
                    TranscriptSource: transcript is not null ? TranscriptSource.YouTubeAuto : TranscriptSource.None,
                    SpeakerLeaderId: null,
                    SpeakerNameFreeText: null,
                    SermonSeriesId: null,
                    ServiceType: InferServiceType(video.PublishedAt),
                    IsPublished: settings.YouTubeAutoPublishOnSync,
                    IsMembersOnly: false);

                var result = await sermonService.CreateAsync(request, ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Auto-import failed for {VideoId}: {Errors}", video.VideoId, string.Join("; ", result.Errors));
                    continue;
                }
                imported++;
                await audit.WriteAsync("Sermon.AutoImported", nameof(Sermon), result.Sermon!.Id.ToString(),
                    details: new { video.VideoId, video.Title }, cancellationToken: ct).ConfigureAwait(false);
            }

            // Update last-sync state.
            settings.YouTubeLastSuccessfulSyncAt = DateTimeOffset.UtcNow;
            settings.YouTubeLastSyncStatus = "Success";
            settings.YouTubeLastSyncImportedCount = imported;
            await settingsRepo.UpdateAsync(settings, ct).ConfigureAwait(false);

            await notifier.NotifyContentChangedAsync(
                new ContentChangedMessage("SermonSync", Guid.Empty, "Completed"), ct).ConfigureAwait(false);

            _logger.LogInformation("YouTube sync run completed. Imported={Imported}, Skipped={Skipped} (duplicates), APIReturned={Total}",
                imported, skippedDuplicate, videos.Count);
            return settings.YouTubeSyncIntervalMinutes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube sync run failed.");
            try
            {
                settings.YouTubeLastSyncStatus = $"Error: {Truncate(ex.Message, 480)}";
                await settingsRepo.UpdateAsync(settings, ct).ConfigureAwait(false);
            }
            catch { /* best-effort */ }
            return settings.YouTubeSyncIntervalMinutes;
        }
    }

    private static async Task<string?> CopyThumbnailAsync(IBlobStorageService blobs, string videoId, string thumbnailUrl, CancellationToken ct)
    {
        using var http = new HttpClient();
        using var resp = await http.GetAsync(thumbnailUrl, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        await using var ms = new MemoryStream(bytes);
        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        var ext = contentType == "image/webp" ? "webp" : "jpg";
        var blobName = $"sermon-thumbnails/{videoId}.{ext}";
        return await blobs.UploadAsync(blobName, ms, contentType, ct).ConfigureAwait(false);
    }

    private static string MakeUniqueSlug(string title, string videoId)
    {
        var slug = (title ?? string.Empty).ToLowerInvariant();
        var safe = new System.Text.StringBuilder(slug.Length);
        foreach (var c in slug) safe.Append(char.IsLetterOrDigit(c) ? c : '-');
        var s = safe.ToString().Trim('-');
        while (s.Contains("--", StringComparison.Ordinal)) s = s.Replace("--", "-", StringComparison.Ordinal);
        if (s.Length == 0) s = "sermon";
        if (s.Length > 160) s = s[..160];
        // Sanitize video ID — replace underscores and non-alphanumeric chars with dashes
        var safeId = new System.Text.StringBuilder(videoId.Length);
        foreach (var c in videoId) safeId.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        var idPart = safeId.ToString().Trim('-');
        return $"{s}-{idPart}";
    }

    private static ServiceType InferServiceType(DateTimeOffset publishedAt)
    {
        var hour = publishedAt.Hour;
        var dow = publishedAt.DayOfWeek;
        if (dow == DayOfWeek.Wednesday) return ServiceType.WednesdayNight;
        if (hour < 10) return ServiceType.AmBibleClass;
        if (hour < 13) return ServiceType.AmWorship;
        if (hour >= 17) return ServiceType.PmWorship;
        return ServiceType.AmWorship;
    }

    private static string? WrapDescriptionAsTipTap(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        var paragraphs = description.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var doc = new
        {
            type = "doc",
            content = paragraphs.Select(p => new
            {
                type = "paragraph",
                content = new[] { new { type = "text", text = p } },
            }).ToArray(),
        };
        return JsonSerializer.Serialize(doc);
    }

    private static IEnumerable<string> ParseDefaultTags(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return Array.Empty<string>();
            return doc.RootElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }
        catch (JsonException) { return Array.Empty<string>(); }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
