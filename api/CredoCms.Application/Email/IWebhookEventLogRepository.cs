using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

public interface IWebhookEventLogRepository
{
    /// <summary>True if this <c>sg_event_id</c> has already been processed.</summary>
    Task<bool> ExistsAsync(string eventId, CancellationToken ct = default);

    Task AddAsync(WebhookEventLog log, CancellationToken ct = default);
}
