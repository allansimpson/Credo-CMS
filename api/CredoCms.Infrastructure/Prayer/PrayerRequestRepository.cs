using CredoCms.Application.Prayer;
using CredoCms.Domain.Prayer;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Prayer;

public sealed class PrayerRequestRepository : IPrayerRequestRepository
{
    private readonly ApplicationDbContext _db;
    public PrayerRequestRepository(ApplicationDbContext db) => _db = db;

    public Task<PrayerRequest?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.PrayerRequests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<List<PrayerRequest>> ListMemberVisibleAsync(int archiveDays, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-Math.Max(0, archiveDays));
        // Active stays visible indefinitely; Answered is shown for `archiveDays`
        // after creation, then implicitly archived for the member view. The
        // status field stays Answered until an admin moves it to Archived.
        return await _db.PrayerRequests.AsNoTracking()
            .Where(r => r.Status != PrayerRequestStatus.Archived
                && (r.Status == PrayerRequestStatus.Active
                    || (r.Status == PrayerRequestStatus.Answered && r.CreatedAt >= cutoff)))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<PrayerRequest>> ListAdminAsync(AdminPrayerListQuery query, CancellationToken ct = default)
    {
        var q = _db.PrayerRequests.AsNoTracking().AsQueryable();
        if (query.Status is { } status) q = q.Where(r => r.Status == status);
        if (query.IsAnonymous is { } anon) q = q.Where(r => r.IsAnonymous == anon);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(r => EF.Functions.Like(r.Title, $"%{term}%"));
        }
        return await q.OrderByDescending(r => r.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(PrayerRequest request, CancellationToken ct = default)
    {
        _db.PrayerRequests.Add(request);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(PrayerRequest request, CancellationToken ct = default)
    {
        _db.PrayerRequests.Update(request);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default)
    {
        var entity = await _db.PrayerRequests.FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = byUserId;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task<PrayerRequestUpdate?> GetUpdateAsync(Guid id, CancellationToken ct = default) =>
        _db.PrayerRequestUpdates.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<List<PrayerRequestUpdate>> ListUpdatesForAsync(Guid prayerRequestId, CancellationToken ct = default) =>
        await _db.PrayerRequestUpdates.AsNoTracking()
            .Where(u => u.PrayerRequestId == prayerRequestId)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task AddUpdateAsync(PrayerRequestUpdate update, CancellationToken ct = default)
    {
        _db.PrayerRequestUpdates.Add(update);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SoftDeleteUpdateAsync(Guid id, Guid byUserId, CancellationToken ct = default)
    {
        var entity = await _db.PrayerRequestUpdates.FirstOrDefaultAsync(u => u.Id == id, ct).ConfigureAwait(false);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = byUserId;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task<int> PrayedForCountAsync(Guid prayerRequestId, CancellationToken ct = default) =>
        _db.PrayerRequestPrayedFor.CountAsync(p => p.PrayerRequestId == prayerRequestId, ct);

    public Task<bool> HasPrayedAsync(Guid prayerRequestId, Guid userId, CancellationToken ct = default) =>
        _db.PrayerRequestPrayedFor.AnyAsync(p => p.PrayerRequestId == prayerRequestId && p.UserId == userId, ct);

    public async Task<bool> AddPrayedForAsync(Guid prayerRequestId, Guid userId, CancellationToken ct = default)
    {
        // Idempotent: short-circuit if a row already exists. The unique index
        // on (PrayerRequestId, UserId) is the durable safeguard; this check
        // avoids the unnecessary INSERT round trip.
        if (await HasPrayedAsync(prayerRequestId, userId, ct).ConfigureAwait(false)) return false;
        _db.PrayerRequestPrayedFor.Add(new Domain.Prayer.PrayerRequestPrayedFor
        {
            Id = Guid.NewGuid(),
            PrayerRequestId = prayerRequestId,
            UserId = userId,
            PrayedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RemovePrayedForAsync(Guid prayerRequestId, Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.PrayerRequestPrayedFor
            .Where(p => p.PrayerRequestId == prayerRequestId && p.UserId == userId)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }
}
