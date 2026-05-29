using System.Text.RegularExpressions;
using CredoCms.Application.Common;
using CredoCms.Application.Documents;
using CredoCms.Application.Pages;
using CredoCms.Application.Scripture;
using CredoCms.Application.Tags;
using CredoCms.Domain.Bible;
using CredoCms.Domain.Sermons;
using FluentValidation;

namespace CredoCms.Application.Sermons;

public interface ISermonRepository
{
    Task<PagedResult<SermonListItemDto>> ListAsync(SermonListQuery query, CancellationToken ct = default);
    Task<Sermon?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
    Task<Sermon?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Sermon?> GetByYouTubeVideoIdAsync(string videoId, bool includeDeleted = false, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default);

    Task<List<Guid>> GetTagIdsAsync(Guid sermonId, CancellationToken ct = default);
    Task ReplaceTagsAsync(Guid sermonId, IEnumerable<Guid> tagIds, CancellationToken ct = default);

    Task<List<Guid>> GetAttachmentIdsAsync(Guid sermonId, CancellationToken ct = default);
    Task ReplaceAttachmentsAsync(Guid sermonId, IEnumerable<Guid> documentIds, CancellationToken ct = default);

    Task<bool> AreAllPublicPdfsAsync(IEnumerable<Guid> documentIds, CancellationToken ct = default);

    Task AddAsync(Sermon sermon, CancellationToken ct = default);
    Task UpdateAsync(Sermon sermon, CancellationToken ct = default);
    Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default);

    Task<List<SermonsByBookCount>> CountByBookAsync(bool includeMembersOnly, CancellationToken ct = default);
    Task<SermonsByDayResponse> ListByDayAsync(SermonsByDayQuery query, bool includeMembersOnly, CancellationToken ct = default);

    /// <summary>Year + month rollup of every published sermon visible to the
    /// caller. Drives the side-rail's archive index.</summary>
    Task<YearsResponse> ListYearStatsAsync(bool includeMembersOnly, CancellationToken ct = default);

    Task<SermonDetailDto?> ToDetailAsync(Sermon sermon, CancellationToken ct = default);
    Task<PublicSermonDto?> ToPublicAsync(Sermon sermon, CancellationToken ct = default);
}

public sealed record SermonOperationResult(bool Succeeded, string[] Errors, SermonDetailDto? Sermon);

public interface ISermonService
{
    Task<PagedResult<SermonListItemDto>> ListAsync(SermonListQuery query, CancellationToken ct = default);
    Task<SermonDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
    Task<PublicSermonDto?> GetPublicBySlugAsync(string slug, bool includeMembersOnly, CancellationToken ct = default);
    Task<PagedResult<SermonListItemDto>> ListPublicAsync(SermonListQuery query, bool includeMembersOnly, CancellationToken ct = default);
    Task<List<SermonsByBookCount>> CountByBookAsync(bool includeMembersOnly, CancellationToken ct = default);
    Task<SermonsByDayResponse> ListPublicByDayAsync(SermonsByDayQuery query, bool includeMembersOnly, CancellationToken ct = default);
    Task<YearsResponse> ListYearStatsAsync(bool includeMembersOnly, CancellationToken ct = default);

    Task<SermonOperationResult> CreateAsync(CreateSermonRequest request, CancellationToken ct = default);
    Task<SermonOperationResult> UpdateAsync(Guid id, UpdateSermonRequest request, CancellationToken ct = default);
    Task<SermonOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<SermonOperationResult> RestoreAsync(Guid id, CancellationToken ct = default);
    Task<SermonOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed partial class CreateSermonRequestValidator : AbstractValidator<CreateSermonRequest>
{
    [GeneratedRegex(@"^[A-Za-z0-9_-]{6,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex YouTubeIdRegex();

    public CreateSermonRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.YouTubeVideoId).NotEmpty().MaximumLength(20)
            .Matches(YouTubeIdRegex()).WithMessage("YouTube video ID must be 6–20 alphanumeric or '_-' characters.");
        RuleFor(x => x.SpeakerNameFreeText).MaximumLength(200);
        RuleFor(x => x.YouTubeChannelId).MaximumLength(50);
        RuleFor(x => x.ServiceType).IsInEnum();
    }
}

public sealed class UpdateSermonRequestValidator : AbstractValidator<UpdateSermonRequest>
{
    public UpdateSermonRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SpeakerNameFreeText).MaximumLength(200);
        RuleFor(x => x.ServiceType).IsInEnum();
    }
}

