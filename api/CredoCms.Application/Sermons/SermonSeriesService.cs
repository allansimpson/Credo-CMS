using System.Globalization;
using System.Text.Json;
using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using CredoCms.Application.Pages;
using CredoCms.Application.Scripture;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Bible;
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
    string? Context,
    string? ScopeLabel,
    int? PlannedParts,
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
    string? Context,
    string? ScopeLabel,
    int? PlannedParts,
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

/// <summary>
/// Drives the /sermons/series public page. Adds counts, status, the
/// flagship "latest sermon" pointer, and the derived strings (plain-text
/// description, computed scope label) the page needs without leaking
/// every sermon row down the wire.
/// </summary>
public sealed record PublicSermonSeriesWithStatsDto(
    Guid Id,
    string Slug,
    string Title,
    string? BannerImageUrl,
    string? BannerImageWebpUrl,
    string? BannerImageAlt,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<ScriptureReferenceDto> ScriptureReferences,
    /// <summary>Truncated plain-text snippet derived from
    /// <c>DescriptionJson</c> server-side. <c>""</c> when no body.</summary>
    string Description,
    /// <summary>Teaching-track context, e.g. "AM Worship". Defaults to
    /// the first entry of <c>SiteSettings.SermonContextsJson</c> when the
    /// series has no override.</summary>
    string Context,
    /// <summary>Compact scope label. Editor-set when present; otherwise
    /// derived from the series' <see cref="ScriptureReferences"/>; falls
    /// back to "Various".</summary>
    string ScopeLabel,
    int SermonCount,
    int? PlannedParts,
    PublicSermonSeriesLatestSermonDto? LatestSermon,
    /// <summary>"active" when <c>EndDate is null</c>, else "complete".</summary>
    string Status);

public sealed record PublicSermonSeriesLatestSermonDto(
    string Slug,
    string Title,
    DateTimeOffset PublishedAt,
    /// <summary>Pre-formatted "MMM dd" string for the hero eyebrow.</summary>
    string DateLabel);

/// <summary>Aggregated sermon stats per series, projected by the repo
/// in a single round-trip. The service composes the full
/// <see cref="PublicSermonSeriesWithStatsDto"/> on top.</summary>
public sealed record SermonSeriesPublicStatsRow(
    Guid SeriesId,
    int SermonCount,
    string? LatestSermonSlug,
    string? LatestSermonTitle,
    DateTimeOffset? LatestSermonPublishedAt);

public sealed record CreateSermonSeriesRequest(
    string Slug,
    string Title,
    string? DescriptionJson,
    string? BannerImageUrl,
    string? BannerImageWebpUrl,
    string? BannerImageAlt,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Context,
    string? ScopeLabel,
    int? PlannedParts,
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
    string? Context,
    string? ScopeLabel,
    int? PlannedParts,
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

    /// <summary>Distinct non-null Context values currently in use across
    /// non-deleted series. Drives the SiteSettings removal-protection so
    /// admins can't drop a context still pinned to a series.</summary>
    Task<List<string>> GetUsedContextsAsync(CancellationToken ct = default);

    /// <summary>One row per series with the public sermon count and the
    /// latest-published sermon's slug/title/date. Series with zero
    /// published sermons still appear with <c>SermonCount = 0</c> and
    /// null latest-* fields.</summary>
    Task<List<SermonSeriesPublicStatsRow>> GetPublicStatsAsync(CancellationToken ct = default);

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

    /// <summary>Public DTO list with per-series stats, derived
    /// description, derived scope label, and the resolved Context (falling
    /// back to the first entry of <c>SiteSettings.SermonContextsJson</c>).
    /// Drives the by-series public page hero + archive.</summary>
    Task<List<PublicSermonSeriesWithStatsDto>> ListPublicWithStatsAsync(CancellationToken ct = default);

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
        RuleFor(x => x.Context).MaximumLength(100);
        RuleFor(x => x.ScopeLabel).MaximumLength(120);
        RuleFor(x => x.PlannedParts).InclusiveBetween(1, 200)
            .When(x => x.PlannedParts is not null);
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
        RuleFor(x => x.Context).MaximumLength(100);
        RuleFor(x => x.ScopeLabel).MaximumLength(120);
        RuleFor(x => x.PlannedParts).InclusiveBetween(1, 200)
            .When(x => x.PlannedParts is not null);
        RuleFor(x => x.EndDate).Must((req, end) => end is null || end >= req.StartDate)
            .WithMessage("End date must be ≥ start date.");
    }
}

