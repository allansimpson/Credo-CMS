using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Email;

public sealed class EmailBroadcastRepository : IEmailBroadcastRepository
{
    private readonly ApplicationDbContext _db;
    public EmailBroadcastRepository(ApplicationDbContext db) => _db = db;

    public Task<EmailBroadcast?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.EmailBroadcasts.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<PagedResult<EmailBroadcast>> ListAsync(
        BroadcastStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.EmailBroadcasts.AsNoTracking().AsQueryable();
        if (status is { } s) q = q.Where(b => b.Status == s);
        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);
        return new PagedResult<EmailBroadcast>(items, total, page, pageSize);
    }

    public Task<List<EmailBroadcast>> ListDueScheduledAsync(DateTimeOffset now, CancellationToken ct = default) =>
        _db.EmailBroadcasts
            .Where(b => b.Status == BroadcastStatus.Scheduled && b.ScheduledSendAt <= now)
            .ToListAsync(ct);

    public Task<List<EmailBroadcast>> ListInFlightAsync(CancellationToken ct = default) =>
        _db.EmailBroadcasts.Where(b => b.Status == BroadcastStatus.Sending).ToListAsync(ct);

    public async Task AddAsync(EmailBroadcast broadcast, CancellationToken ct = default)
    {
        _db.EmailBroadcasts.Add(broadcast);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EmailBroadcast broadcast, CancellationToken ct = default)
    {
        _db.EmailBroadcasts.Update(broadcast);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<EmailBroadcast?> IncrementStatsAsync(
        Guid broadcastId,
        int deliveredDelta, int bouncedDelta, int complaintDelta, int openDelta,
        CancellationToken ct = default)
    {
        var b = await _db.EmailBroadcasts.FirstOrDefaultAsync(x => x.Id == broadcastId, ct).ConfigureAwait(false);
        if (b is null) return null;
        b.DeliveredCount += deliveredDelta;
        b.BouncedCount += bouncedDelta;
        b.ComplaintCount += complaintDelta;
        b.OpenCount += openDelta;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return b;
    }
}
