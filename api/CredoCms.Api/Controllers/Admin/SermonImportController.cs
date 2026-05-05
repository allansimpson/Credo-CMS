using CredoCms.Application.Common;
using CredoCms.Application.Scripture;
using CredoCms.Application.Sermons;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.YouTube;
using CredoCms.Domain.Common;
using CredoCms.Domain.Sermons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/sermons")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class SermonImportController : ControllerBase
{
    private readonly IYouTubeApiClient _youtube;
    private readonly IYouTubeTranscriptClient _transcripts;
    private readonly ISermonService _sermons;
    private readonly ISermonRepository _sermonRepo;
    private readonly ISiteSettingsRepository _siteSettings;
    private readonly IAuditLogger _audit;

    public SermonImportController(
        IYouTubeApiClient youtube,
        IYouTubeTranscriptClient transcripts,
        ISermonService sermons,
        ISermonRepository sermonRepo,
        ISiteSettingsRepository siteSettings,
        IAuditLogger audit)
    {
        _youtube = youtube;
        _transcripts = transcripts;
        _sermons = sermons;
        _sermonRepo = sermonRepo;
        _siteSettings = siteSettings;
        _audit = audit;
    }

    public sealed record ImportRequest(string UrlOrVideoId);

    /// <summary>Manual single-video import. Accepts a YouTube URL or bare video ID.</summary>
    [HttpPost("import")]
    public async Task<ActionResult<SermonDetailDto>> ImportAsync([FromBody] ImportRequest req, CancellationToken ct)
    {
        var videoId = YouTubeUrlParser.TryParse(req?.UrlOrVideoId);
        if (videoId is null) return BadRequest(new { errors = new[] { "Could not parse a YouTube video ID." } });

        var existing = await _sermonRepo.GetByYouTubeVideoIdAsync(videoId, includeDeleted: true, ct);
        if (existing is not null)
            return BadRequest(new { errors = new[] { "A sermon for that YouTube video already exists." } });

        var video = await _youtube.GetByIdAsync(videoId, ct);
        if (video is null)
            return BadRequest(new { errors = new[] { "Video not found via YouTube API. Check the API key and the video ID." } });

        var transcript = await _transcripts.FetchTranscriptAsync(video.VideoId, ct);

        var slug = (video.Title ?? string.Empty).ToLowerInvariant();
        var sb = new System.Text.StringBuilder(slug.Length);
        foreach (var c in slug) sb.Append(char.IsLetterOrDigit(c) ? c : '-');
        var s = sb.ToString().Trim('-');
        while (s.Contains("--", StringComparison.Ordinal)) s = s.Replace("--", "-", StringComparison.Ordinal);
        if (s.Length == 0) s = "sermon";
        if (s.Length > 160) s = s[..160];
        var draftSlug = $"{s}-{video.VideoId.ToLowerInvariant()}";

        var request = new CreateSermonRequest(
            Slug: draftSlug,
            Title: video.Title ?? "Untitled",
            DescriptionJson: null,
            YouTubeVideoId: video.VideoId,
            YouTubeChannelId: video.ChannelId,
            ThumbnailBlobUrl: null,
            ThumbnailWebpBlobUrl: null,
            PublishedAt: video.PublishedAt,
            YouTubePublishedAt: video.PublishedAt,
            DurationSeconds: video.DurationSeconds,
            Transcript: transcript,
            TranscriptSource: transcript is null ? TranscriptSource.None : TranscriptSource.YouTubeAuto,
            SpeakerLeaderId: null,
            SpeakerNameFreeText: null,
            SermonSeriesId: null,
            IsPublished: false,
            IsMembersOnly: false,
            Tags: video.Tags.Select(t => new SermonTagInput(null, t)).ToList(),
            AttachmentDocumentIds: new List<Guid>(),
            ScriptureReferences: new List<ScriptureReferenceInput>());

        var result = await _sermons.CreateAsync(request, ct);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors });

        await _audit.WriteAsync("Sermon.ManualImport", "Sermon", result.Sermon!.Id.ToString(),
            details: new { video.VideoId, video.Title }, cancellationToken: ct);
        return CreatedAtAction(nameof(SermonsController.GetAsync), "Sermons",
            new { id = result.Sermon.Id }, result.Sermon);
    }

    /// <summary>Manual sync trigger (Administrator only).</summary>
    [HttpPost("sync")]
    [Authorize(Roles = SystemConstants.Roles.Administrator)]
    public IActionResult TriggerSync([FromServices] CredoCms.Infrastructure.YouTube.YouTubeSyncService sync)
    {
        // Fire and forget — the background service drains the trigger queue
        // on its next loop iteration.
        _ = sync.TriggerNowAsync();
        _ = _audit.WriteAsync("Sermon.SyncTriggeredManually", "SermonSync");
        return Accepted();
    }
}
