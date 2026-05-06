using CredoCms.Application.Common;
using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

public interface IEmailBroadcastRecipientRepository
{
    Task<PagedResult<EmailBroadcastRecipient>> ListAsync(
        Guid broadcastId,
        RecipientStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Streamed for CSV export — no pagination, no buffering.</summary>
    IAsyncEnumerable<EmailBroadcastRecipient> StreamAsync(
        Guid broadcastId,
        RecipientStatus? status,
        CancellationToken ct = default);

    /// <summary>Bulk insert at send time. Set creates per-broadcast.</summary>
    Task BulkInsertAsync(IReadOnlyCollection<EmailBroadcastRecipient> recipients, CancellationToken ct = default);

    /// <summary>Lookup by <c>SendGridMessageId</c> for webhook event routing.</summary>
    Task<EmailBroadcastRecipient?> GetBySendGridMessageIdAsync(string messageId, CancellationToken ct = default);

    Task UpdateAsync(EmailBroadcastRecipient recipient, CancellationToken ct = default);

    /// <summary>Called from the user hard-delete flow. Nulls
    /// <c>UserId</c> while preserving the snapshot fields so the audit
    /// row remains meaningful.</summary>
    Task NullUserReferencesAsync(Guid userId, CancellationToken ct = default);
}
