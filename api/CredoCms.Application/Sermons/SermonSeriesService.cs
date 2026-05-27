using CredoCms.Application.Common;
using CredoCms.Application.Pages;
using CredoCms.Application.Scripture;
using CredoCms.Domain.Sermons;
using FluentValidation;

namespace CredoCms.Application.Sermons;

public sealed record SermonSeriesListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string? BannerImageUrl,
    string? BannerImageWebpUrl,
    string? BannerImageAlt,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsDeleted,
    DateTimeOffset ModifiedAt);

public sealed record SermonSeriesDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string? DescriptionJson,
    string? BannerImageUrl,
    string? BannerImageWebpUrl,
    string? BannerImageAlt,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsDeleted,
    IReadOnlyList<ScriptureReferenceDto> ScriptureReferences,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    Guid? ModifiedByUserId,
    DateTimeOffset? DeletedAt);

public sealed record PublicSermonSeriesDto(
    Guid Id,
    string Slug,
    string Title,
    string? DescriptionJson,
    string? BannerImageUrl,
    string? BannerImageWebpUrl,
    string? BannerImageAlt,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<ScriptureReferenceDto> ScriptureReferences);

public sealed record CreateSermonSeriesRequest(
    string Slug,
    string Title,
    string? DescriptionJson,
    string? BannerImageUrl,
    string? BannerImageWebpUrl,
    string? BannerImageAlt,
    DateOnly StartDate,
    DateOnly? EndDate,
    IList<ScriptureReferenceInput> ScriptureReferences);

public sealed record UpdateSermonSeriesRequest(
    string Slug,
    string Title,
    string? DescriptionJson,
    string? BannerImageUrl,
    string? BannerImageWebpUrl,
    string? BannerImageAlt,
    DateOnly StartDate,
    DateOnly? EndDate,
    IList<ScriptureReferenceInput> ScriptureReferences);

public sealed record SermonSeriesListQuery(
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 25);

public interface ISermonSeriesRepository
{
    Task<PagedResult<SermonSeriesListItemDto>> ListAsync(SermonSeriesListQuery query, CancellationToken ct = default);
    Task<SermonSeries?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
    Task<SermonSeries?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default);
    Task<List<PublicSermonSeriesDto>> ListPublicAsync(CancellationToken ct = default);
    Task AddAsync(SermonSeries series, CancellationToken ct = default);
    Task UpdateAsync(SermonSeries series, CancellationToken ct = default);
    Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed record SermonSeriesOperationResult(bool Succeeded, string[] Errors, SermonSeriesDetailDto? Series);

public interface ISermonSeriesService
{
    Task<PagedResult<SermonSeriesListItemDto>> ListAsync(SermonSeriesListQuery query, CancellationToken ct = default);
    Task<SermonSeriesDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
    Task<PublicSermonSeriesDto?> GetPublicBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<PublicSermonSeriesDto>> ListPublicAsync(CancellationToken ct = default);
    Task<SermonSeriesOperationResult> CreateAsync(CreateSermonSeriesRequest request, CancellationToken ct = default);
    Task<SermonSeriesOperationResult> UpdateAsync(Guid id, UpdateSermonSeriesRequest request, CancellationToken ct = default);
    Task<SermonSeriesOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<SermonSeriesOperationResult> RestoreAsync(Guid id, CancellationToken ct = default);
    Task<SermonSeriesOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class CreateSermonSeriesRequestValidator : AbstractValidator<CreateSermonSeriesRequest>
{
    public CreateSermonSeriesRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BannerImageUrl).MaximumLength(2000);
        RuleFor(x => x.BannerImageWebpUrl).MaximumLength(2000);
        RuleFor(x => x.BannerImageAlt).MaximumLength(500);
        RuleFor(x => x.EndDate).Must((req, end) => end is null || end >= req.StartDate)
            .WithMessage("End date must be ≥ start date.");
    }
}