public sealed class SermonService : ISermonService
{
    public const string EntityType = nameof(Sermon);

    private readonly ISermonRepository _repo;
    private readonly ITagService _tags;
    private readonly IScriptureReferenceService _scriptureRefs;
    private readonly IDocumentService _documents;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreateSermonRequest> _createValidator;
    private readonly IValidator<UpdateSermonRequest> _updateValidator;

    public SermonService(
        ISermonRepository repo,
        ITagService tags,
        IScriptureReferenceService scriptureRefs,
        IDocumentService documents,
        IAuditLogger audit,
        IValidator<CreateSermonRequest> createValidator,
        IValidator<UpdateSermonRequest> updateValidator)
    {
        _repo = repo;
        _tags = tags;
        _scriptureRefs = scriptureRefs;
        _documents = documents;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public Task<PagedResult<SermonListItemDto>> ListAsync(SermonListQuery query, CancellationToken ct = default)
        => _repo.ListAsync(query, ct);

    public async Task<SermonDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var sermon = await _repo.GetByIdAsync(id, includeDeleted, ct).ConfigureAwait(false);
        if (sermon is null) return null;
        return await _repo.ToDetailAsync(sermon, ct).ConfigureAwait(false);
    }

    public async Task<PublicSermonDto?> GetPublicBySlugAsync(string slug, bool includeMembersOnly, CancellationToken ct = default)
    {
        var sermon = await _repo.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (sermon is null || !sermon.IsPublished) return null;
        if (sermon.IsMembersOnly && !includeMembersOnly) return null;
        return await _repo.ToPublicAsync(sermon, ct).ConfigureAwait(false);
    }

