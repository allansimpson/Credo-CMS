using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Email;

public sealed class AdminNotificationLastSentRepository : IAdminNotificationLastSentRepository
{
    private readonly ApplicationDbContext _db;
    public AdminNotificationLastSentRepository(ApplicationDbContext db) => _db = db;

    public Task<AdminNotificationLastSent?> GetAsync(Guid userId, AdminNotificationCategory category, CancellationToken ct = default) =>
        _db.AdminNotificationLastSent
            .FirstOrDefaultAsync(x => x.UserId == userId && x.NotificationCategory == category, ct);

    public async Task UpsertAsync(AdminNotificationLastSent record, CancellationToken ct = default)
    {
        var existing = await _db.AdminNotificationLastSent
            .FirstOrDefaultAsync(x => x.UserId == record.UserId && x.NotificationCategory == record.NotificationCategory, ct)
            .ConfigureAwait(false);
        if (existing is null)
        {
            _db.AdminNotificationLastSent.Add(record);
        }
        else
        {
            existing.LastSentAt = record.LastSentAt;
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
