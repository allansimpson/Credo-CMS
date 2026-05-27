using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Email;

public sealed class WebhookEventLogRepository : IWebhookEventLogRepository
{
    private readonly ApplicationDbContext _db;
    public WebhookEventLogRepository(ApplicationDbContext db) => _db = db;

    public Task<bool> ExistsAsync(string eventId, CancellationToken ct = default) =>
        _db.WebhookEventLog.AsNoTracking().AnyAsync(e => e.EventId == eventId, ct);

    public async Task AddAsync(WebhookEventLog log, CancellationToken ct = default)
    {
        _db.WebhookEventLog.Add(log);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
