using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

public interface IAdminNotificationLastSentRepository
{
    Task<AdminNotificationLastSent?> GetAsync(
        Guid userId,
        AdminNotificationCategory category,
        CancellationToken ct = default);

    /// <summary>Insert if absent, update <c>LastSentAt</c> if present.</summary>
    Task UpsertAsync(AdminNotificationLastSent record, CancellationToken ct = default);
}