    public Task<PagedResult<SermonListItemDto>> ListPublicAsync(SermonListQuery query, bool includeMembersOnly, CancellationToken ct = default)
    {
        // Force published-only and members-only filter through SermonListQuery.
        var filtered = query with { PublishedOnly = true };
        return _repo.ListAsync(filtered, ct).ContinueWith(t =>
        {
            var page = t.Result;
            var items = page.Items
                .Where(i => includeMembersOnly || !i.IsMembersOnly)
                .ToList();
            return new PagedResult<SermonListItemDto>(items, items.Count, page.Page, page.PageSize);
        }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public Task<List<SermonsByBookCount>> CountByBookAsync(bool includeMembersOnly, CancellationToken ct = default)
        => _repo.CountByBookAsync(includeMembersOnly, ct);

    public Task<SermonsByDayResponse> ListPublicByDayAsync(SermonsByDayQuery query, bool includeMembersOnly, CancellationToken ct = default)
        => _repo.ListByDayAsync(query, includeMembersOnly, ct);

    public Task<YearsResponse> ListYearStatsAsync(bool includeMembersOnly, CancellationToken ct = default)
        => _repo.ListYearStatsAsync(includeMembersOnly, ct);

    public async Task<SermonOperationResult> CreateAsync(CreateSermonRequest request, CancellationToken ct = default)
    {
        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        if (await _repo.SlugExistsAsync(request.Slug, null, ct).ConfigureAwait(false))
            return new(false, new[] { $"Slug '{request.Slug}' is already in use." }, null);

        var existingByVideo = await _repo.GetByYouTubeVideoIdAsync(request.YouTubeVideoId, includeDeleted: false, ct).ConfigureAwait(false);
        if (existingByVideo is not null)
            return new(false, new[] { $"A sermon for YouTube video '{request.YouTubeVideoId}' already exists." }, null);

        if (request.AttachmentDocumentIds is { Count: > 0 }
            && !await _repo.AreAllPublicPdfsAsync(request.AttachmentDocumentIds, ct).ConfigureAwait(false))
        {
            return new(false, new[] { "Attachments must be public, non-deleted PDF documents." }, null);
        }

        var now = DateTimeOffset.UtcNow;
        var sermon = new Sermon
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            Title = request.Title,
            DescriptionJson = request.DescriptionJson,
            YouTubeVideoId = request.YouTubeVideoId,
            YouTubeChannelId = request.YouTubeChannelId,
            ThumbnailBlobUrl = request.ThumbnailBlobUrl,
            ThumbnailWebpBlobUrl = request.ThumbnailWebpBlobUrl,
            PublishedAt = request.PublishedAt,
            YouTubePublishedAt = request.YouTubePublishedAt,
            DurationSeconds = request.DurationSeconds,
            Transcript = request.Transcript,
            TranscriptSource = request.TranscriptSource,
            SpeakerLeaderId = request.SpeakerLeaderId,
            SpeakerNameFreeText = request.SpeakerNameFreeText,
            SermonSeriesId = request.SermonSeriesId,
            ServiceType = request.ServiceType,
            IsPublished = request.IsPublished,
            IsMembersOnly = request.IsMembersOnly,
            CreatedAt = now,
            ModifiedAt = now,
        };
        await _repo.AddAsync(sermon, ct).ConfigureAwait(false);

        await PersistTagsAsync(sermon.Id, request.Tags ?? [], ct).ConfigureAwait(false);
        await _repo.ReplaceAttachmentsAsync(sermon.Id, request.AttachmentDocumentIds ?? [], ct).ConfigureAwait(false);
        await _scriptureRefs.ReplaceAllAsync(EntityType, sermon.Id, request.ScriptureReferences ?? [], ct).ConfigureAwait(false);
        await _audit.WriteAsync("Sermon.Created", EntityType, sermon.Id.ToString(),
            details: new { sermon.Slug, sermon.Title, sermon.YouTubeVideoId, sermon.IsPublished },
            cancellationToken: ct).ConfigureAwait(false);

        return new(true, Array.Empty<string>(), await _repo.ToDetailAsync(sermon, ct).ConfigureAwait(false));
    }

    public async Task<SermonOperationResult> UpdateAsync(Guid id, UpdateSermonRequest request, CancellationToken ct = default)
    {
        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        var sermon = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (sermon is null) return new(false, new[] { "Sermon not found." }, null);

        if (!string.Equals(sermon.Slug, request.Slug, StringComparison.Ordinal)
            && await _repo.SlugExistsAsync(request.Slug, id, ct).ConfigureAwait(false))
        {
            return new(false, new[] { $"Slug '{request.Slug}' is already in use." }, null);
        }

        if (request.AttachmentDocumentIds is { Count: > 0 }
            && !await _repo.AreAllPublicPdfsAsync(request.AttachmentDocumentIds, ct).ConfigureAwait(false))
        {
            return new(false, new[] { "Attachments must be public, non-deleted PDF documents." }, null);
        }

        sermon.Slug = request.Slug;
        sermon.Title = request.Title;
        sermon.DescriptionJson = request.DescriptionJson;
        sermon.ThumbnailBlobUrl = request.ThumbnailBlobUrl;
        sermon.ThumbnailWebpBlobUrl = request.ThumbnailWebpBlobUrl;
        sermon.PublishedAt = request.PublishedAt;
        sermon.Transcript = request.Transcript;
        sermon.TranscriptSource = request.TranscriptSource;
        sermon.SpeakerLeaderId = request.SpeakerLeaderId;
        sermon.SpeakerNameFreeText = request.SpeakerNameFreeText;
        sermon.SermonSeriesId = request.SermonSeriesId;
        sermon.ServiceType = request.ServiceType;
        sermon.IsPublished = request.IsPublished;
        sermon.IsMembersOnly = request.IsMembersOnly;
        sermon.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(sermon, ct).ConfigureAwait(false);

        await PersistTagsAsync(sermon.Id, request.Tags ?? [], ct).ConfigureAwait(false);
        await _repo.ReplaceAttachmentsAsync(sermon.Id, request.AttachmentDocumentIds ?? [], ct).ConfigureAwait(false);
        await _scriptureRefs.ReplaceAllAsync(EntityType, sermon.Id, request.ScriptureReferences ?? [], ct).ConfigureAwait(false);
        await _audit.WriteAsync("Sermon.Updated", EntityType, id.ToString(),
            details: new { sermon.Slug, sermon.Title, sermon.IsPublished, sermon.IsMembersOnly },
            cancellationToken: ct).ConfigureAwait(false);

        return new(true, Array.Empty<string>(), await _repo.ToDetailAsync(sermon, ct).ConfigureAwait(false));
    }

    public async Task<SermonOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sermon = await _repo.GetByIdAsync(id, false, ct).ConfigureAwait(false);
        if (sermon is null) return new(false, new[] { "Sermon not found." }, null);
        sermon.IsDeleted = true;
        sermon.IsPublished = false;
        sermon.DeletedAt = DateTimeOffset.UtcNow;
        sermon.ModifiedAt = sermon.DeletedAt.Value;
        await _repo.UpdateAsync(sermon, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Sermon.SoftDeleted", EntityType, id.ToString(),
            details: new { sermon.Slug, sermon.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), await _repo.ToDetailAsync(sermon, ct).ConfigureAwait(false));
    }

