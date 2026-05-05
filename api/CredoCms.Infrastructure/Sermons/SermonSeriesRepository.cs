using CredoCms.Application.Common;
using CredoCms.Application.Sermons;
using CredoCms.Domain.Sermons;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Sermons;

public sealed class SermonSeriesRepository : ISermonSeriesRepository
{
    private readonly ApplicationDbContext _db;
    public SermonSeriesRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<SermonSeriesListItemDto>> ListAsync(SermonSeriesListQuery query, CancellationToken ct = default)
    {
        IQueryable<SermonSeries> q = query.IncludeDeleted
            ? _db.SermonSeries.IgnoreQueryFilters()
            : _db.SermonSeries;
        if (query.IncludeDeleted) q = q.Where(s => s.IsDeleted);

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var items = await q
            .OrderByDescending(s => s.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SermonSeriesListItemDto(
                s.Id, s.Slug, s.Title,
                s.BannerImageUrl, s.BannerImageWebpUrl, s.BannerImageAlt,
                s.StartDate, s.EndDate, s.IsDeleted, s.ModifiedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<SermonSeriesListItemDto>(items, total, page, pageSize);
    }

    public Task<SermonSeries?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.SermonSeries.IgnoreQueryFilters() : _db.SermonSeries;
        return q.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public Task<SermonSeries?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _db.SermonSeries.FirstOrDefaultAsync(s => s.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default)
    {
        var q = _db.SermonSeries.AsQueryable();
        if (excludingId is not null) q = q.Where(s => s.Id != excludingId);
        return q.AnyAsync(s => s.Slug == slug, ct);
    }

    public async Task<List<PublicSermonSeriesDto>> ListPublicAsync(CancellationToken ct = default)
    {
        return await _db.SermonSeries
            .OrderByDescending(s => s.StartDate)
            .Select(s => new PublicSermonSeriesDto(
                s.Id, s.Slug, s.Title, s.DescriptionJson,
                s.BannerImageUrl, s.BannerImageWebpUrl, s.BannerImageAlt,
                s.StartDate, s.EndDate,
                new List<Application.Scripture.ScriptureReferenceDto>()))
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(SermonSeries series, CancellationToken ct = default)
    {
        _db.SermonSeries.Add(series);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(SermonSeries series, CancellationToken ct = default)
    {
        _db.SermonSeries.Update(series);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _db.SermonSeries.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (s is null) return false;
        _db.SermonSeries.Remove(s);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
