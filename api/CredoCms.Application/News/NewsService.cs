using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Application.Pages;
using CredoCms.Application.Search;
using CredoCms.Domain.News;
using FluentValidation;

namespace CredoCms.Application.News;

public sealed class NewsService : INewsService
{
    private readonly INewsRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly ISearchIndexer? _search;
    private readonly IValidator<CreateNewsItemRequest> _createValidator;
    private readonly IValidator<UpdateNewsItemRequest> _updateValidator;
    private readonly IEmailOnPublishService? _emailOnPublish;

    public NewsService(
        INewsRepository repo,
        IAuditLogger audit,
        IValidator<CreateNewsItemRequest> createValidator,
        IValidator<UpdateNewsItemRequest> updateValidator,
        ISearchIndexer? search = null,
        IEmailOnPublishService? emailOnPublish = null)
    {
        _repo = repo;
        _audit = audit;
        _search = search;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _emailOnPublish = emailOnPublish;
    }

    private async Task IndexAsync(NewsItem n, CancellationToken ct)
    {
        if (_search is null) return;
        await _search.UpsertAsync(new SearchUpsertCommand(
            EntityType: nameof(NewsItem), EntityId: n.Id,
            Title: n.Title,
            BodyText: PageService.AutoExcerpt(n.BodyJson, 8000) + " " + (n.Excerpt ?? ""),
            Url: "/news/" + n.Slug,
            IsPublished: n.IsPublished, IsMembersOnly: n.IsMembersOnly), ct).ConfigureAwait(false);
    }

    /// <summary>Phase 5: when an item transitions to published with the
    /// SendEmailOnPublish flag set, queue an EmailBroadcast and clear the
    /// flag inside the same transaction so a re-publish does not re-fire
    /// unless the editor explicitly re-enables it.</summary>
    private async Task TriggerEmailOnPublishAsync(NewsItem item, bool wasPublished, CancellationToken ct)
    {
        if (_emailOnPublish is null) return;
        if (!item.SendEmailOnPublish || !item.IsPublished) return;
        if (wasPublished) return; // Only fire on the false → true transition.
        var broadcastId = await _emailOnPublish.OnNewsPublishedAsync(item, ct).ConfigureAwait(false);
        if (broadcastId is null) return;
        item.SendEmailOnPublish = false;
        await _repo.UpdateAsync(item, ct).ConfigureAwait(false);
    }

    public Task<PagedResult<NewsListItemDto>> ListAsync(NewsListQuery query, CancellationToken ct = default)
        => _repo.ListAsync(query, ct);