    public async Task<SermonOperationResult> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var sermon = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (sermon is null) return new(false, new[] { "Sermon not found." }, null);
        if (await _repo.SlugExistsAsync(sermon.Slug, excludingId: id, ct).ConfigureAwait(false))
            return new(false, new[] { $"Cannot restore — another sermon uses slug '{sermon.Slug}'." }, null);
        sermon.IsDeleted = false;
        sermon.DeletedAt = null;
        sermon.ModifiedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(sermon, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Sermon.Restored", EntityType, id.ToString(),
            details: new { sermon.Slug, sermon.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), await _repo.ToDetailAsync(sermon, ct).ConfigureAwait(false));
    }

    public async Task<SermonOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sermon = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (sermon is null) return new(false, new[] { "Sermon not found." }, null);
        if (!sermon.IsDeleted)
            return new(false, new[] { "Soft-delete first, then hard-delete." }, null);

        await _scriptureRefs.DeleteAllForParentAsync(EntityType, id, ct).ConfigureAwait(false);
        await _repo.ReplaceTagsAsync(id, Array.Empty<Guid>(), ct).ConfigureAwait(false);
        await _repo.ReplaceAttachmentsAsync(id, Array.Empty<Guid>(), ct).ConfigureAwait(false);
        await _repo.HardDeleteAsync(id, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Sermon.HardDeleted", EntityType, id.ToString(),
            details: new { sermon.Slug, sermon.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), null);
    }

    /// <summary>
    /// Resolves and persists the tag list for a sermon. New names go through
    /// TagService.NormalizeAndUpsertAsync; existing IDs are validated by the
    /// repository's ReplaceTagsAsync. UsageCount maintained.
    /// </summary>
    private async Task PersistTagsAsync(Guid sermonId, IList<SermonTagInput> inputs, CancellationToken ct)
    {
        var resolvedIds = new List<Guid>(inputs.Count);
        foreach (var input in inputs)
        {
            if (input.Id is { } existing)
            {
                resolvedIds.Add(existing);
            }
            else
            {
                var tag = await _tags.NormalizeAndUpsertAsync(input.Name, ct).ConfigureAwait(false);
                resolvedIds.Add(tag.Id);
            }
        }
        await _repo.ReplaceTagsAsync(sermonId, resolvedIds.Distinct(), ct).ConfigureAwait(false);
    }
}