public sealed class UpdateSermonSeriesRequestValidator : AbstractValidator<UpdateSermonSeriesRequest>
{
    public UpdateSermonSeriesRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BannerImageUrl).MaximumLength(2000);
        RuleFor(x => x.BannerImageWebpUrl).MaximumLength(2000);
        RuleFor(x => x.BannerImageAlt).MaximumLength(500);
        RuleFor(x => x.EndDate).Must((req, end) => end is null || end >= req.StartDate)
            .WithMessage("End date must be ≥ start date.");
    }
}

public sealed class SermonSeriesService : ISermonSeriesService
{
    public const string EntityType = nameof(SermonSeries);

    private readonly ISermonSeriesRepository _repo;
    private readonly IScriptureReferenceService _scriptureRefs;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreateSermonSeriesRequest> _createValidator;
    private readonly IValidator<UpdateSermonSeriesRequest> _updateValidator;

    public SermonSeriesService(
        ISermonSeriesRepository repo,
        IScriptureReferenceService scriptureRefs,
        IAuditLogger audit,
        IValidator<CreateSermonSeriesRequest> createValidator,
        IValidator<UpdateSermonSeriesRequest> updateValidator)
    {
        _repo = repo;
        _scriptureRefs = scriptureRefs;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public Task<PagedResult<SermonSeriesListItemDto>> ListAsync(SermonSeriesListQuery query, CancellationToken ct = default)
        => _repo.ListAsync(query, ct);

    public async Task<SermonSeriesDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var series = await _repo.GetByIdAsync(id, includeDeleted, ct).ConfigureAwait(false);
        if (series is null) return null;
        var refs = await _scriptureRefs.ListForParentAsync(EntityType, id, ct).ConfigureAwait(false);
        return ToDetail(series, refs);
    }

    public async Task<PublicSermonSeriesDto?> GetPublicBySlugAsync(string slug, CancellationToken ct = default)
    {
        var series = await _repo.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (series is null) return null;
        var refs = await _scriptureRefs.ListForParentAsync(EntityType, series.Id, ct).ConfigureAwait(false);
        return ToPublic(series, refs);
    }

    public async Task<List<PublicSermonSeriesDto>> ListPublicAsync(CancellationToken ct = default)
    {
        var rows = await _repo.ListPublicAsync(ct).ConfigureAwait(false);
        // Hydrate with scripture refs (one query per series; small N).
        var hydrated = new List<PublicSermonSeriesDto>(rows.Count);
        foreach (var row in rows)
        {
            var refs = await _scriptureRefs.ListForParentAsync(EntityType, row.Id, ct).ConfigureAwait(false);
            hydrated.Add(row with { ScriptureReferences = refs });
        }
        return hydrated;
    }

    public async Task<SermonSeriesOperationResult> CreateAsync(CreateSermonSeriesRequest request, CancellationToken ct = default)
    {
        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        if (await _repo.SlugExistsAsync(request.Slug, null, ct).ConfigureAwait(false))
            return new(false, new[] { $"Slug '{request.Slug}' is already in use." }, null);

        var now = DateTimeOffset.UtcNow;
        var series = new SermonSeries
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            Title = request.Title,
            DescriptionJson = request.DescriptionJson,
            BannerImageUrl = request.BannerImageUrl,
            BannerImageWebpUrl = request.BannerImageWebpUrl,
            BannerImageAlt = request.BannerImageAlt,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = now,
            ModifiedAt = now,
        };
        await _repo.AddAsync(series, ct).ConfigureAwait(false);
        await _scriptureRefs.ReplaceAllAsync(EntityType, series.Id, request.ScriptureReferences, ct).ConfigureAwait(false);
        await _audit.WriteAsync("SermonSeries.Created", EntityType, series.Id.ToString(),
            details: new { series.Slug, series.Title }, cancellationToken: ct).ConfigureAwait(false);

