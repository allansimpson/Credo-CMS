using CredoCms.Domain.Prayer;

namespace CredoCms.Application.Prayer;

public interface IPrayerRequestRepository
{
    Task<PrayerRequest?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Active + answered requests within the archive lookback window. Archived
    /// requests are excluded; soft-deleted rows are excluded by the query
    /// filter.
    /// </summary>
    Task<List<PrayerRequest>> ListMemberVisibleAsync(int archiveDays, CancellationToken ct = default);

    /// <summary>Full admin list with optional status / anonymous / text filters.</summary>
    Task<List<PrayerRequest>> ListAdminAsync(AdminPrayerListQuery query, CancellationToken ct = default);

    Task AddAsync(PrayerRequest request, CancellationToken ct = default);
    Task UpdateAsync(PrayerRequest request, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default);

    // ---- Updates ----------------------------------------------------------

    Task<PrayerRequestUpdate?> GetUpdateAsync(Guid id, CancellationToken ct = default);
    Task<List<PrayerRequestUpdate>> ListUpdatesForAsync(Guid prayerRequestId, CancellationToken ct = default);
    Task AddUpdateAsync(PrayerRequestUpdate update, CancellationToken ct = default);
    Task SoftDeleteUpdateAsync(Guid id, Guid byUserId, CancellationToken ct = default);

    // ---- "I prayed for this" toggle --------------------------------------

    Task<int> PrayedForCountAsync(Guid prayerRequestId, CancellationToken ct = default);
    Task<bool> HasPrayedAsync(Guid prayerRequestId, Guid userId, CancellationToken ct = default);

    /// <summary>Idempotent insert. Returns true if a new row was added,
    /// false if the user had already prayed for this request.</summary>
    Task<bool> AddPrayedForAsync(Guid prayerRequestId, Guid userId, CancellationToken ct = default);

    /// <summary>Idempotent delete. Returns true if a row was removed.</summary>
    Task<bool> RemovePrayedForAsync(Guid prayerRequestId, Guid userId, CancellationToken ct = default);
}
