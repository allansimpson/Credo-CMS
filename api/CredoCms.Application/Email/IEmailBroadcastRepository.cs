using CredoCms.Application.Common;
using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

public interface IEmailBroadcastRepository
{
    Task<EmailBroadcast?> GetAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<EmailBroadcast>> ListAsync(
        BroadcastStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Returns broadcasts whose <c>Status=Scheduled</c> and
    /// <c>ScheduledSendAt &lt;= now</c>. Used by the broadcast worker.</summary>
    Task<List<EmailBroadcast>> ListDueScheduledAsync(DateTimeOffset now, CancellationToken ct = default);

    /// <summary>Returns broadcasts in <c>Sending</c> state. Used on worker
    /// startup to resume any in-flight sends.</summary>
    Task<List<EmailBroadcast>> ListInFlightAsync(CancellationToken ct = default);

    Task AddAsync(EmailBroadcast broadcast, CancellationToken ct = default);
    Task UpdateAsync(EmailBroadcast broadcast, CancellationToken ct = default);

    /// <summary>Atomic counter increment driven by webhook events. Returns
    /// the updated broadcast for SignalR emission.</summary>
    Task<EmailBroadcast?> IncrementStatsAsync(
        Guid broadcastId,
        int deliveredDelta,
        int bouncedDelta,
        int complaintDelta,
        int openDelta,
        CancellationToken ct = default);
}