        var refs = await _scriptureRefs.ListForParentAsync(EntityType, series.Id, ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDetail(series, refs));
    }

    public async Task<SermonSeriesOperationResult> UpdateAsync(Guid id, UpdateSermonSeriesRequest request, CancellationToken ct = default)
    {
        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        var series = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (series is null) return new(false, new[] { "Sermon series not found." }, null);

        if (!string.Equals(series.Slug, request.Slug, StringComparison.Ordinal)
            && await _repo.SlugExistsAsync(request.Slug, id, ct).ConfigureAwait(false))
        {
            return new(false, new[] { $"Slug '{request.Slug}' is already in use." }, null);
        }

        series.Slug = request.Slug;
        series.Title = request.Title;
        series.DescriptionJson = request.DescriptionJson;
        series.BannerImageUrl = request.BannerImageUrl;
        series.BannerImageWebpUrl = request.BannerImageWebpUrl;
        series.BannerImageAlt = request.BannerImageAlt;
        series.StartDate = request.StartDate;
        series.EndDate = request.EndDate;
        series.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(series, ct).ConfigureAwait(false);
        await _scriptureRefs.ReplaceAllAsync(EntityType, id, request.ScriptureReferences, ct).ConfigureAwait(false);
        await _audit.WriteAsync("SermonSeries.Updated", EntityType, id.ToString(),
            details: new { series.Slug, series.Title }, cancellationToken: ct).ConfigureAwait(false);

        var refs = await _scriptureRefs.ListForParentAsync(EntityType, id, ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDetail(series, refs));
    }

    public async Task<SermonSeriesOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var series = await _repo.GetByIdAsync(id, false, ct).ConfigureAwait(false);
        if (series is null) return new(false, new[] { "Sermon series not found." }, null);
        series.IsDeleted = true;
        series.DeletedAt = DateTimeOffset.UtcNow;
        series.ModifiedAt = series.DeletedAt.Value;
        await _repo.UpdateAsync(series, ct).ConfigureAwait(false);
        await _audit.WriteAsync("SermonSeries.SoftDeleted", EntityType, id.ToString(),
            details: new { series.Slug, series.Title }, cancellationToken: ct).ConfigureAwait(false);
        var refs = await _scriptureRefs.ListForParentAsync(EntityType, id, ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDetail(series, refs));
    }

    public async Task<SermonSeriesOperationResult> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var series = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (series is null) return new(false, new[] { "Sermon series not found." }, null);
        if (await _repo.SlugExistsAsync(series.Slug, excludingId: id, ct).ConfigureAwait(false))
            return new(false, new[] { $"Cannot restore — another series uses slug '{series.Slug}'." }, null);
        series.IsDeleted = false;
        series.DeletedAt = null;
        series.ModifiedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(series, ct).ConfigureAwait(false);
        await _audit.WriteAsync("SermonSeries.Restored", EntityType, id.ToString(),
            details: new { series.Slug, series.Title }, cancellationToken: ct).ConfigureAwait(false);
        var refs = await _scriptureRefs.ListForParentAsync(EntityType, id, ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDetail(series, refs));
    }

    public async Task<SermonSeriesOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var series = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (series is null) return new(false, new[] { "Sermon series not found." }, null);
        if (!series.IsDeleted)
            return new(false, new[] { "Soft-delete first, then hard-delete." }, null);
        await _scriptureRefs.DeleteAllForParentAsync(EntityType, id, ct).ConfigureAwait(false);
        await _repo.HardDeleteAsync(id, ct).ConfigureAwait(false);
        await _audit.WriteAsync("SermonSeries.HardDeleted", EntityType, id.ToString(),
            details: new { series.Slug, series.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), null);
    }

    private static SermonSeriesDetailDto ToDetail(SermonSeries s, IReadOnlyList<ScriptureReferenceDto> refs) => new(
        s.Id, s.Slug, s.Title, s.DescriptionJson,
        s.BannerImageUrl, s.BannerImageWebpUrl, s.BannerImageAlt,
        s.StartDate, s.EndDate, s.IsDeleted, refs,
        s.CreatedAt, s.ModifiedAt, s.ModifiedByUserId, s.DeletedAt);

    private static PublicSermonSeriesDto ToPublic(SermonSeries s, IReadOnlyList<ScriptureReferenceDto> refs) => new(
        s.Id, s.Slug, s.Title, s.DescriptionJson,
        s.BannerImageUrl, s.BannerImageWebpUrl, s.BannerImageAlt,
        s.StartDate, s.EndDate, refs);
}
