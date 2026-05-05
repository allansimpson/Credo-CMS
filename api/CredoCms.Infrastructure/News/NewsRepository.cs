using CredoCms.Application.Common;
using CredoCms.Application.News;
using CredoCms.Domain.News;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.News;

public sealed class NewsRepository : INewsRepository
{
    private readonly ApplicationDbContext _db;

    public NewsRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<NewsListItemDto>> ListAsync(NewsListQuery query, CancellationToken ct = default)
    {
        IQueryable<NewsItem> q = query.IncludeDeleted ? _db.News.IgnoreQueryFilters() : _db.News;
        if (query.IncludeDeleted) q = q.Where(n => n.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(n => EF.Functions.Like(n.Title, $"%{s}%") || EF.Functions.Like(n.Slug, $"%{s}%"));
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var items = await q
            .OrderByDescending(n => n.PublishedAt ?? n.ModifiedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NewsListItemDto(
                n.Id, n.Slug, n.Title, n.Excerpt, n.IsPublished, n.IsMembersOnly,
                n.PublishedAt, n.ExpiresAt, n.ModifiedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<NewsListItemDto>(items, total, page, pageSize);
    }

    public Task<NewsItem?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.News.IgnoreQueryFilters() : _db.News;
        return q.FirstOrDefaultAsync(n => n.Id == id, ct);
    }

    public Task<NewsItem?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _db.News.FirstOrDefaultAsync(n => n.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default)
    {
        var q = _db.News.AsQueryable();
        if (excludingId is not null) q = q.Where(n => n.Id != excludingId);
        return q.AnyAsync(n => n.Slug == slug, ct);
    }

    public async Task<PagedResult<PublicNewsItemDto>> ListPublicAsync(
        bool includeMembersOnly,
        DateTimeOffset nowUtc,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        IQueryable<NewsItem> q = _db.News.Where(n => n.IsPublished);
        q = q.Where(n => n.ExpiresAt == null || n.ExpiresAt > nowUtc);
        if (!includeMembersOnly) q = q.Where(n => !n.IsMembersOnly);

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var items = await q
            .OrderByDescending(n => n.PublishedAt ?? n.ModifiedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new PublicNewsItemDto(
                n.Id, n.Slug, n.Title, n.Excerpt,
                n.HeroImageUrl, n.HeroImageWebpUrl, n.HeroImageAlt,
                n.IsMembersOnly,
                (n.PublishedAt ?? n.ModifiedAt),
                n.CalendarDate))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<PublicNewsItemDto>(items, total, page, pageSize);
    }

    public async Task AddAsync(NewsItem item, CancellationToken ct = default)
    {
        _db.News.Add(item);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(NewsItem item, CancellationToken ct = default)
    {
        _db.News.Update(item);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _db.News.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);
        if (item is null) return false;
        _db.News.Remove(item);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
