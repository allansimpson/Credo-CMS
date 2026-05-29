using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.YouTube;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.YouTube;

public sealed class YouTubeApiClient : IYouTubeApiClient
{
    private readonly ISiteSettingsRepository _settings;
    private readonly ILogger<YouTubeApiClient> _logger;

    public YouTubeApiClient(ISiteSettingsRepository settings, ILogger<YouTubeApiClient> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<YouTubeVideo?> GetByIdAsync(string videoId, CancellationToken ct = default)
    {
        var apiKey = (await _settings.GetAsync(ct).ConfigureAwait(false)).YouTubeApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("YouTube API key not configured.");
            return null;
        }

        using var svc = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = apiKey,
            ApplicationName = "CredoCms",
        });

        var req = svc.Videos.List("snippet,contentDetails");
        req.Id = videoId;
        var response = await req.ExecuteAsync(ct).ConfigureAwait(false);
        var v = response.Items.FirstOrDefault();
        if (v is null) return null;

        return new YouTubeVideo(
            VideoId: v.Id,
            ChannelId: v.Snippet.ChannelId,
            Title: v.Snippet.Title,
            Description: v.Snippet.Description,
            PublishedAt: v.Snippet.PublishedAtDateTimeOffset ?? DateTimeOffset.UtcNow,
            DurationSeconds: ParseIso8601Duration(v.ContentDetails.Duration),
            ThumbnailUrl: v.Snippet.Thumbnails?.High?.Url ?? v.Snippet.Thumbnails?.Default__?.Url,
            Tags: ((IReadOnlyList<string>?)v.Snippet.Tags?.ToList()) ?? Array.Empty<string>());
    }

    public async Task<IReadOnlyList<YouTubeVideo>> SearchChannelAsync(
        string channelId, DateTimeOffset? since, CancellationToken ct = default)
    {
        var apiKey = (await _settings.GetAsync(ct).ConfigureAwait(false)).YouTubeApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("YouTube API key not configured.");
            return Array.Empty<YouTubeVideo>();
        }

        using var svc = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = apiKey,
            ApplicationName = "CredoCms",
        });

        var allVideoIds = new List<string>();
        string? pageToken = null;

        do
        {
            var search = svc.Search.List("snippet");
            search.ChannelId = channelId;
            search.Type = "video";
            search.Order = SearchResource.ListRequest.OrderEnum.Date;
            search.MaxResults = 50;
            if (since is { } s) search.PublishedAfterDateTimeOffset = s;
            if (pageToken is not null) search.PageToken = pageToken;

            var searchResponse = await search.ExecuteAsync(ct).ConfigureAwait(false);
            var ids = searchResponse.Items
                .Where(i => i.Id?.Kind == "youtube#video")
                .Select(i => i.Id.VideoId)
                .ToList();
            allVideoIds.AddRange(ids);

            pageToken = searchResponse.NextPageToken;
            _logger.LogInformation("YouTube search page fetched: {Count} videos (total so far: {Total}, hasMore: {HasMore})",
                ids.Count, allVideoIds.Count, pageToken is not null);
        } while (pageToken is not null);

        if (allVideoIds.Count == 0) return Array.Empty<YouTubeVideo>();

        var results = new List<YouTubeVideo>();
        foreach (var batch in allVideoIds.Chunk(50))
        {
            var detailReq = svc.Videos.List("snippet,contentDetails");
            detailReq.Id = string.Join(",", batch);
            var detailResponse = await detailReq.ExecuteAsync(ct).ConfigureAwait(false);

            results.AddRange(detailResponse.Items.Select(v => new YouTubeVideo(
                VideoId: v.Id,
                ChannelId: v.Snippet.ChannelId,
                Title: v.Snippet.Title,
                Description: v.Snippet.Description,
                PublishedAt: v.Snippet.PublishedAtDateTimeOffset ?? DateTimeOffset.UtcNow,
                DurationSeconds: ParseIso8601Duration(v.ContentDetails.Duration),
                ThumbnailUrl: v.Snippet.Thumbnails?.High?.Url ?? v.Snippet.Thumbnails?.Default__?.Url,
                Tags: ((IReadOnlyList<string>?)v.Snippet.Tags?.ToList()) ?? Array.Empty<string>())));
        }

        return results;
    }

    /// <summary>Parses an ISO 8601 duration ("PT1H23M45S") to seconds.</summary>
    internal static int? ParseIso8601Duration(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;
        try { return (int)System.Xml.XmlConvert.ToTimeSpan(iso).TotalSeconds; }
        catch { return null; }
    }
}