public sealed class SermonSeriesService : ISermonSeriesService
{
    public const string EntityType = nameof(SermonSeries);
    private const string FallbackContext = "AM Worship";
    private const int DescriptionMaxLength = 280;

    private readonly ISermonSeriesRepository _repo;
    private readonly IScriptureReferenceService _scriptureRefs;
    private readonly ISiteSettingsRepository _settings;
    private readonly IOutputCacheInvalidator _cache;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreateSermonSeriesRequest> _createValidator;
    private readonly IValidator<UpdateSermonSeriesRequest> _updateValidator;

    public SermonSeriesService(
        ISermonSeriesRepository repo,
        IScriptureReferenceService scriptureRefs,
        ISiteSettingsRepository settings,
        IOutputCacheInvalidator cache,
        IAuditLogger audit,
        IValidator<CreateSermonSeriesRequest> createValidator,
        IValidator<UpdateSermonSeriesRequest> updateValidator)
    {
        _repo = repo;
        _scriptureRefs = scriptureRefs;
        _settings = settings;
        _cache = cache;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private async Task InvalidateSermonSeriesCacheAsync(CancellationToken ct)
    {
        await _cache.InvalidateAsync(OutputCacheTags.SermonSeries, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Sermons, ct).ConfigureAwait(false);
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

    public async Task<List<PublicSermonSeriesWithStatsDto>> ListPublicWithStatsAsync(CancellationToken ct = default)
    {
        // Three round-trips: the series rows, the per-series aggregated
        // sermon stats, and a single SiteSettings read for the fallback
        // context. Refs come per-series (N tiny queries) — series N is
        // small for any one church, not worth a join.
        var listQuery = new SermonSeriesListQuery(IncludeDeleted: false, Page: 1, PageSize: 500);
        var page = await _repo.ListAsync(listQuery, ct).ConfigureAwait(false);
        var stats = await _repo.GetPublicStatsAsync(ct).ConfigureAwait(false);
        var settings = await _settings.GetAsync(ct).ConfigureAwait(false);

        var fallbackContext = ResolveFallbackContext(settings.SermonContextsJson);
        var statsLookup = stats.ToDictionary(x => x.SeriesId);

        var result = new List<PublicSermonSeriesWithStatsDto>(page.Items.Count);
        foreach (var s in page.Items)
        {
            var refs = await _scriptureRefs.ListForParentAsync(EntityType, s.Id, ct).ConfigureAwait(false);

            // The list projection drops DescriptionJson — fetch the entity
            // when we'll actually use it. Series count is small; per-row
            // round-trip is acceptable here.
            var entity = await _repo.GetByIdAsync(s.Id, includeDeleted: false, ct).ConfigureAwait(false);
            var description = ProseMirrorText.Excerpt(entity?.DescriptionJson, DescriptionMaxLength);

            var contextValue = string.IsNullOrWhiteSpace(s.Context) ? fallbackContext : s.Context!;
            var scopeLabel = !string.IsNullOrWhiteSpace(s.ScopeLabel)
                ? s.ScopeLabel!
                : DeriveScopeLabel(refs);

            statsLookup.TryGetValue(s.Id, out var row);
            var sermonCount = row?.SermonCount ?? 0;
            PublicSermonSeriesLatestSermonDto? latest = null;
            if (row is not null && row.LatestSermonSlug is not null && row.LatestSermonTitle is not null && row.LatestSermonPublishedAt is { } published)
            {
                latest = new PublicSermonSeriesLatestSermonDto(
                    Slug: row.LatestSermonSlug,
                    Title: row.LatestSermonTitle,
                    PublishedAt: published,
                    DateLabel: published.ToString("MMM dd", CultureInfo.InvariantCulture));
            }

            result.Add(new PublicSermonSeriesWithStatsDto(
                Id: s.Id,
                Slug: s.Slug,
                Title: s.Title,
                BannerImageUrl: s.BannerImageUrl,
                BannerImageWebpUrl: s.BannerImageWebpUrl,
                BannerImageAlt: s.BannerImageAlt,
                StartDate: s.StartDate,
                EndDate: s.EndDate,
                ScriptureReferences: refs,
                Description: description,
                Context: contextValue,
                ScopeLabel: scopeLabel,
                SermonCount: sermonCount,
                PlannedParts: s.PlannedParts,
                LatestSermon: latest,
                Status: s.EndDate is null ? "active" : "complete"));
        }
        return result;
    }

    private static string ResolveFallbackContext(string sermonContextsJson)
    {
        if (string.IsNullOrWhiteSpace(sermonContextsJson)) return FallbackContext;
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(sermonContextsJson);
            return list is { Count: > 0 } && !string.IsNullOrWhiteSpace(list[0])
                ? list[0]
                : FallbackContext;
        }
        catch
        {
            return FallbackContext;
        }
    }

    /// <summary>
    /// Compact scope label derived from the series-level scripture refs.
    /// Single-book series → book name (with a chapter range when the
    /// single ref spans multiple chapters: "Hebrews", "Luke 14–15"). Refs
    /// touching multiple books, or none at all, fall back to "Various".
    /// </summary>
    internal static string DeriveScopeLabel(IReadOnlyList<ScriptureReferenceDto> refs)
    {
        if (refs.Count == 0) return "Various";

        var distinctBooks = refs.Select(r => r.Book).Distinct().ToList();
        if (distinctBooks.Count != 1) return "Various";

        var book = distinctBooks[0];
        var bookName = BibleBooks.Get(book).Name;

        // Only attempt a chapter range when there's exactly one ref — past
        // that, joining multiple ranges with commas reads worse than the
        // bare book name on the eyebrow.
        if (refs.Count == 1)
        {
            var r = refs[0];
            var endChapter = r.ChapterEnd ?? r.ChapterStart;
            if (endChapter > r.ChapterStart)
            {
                return $"{bookName} {r.ChapterStart}–{endChapter}";
            }
        }
        return bookName;
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
            Context = string.IsNullOrWhiteSpace(request.Context) ? null : request.Context.Trim(),
            ScopeLabel = string.IsNullOrWhiteSpace(request.ScopeLabel) ? null : request.ScopeLabel.Trim(),
            PlannedParts = request.PlannedParts,
            CreatedAt = now,
            ModifiedAt = now,
        };
        await _repo.AddAsync(series, ct).ConfigureAwait(false);
        await _scriptureRefs.ReplaceAllAsync(EntityType, series.Id, request.ScriptureReferences, ct).ConfigureAwait(false);
        await InvalidateSermonSeriesCacheAsync(ct).ConfigureAwait(false);
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
        series.Context = string.IsNullOrWhiteSpace(request.Context) ? null : request.Context.Trim();
        series.ScopeLabel = string.IsNullOrWhiteSpace(request.ScopeLabel) ? null : request.ScopeLabel.Trim();
        series.PlannedParts = request.PlannedParts;
        series.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(series, ct).ConfigureAwait(false);
        await _scriptureRefs.ReplaceAllAsync(EntityType, id, request.ScriptureReferences, ct).ConfigureAwait(false);
        await InvalidateSermonSeriesCacheAsync(ct).ConfigureAwait(false);
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
        await InvalidateSermonSeriesCacheAsync(ct).ConfigureAwait(false);
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
        await InvalidateSermonSeriesCacheAsync(ct).ConfigureAwait(false);
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
        await InvalidateSermonSeriesCacheAsync(ct).ConfigureAwait(false);
        await _audit.WriteAsync("SermonSeries.HardDeleted", EntityType, id.ToString(),
            details: new { series.Slug, series.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), null);
    }

    private static SermonSeriesDetailDto ToDetail(SermonSeries s, IReadOnlyList<ScriptureReferenceDto> refs) => new(
        s.Id, s.Slug, s.Title, s.DescriptionJson,
        s.BannerImageUrl, s.BannerImageWebpUrl, s.BannerImageAlt,
        s.StartDate, s.EndDate,
        s.Context, s.ScopeLabel, s.PlannedParts,
        s.IsDeleted, refs,
        s.CreatedAt, s.ModifiedAt, s.ModifiedByUserId, s.DeletedAt);

    private static PublicSermonSeriesDto ToPublic(SermonSeries s, IReadOnlyList<ScriptureReferenceDto> refs) => new(
        s.Id, s.Slug, s.Title, s.DescriptionJson,
        s.BannerImageUrl, s.BannerImageWebpUrl, s.BannerImageAlt,
        s.StartDate, s.EndDate, refs);
}
