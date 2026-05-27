using CredoCms.Application.Classes;
using CredoCms.Domain.Classes;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Classes;

public sealed class ClassSlotRepository : IClassSlotRepository
{
    private readonly ApplicationDbContext _db;
    public ClassSlotRepository(ApplicationDbContext db) => _db = db;

    public Task<ClassSlot?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.ClassSlots.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<ClassSlot?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _db.ClassSlots.FirstOrDefaultAsync(s => s.Slug == slug, ct);

    public async Task<List<ClassSlot>> ListAdminAsync(string? search, bool includeInactive, CancellationToken ct = default)
    {
        var q = _db.ClassSlots.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => EF.Functions.Like(x.Name, $"%{s}%")
                || EF.Functions.Like(x.Slug, $"%{s}%")
                || EF.Functions.Like(x.AudienceAgeGroup, $"%{s}%"));
        }
        if (!includeInactive) q = q.Where(x => x.IsActive);
        return await q.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<ClassSlot>> ListPublicAsync(CancellationToken ct = default) =>
        await _db.ClassSlots.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            .ToListAsync(ct).ConfigureAwait(false);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken ct = default)
    {
        var q = _db.ClassSlots.Where(s => s.Slug == slug);
        if (excludeId is { } id) q = q.Where(s => s.Id != id);
        return q.AnyAsync(ct);
    }

    public async Task AddAsync(ClassSlot entity, CancellationToken ct = default)
    {
        _db.ClassSlots.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ClassSlot entity, CancellationToken ct = default)
    {
        _db.ClassSlots.Update(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default)
    {
        var entity = await _db.ClassSlots.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = byUserId;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task<int> CountOfferingsAsync(Guid slotId, CancellationToken ct = default) =>
        _db.ClassOfferings.CountAsync(o => o.ClassSlotId == slotId, ct);
}

public sealed class ClassOfferingRepository : IClassOfferingRepository
{
    private readonly ApplicationDbContext _db;
    public ClassOfferingRepository(ApplicationDbContext db) => _db = db;

    public Task<ClassOffering?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.ClassOfferings.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<List<ClassOffering>> ListForSlotAsync(Guid slotId, CancellationToken ct = default) =>
        await _db.ClassOfferings.AsNoTracking()
            .Where(o => o.ClassSlotId == slotId)
            .OrderByDescending(o => o.StartDate)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<List<ClassOffering>> ListAdminAsync(AdminClassOfferingsQuery query, CancellationToken ct = default)
    {
        var q = _db.ClassOfferings.AsNoTracking().AsQueryable();
        if (query.ClassSlotId is { } slotId) q = q.Where(o => o.ClassSlotId == slotId);
        if (query.FromDate is { } from) q = q.Where(o => o.EndDate >= from);
        if (query.ToDate is { } to) q = q.Where(o => o.StartDate <= to);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        q = query.Status switch
        {
            OfferingStatusFilter.Current => q.Where(o => o.StartDate <= today && o.EndDate >= today),
            OfferingStatusFilter.Upcoming => q.Where(o => o.StartDate > today),
            OfferingStatusFilter.Past => q.Where(o => o.EndDate < today),
            _ => q,
        };

        return await q.OrderByDescending(o => o.StartDate).ToListAsync(ct).ConfigureAwait(false);
    }

    public Task<ClassOffering?> GetCurrentForSlotAsync(Guid slotId, DateOnly today, CancellationToken ct = default) =>
        _db.ClassOfferings.AsNoTracking()
            .Where(o => o.ClassSlotId == slotId && o.StartDate <= today && o.EndDate >= today)
            .OrderBy(o => o.StartDate)
            .FirstOrDefaultAsync(ct);

    public Task<ClassOffering?> GetUpcomingForSlotAsync(Guid slotId, DateOnly today, CancellationToken ct = default) =>
        _db.ClassOfferings.AsNoTracking()
            .Where(o => o.ClassSlotId == slotId && o.StartDate > today)
            .OrderBy(o => o.StartDate)
            .FirstOrDefaultAsync(ct);

    public Task<ClassOffering?> GetRecentPastForSlotAsync(Guid slotId, DateOnly today, int lookbackDays, CancellationToken ct = default)
    {
        var cutoff = today.AddDays(-Math.Max(0, lookbackDays));
        return _db.ClassOfferings.AsNoTracking()
            .Where(o => o.ClassSlotId == slotId && o.EndDate < today && o.EndDate >= cutoff)
            .OrderByDescending(o => o.EndDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(ClassOffering entity, CancellationToken ct = default)
    {
        _db.ClassOfferings.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ClassOffering entity, CancellationToken ct = default)
    {
        _db.ClassOfferings.Update(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default)
    {
        var entity = await _db.ClassOfferings.FirstOrDefaultAsync(o => o.Id == id, ct).ConfigureAwait(false);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = byUserId;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
