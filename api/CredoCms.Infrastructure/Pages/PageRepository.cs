using CredoCms.Application.Common;
using CredoCms.Application.Pages;
using CredoCms.Domain.Pages;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Pages;

public sealed class PageRepository : IPageRepository
{
    private readonly ApplicationDbContext _db;

    public PageRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<PageListItemDto>> ListAsync(PageListQuery query, CancellationToken ct = default)
    {
        IQueryable<Page> q = query.IncludeDeleted ? _db.Pages.IgnoreQueryFilters() : _db.Pages;
        if (query.IncludeDeleted)
        {
            // When viewing the deleted tab specifically, show only soft-deleted rows.
            q = q.Where(p => p.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(p => EF.Functions.Like(p.Title, $"%{s}%") || EF.Functions.Like(p.Slug, $"%{s}%"));
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var items = await q
            .OrderByDescending(p => p.ModifiedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PageListItemDto(
                p.Id, p.Slug, p.Title, p.Excerpt, p.IsPublished, p.IsMembersOnly,
                p.IsSystemPage, p.ModifiedAt, p.ModifiedByUserId))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<PageListItemDto>(items, total, page, pageSize);
    }

    public Task<Page?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.Pages.IgnoreQueryFilters() : _db.Pages;
        return q.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<Page?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Pages.FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default)
    {
        var q = _db.Pages.AsQueryable();
        if (excludingId is not null) q = q.Where(p => p.Id != excludingId);
        return q.AnyAsync(p => p.Slug == slug, ct);
    }

    public async Task<List<PublicPageDto>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default)
    {
        var q = _db.Pages.Where(p => p.IsPublished);
        if (!includeMembersOnly) q = q.Where(p => !p.IsMembersOnly);

        return await q
            .OrderBy(p => p.Title)
            .Select(p => new PublicPageDto(
                p.Id, p.Slug, p.Title, p.BodyJson, p.Excerpt,
                p.HeroImageUrl, p.HeroImageWebpUrl, p.HeroImageAlt, p.MetaDescription,
                p.IsMembersOnly, p.PublishedAt ?? p.ModifiedAt))
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(Page page, CancellationToken ct = default)
    {
        _db.Pages.Add(page);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Page page, CancellationToken ct = default)
    {
        _db.Pages.Update(page);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var page = await _db.Pages.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
        if (page is null) return false;
        _db.Pages.Remove(page);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