    public async Task<NewsDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, includeDeleted, ct).ConfigureAwait(false);
        return item is null ? null : ToDetail(item);
    }

    public async Task<PublicNewsDetailDto?> GetPublicBySlugAsync(string slug, bool includeMembersOnly, CancellationToken ct = default)
    {
        var item = await _repo.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (item is null || !item.IsPublished) return null;
        if (item.IsMembersOnly && !includeMembersOnly) return null;
        if (item.ExpiresAt is not null && item.ExpiresAt <= DateTimeOffset.UtcNow) return null;

        return new PublicNewsDetailDto(
            item.Id, item.Slug, item.Title, item.BodyJson, item.Excerpt,
            item.HeroImageUrl, item.HeroImageWebpUrl, item.HeroImageAlt, item.MetaDescription,
            item.IsMembersOnly, item.PublishedAt ?? item.ModifiedAt, item.CalendarDate);
    }

    public Task<PagedResult<PublicNewsItemDto>> ListPublicAsync(
        bool includeMembersOnly, int page, int pageSize, CancellationToken ct = default)
        => _repo.ListPublicAsync(includeMembersOnly, DateTimeOffset.UtcNow, page, pageSize, ct);

    public async Task<NewsOperationResult> CreateAsync(CreateNewsItemRequest request, CancellationToken ct = default)
    {
        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid)
            return new NewsOperationResult(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        if (await _repo.SlugExistsAsync(request.Slug, null, ct).ConfigureAwait(false))
            return new NewsOperationResult(false, new[] { $"Slug '{request.Slug}' is already in use." }, null);

        var now = DateTimeOffset.UtcNow;
        var item = new NewsItem
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            Title = request.Title,
            BodyJson = request.BodyJson,
            Excerpt = request.Excerpt ?? PageService.AutoExcerpt(request.BodyJson),
            HeroImageUrl = request.HeroImageUrl,
            HeroImageWebpUrl = request.HeroImageWebpUrl,
            HeroImageAlt = request.HeroImageAlt,
            MetaDescription = request.MetaDescription,
            IsPublished = request.IsPublished,
            IsMembersOnly = request.IsMembersOnly,
            ExpiresAt = request.ExpiresAt,
            CalendarDate = request.CalendarDate,
            ScheduledPublishAt = request.ScheduledPublishAt,
            SendEmailOnPublish = request.SendEmailOnPublish,
            CreatedAt = now,
            ModifiedAt = now,
            PublishedAt = request.IsPublished ? now : null,
        };
        await _repo.AddAsync(item, ct).ConfigureAwait(false);
        await IndexAsync(item, ct).ConfigureAwait(false);
        await TriggerEmailOnPublishAsync(item, wasPublished: false, ct).ConfigureAwait(false);
        await _audit.WriteAsync("News.Created", nameof(NewsItem), item.Id.ToString(),
            details: new { item.Slug, item.Title, item.IsPublished, item.IsMembersOnly },
            cancellationToken: ct).ConfigureAwait(false);

        return new NewsOperationResult(true, Array.Empty<string>(), ToDetail(item));
    }

    public async Task<NewsOperationResult> UpdateAsync(Guid id, UpdateNewsItemRequest request, CancellationToken ct = default)
    {
        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid)
            return new NewsOperationResult(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        var item = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (item is null) return new NewsOperationResult(false, new[] { "News item not found." }, null);

        if (!string.Equals(item.Slug, request.Slug, StringComparison.Ordinal)
            && await _repo.SlugExistsAsync(request.Slug, id, ct).ConfigureAwait(false))
        {
            return new NewsOperationResult(false, new[] { $"Slug '{request.Slug}' is already in use." }, null);
        }

        var wasPublished = item.IsPublished;
        item.Slug = request.Slug;
        item.Title = request.Title;
        item.BodyJson = request.BodyJson;
        item.Excerpt = request.Excerpt ?? PageService.AutoExcerpt(request.BodyJson);
        item.HeroImageUrl = request.HeroImageUrl;
        item.HeroImageWebpUrl = request.HeroImageWebpUrl;
        item.HeroImageAlt = request.HeroImageAlt;
        item.MetaDescription = request.MetaDescription;
        item.IsPublished = request.IsPublished;
        item.IsMembersOnly = request.IsMembersOnly;
        item.ExpiresAt = request.ExpiresAt;
        item.CalendarDate = request.CalendarDate;
        item.ScheduledPublishAt = request.ScheduledPublishAt;
        item.SendEmailOnPublish = request.SendEmailOnPublish;
        item.ModifiedAt = DateTimeOffset.UtcNow;
        if (request.IsPublished && item.PublishedAt is null) item.PublishedAt = item.ModifiedAt;

        await _repo.UpdateAsync(item, ct).ConfigureAwait(false);
        await IndexAsync(item, ct).ConfigureAwait(false);
        await TriggerEmailOnPublishAsync(item, wasPublished, ct).ConfigureAwait(false);
        await _audit.WriteAsync("News.Updated", nameof(NewsItem), id.ToString(),
            details: new { item.Slug, item.Title, item.IsPublished, item.IsMembersOnly },
            cancellationToken: ct).ConfigureAwait(false);

        return new NewsOperationResult(true, Array.Empty<string>(), ToDetail(item));
    }

    public async Task<NewsOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, false, ct).ConfigureAwait(false);
        if (item is null) return new NewsOperationResult(false, new[] { "News item not found." }, null);
        item.IsDeleted = true;
        item.IsPublished = false;
        item.DeletedAt = DateTimeOffset.UtcNow;
        item.ModifiedAt = item.DeletedAt.Value;
        await _repo.UpdateAsync(item, ct).ConfigureAwait(false);
        if (_search is not null)
            await _search.SetPublishedAsync(nameof(NewsItem), id, false, ct).ConfigureAwait(false);
        await _audit.WriteAsync("News.SoftDeleted", nameof(NewsItem), id.ToString(),
            details: new { item.Slug, item.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new NewsOperationResult(true, Array.Empty<string>(), ToDetail(item));
    }

    public async Task<NewsOperationResult> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (item is null) return new NewsOperationResult(false, new[] { "News item not found." }, null);

        if (await _repo.SlugExistsAsync(item.Slug, excludingId: id, ct).ConfigureAwait(false))
            return new NewsOperationResult(false,
                new[] { $"Cannot restore — another item already uses slug '{item.Slug}'." }, null);

        item.IsDeleted = false;
        item.DeletedAt = null;
        item.ModifiedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(item, ct).ConfigureAwait(false);
        await _audit.WriteAsync("News.Restored", nameof(NewsItem), id.ToString(),
            details: new { item.Slug, item.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new NewsOperationResult(true, Array.Empty<string>(), ToDetail(item));
    }

    public async Task<NewsOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (item is null) return new NewsOperationResult(false, new[] { "News item not found." }, null);
        if (!item.IsDeleted)
            return new NewsOperationResult(false,
                new[] { "Soft-delete the item first, then hard-delete from the deleted tab." }, null);

        await _repo.HardDeleteAsync(id, ct).ConfigureAwait(false);
        if (_search is not null)
            await _search.RemoveAsync(nameof(NewsItem), id, ct).ConfigureAwait(false);
        await _audit.WriteAsync("News.HardDeleted", nameof(NewsItem), id.ToString(),
            details: new { item.Slug, item.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new NewsOperationResult(true, Array.Empty<string>(), null);
    }

    internal static NewsDetailDto ToDetail(NewsItem n) => new(
        n.Id, n.Slug, n.Title, n.BodyJson, n.Excerpt,
        n.HeroImageUrl, n.HeroImageWebpUrl, n.HeroImageAlt, n.MetaDescription,
        n.IsPublished, n.IsMembersOnly, n.IsDeleted,
        n.ExpiresAt, n.CalendarDate,
        n.CreatedAt, n.ModifiedAt, n.ModifiedByUserId,
        n.PublishedAt, n.DeletedAt);
}
