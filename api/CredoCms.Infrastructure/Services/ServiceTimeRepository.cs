using CredoCms.Application.Services;
using CredoCms.Domain.Services;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Services;

public sealed class ServiceTimeRepository : IServiceTimeRepository
{
    private readonly ApplicationDbContext _db;
    public ServiceTimeRepository(ApplicationDbContext db) => _db = db;

    public Task<List<ServiceTime>> ListAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.ServiceTimes.IgnoreQueryFilters() : _db.ServiceTimes;
        return q
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.DisplayOrder)
            .ThenBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public Task<ServiceTime?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.ServiceTimes.IgnoreQueryFilters() : _db.ServiceTimes;
        return q.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<PublicServiceTimeDto>> ListPublicAsync(CancellationToken ct = default)
    {
        return await _db.ServiceTimes
            .Where(s => s.IsActive)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new PublicServiceTimeDto(
                s.Name, s.DayOfWeek, s.StartTime, s.EndTime, s.Location, s.Notes, s.DisplayOrder))
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(ServiceTime item, CancellationToken ct = default)
    {
        _db.ServiceTimes.Add(item);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ServiceTime item, CancellationToken ct = default)
    {
        _db.ServiceTimes.Update(item);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _db.ServiceTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        if (item is null) return false;
        _db.ServiceTimes.Remove(item);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
