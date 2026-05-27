namespace CredoCms.Application.YouTube;

public sealed record YouTubeVideo(
    string VideoId,
    string ChannelId,
    string Title,
    string? Description,
    DateTimeOffset PublishedAt,
    int? DurationSeconds,
    string? ThumbnailUrl,
    IReadOnlyList<string> Tags);

public interface IYouTubeApiClient
{
    /// <summary>Fetch a single video's metadata. Returns null if not found.</summary>
    Task<YouTubeVideo?> GetByIdAsync(string videoId, CancellationToken ct = default);

    /// <summary>List videos for a channel, newest first. <paramref name="since"/>
    /// caps how far back the listing goes; null = no time bound.</summary>
    Task<IReadOnlyList<YouTubeVideo>> SearchChannelAsync(
        string channelId, DateTimeOffset? since, CancellationToken ct = default);
}

public interface IYouTubeTranscriptClient
{
    /// <summary>Best-effort transcript fetch via the unofficial timedtext
    /// endpoint. Returns null if unavailable.</summary>
    Task<string?> FetchTranscriptAsync(string videoId, CancellationToken ct = default);
}
