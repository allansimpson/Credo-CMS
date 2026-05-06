using System.Runtime.CompilerServices;
using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Email;

public sealed class EmailBroadcastRecipientRepository : IEmailBroadcastRecipientRepository
{
    private readonly ApplicationDbContext _db;
    public EmailBroadcastRecipientRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<EmailBroadcastRecipient>> ListAsync(
        Guid broadcastId, RecipientStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.EmailBroadcastRecipients.AsNoTracking().Where(r => r.BroadcastId == broadcastId);
        if (status is { } s) q = q.Where(r => r.Status == s);
        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderBy(r => r.EmailAddressSnapshot)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);
        return new PagedResult<EmailBroadcastRecipient>(items, total, page, pageSize);
    }

    public async IAsyncEnumerable<EmailBroadcastRecipient> StreamAsync(
        Guid broadcastId, RecipientStatus? status, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var q = _db.EmailBroadcastRecipients.AsNoTracking().Where(r => r.BroadcastId == broadcastId);
        if (status is { } s) q = q.Where(r => r.Status == s);
        await foreach (var r in q.OrderBy(r => r.EmailAddressSnapshot).AsAsyncEnumerable().WithCancellation(ct))
            yield return r;
    }

    public async Task BulkInsertAsync(IReadOnlyCollection<EmailBroadcastRecipient> recipients, CancellationToken ct = default)
    {
        if (recipients.Count == 0) return;
        _db.EmailBroadcastRecipients.AddRange(recipients);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>SendGrid emits per-recipient <c>sg_message_id</c> values
    /// formatted <c>&lt;batch&gt;.&lt;suffix&gt;</c>; we stored only the
    /// <c>&lt;batch&gt;</c> prefix. Match by either exact or prefix.</summary>
    public Task<EmailBroadcastRecipient?> GetBySendGridMessageIdAsync(string messageId, CancellationToken ct = default)
    {
        var prefix = messageId.Split('.')[0];
        return _db.EmailBroadcastRecipients
            .FirstOrDefaultAsync(r => r.SendGridMessageId == messageId || r.SendGridMessageId == prefix, ct);
    }

    public async Task UpdateAsync(EmailBroadcastRecipient recipient, CancellationToken ct = default)
    {
        _db.EmailBroadcastRecipients.Update(recipient);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task NullUserReferencesAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.EmailBroadcastRecipients
            .Where(r => r.UserId == userId).ToListAsync(ct).ConfigureAwait(false);
        foreach (var r in rows) r.UserId = null;
        if (rows.Count > 0) await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
