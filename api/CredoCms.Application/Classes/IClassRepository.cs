using CredoCms.Domain.Classes;

namespace CredoCms.Application.Classes;

public interface IClassSlotRepository
{
    Task<ClassSlot?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ClassSlot?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<ClassSlot>> ListAdminAsync(string? search, bool includeInactive, CancellationToken ct = default);

    /// <summary>Active slots ordered by DisplayOrder then Name. Used by the
    /// public landing page; the service layer joins offerings on top.</summary>
    Task<List<ClassSlot>> ListPublicAsync(CancellationToken ct = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken ct = default);
    Task AddAsync(ClassSlot entity, CancellationToken ct = default);
    Task UpdateAsync(ClassSlot entity, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default);

    Task<int> CountOfferingsAsync(Guid slotId, CancellationToken ct = default);
}

public interface IClassOfferingRepository
{
    Task<ClassOffering?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<ClassOffering>> ListForSlotAsync(Guid slotId, CancellationToken ct = default);
    Task<List<ClassOffering>> ListAdminAsync(AdminClassOfferingsQuery query, CancellationToken ct = default);

    /// <summary>The "current" offering for a slot — earliest StartDate that
    /// covers <paramref name="today"/> (StartDate &lt;= today &lt;= EndDate),
    /// or null if no offering covers the date.</summary>
    Task<ClassOffering?> GetCurrentForSlotAsync(Guid slotId, DateOnly today, CancellationToken ct = default);

    /// <summary>The next-upcoming offering for a slot — earliest StartDate
    /// that is strictly after <paramref name="today"/>, or null.</summary>
    Task<ClassOffering?> GetUpcomingForSlotAsync(Guid slotId, DateOnly today, CancellationToken ct = default);

    /// <summary>The most recently ended offering for a slot, ended on or
    /// before <paramref name="today"/> and within <paramref name="lookbackDays"/>.
    /// Null if outside the lookback window.</summary>
    Task<ClassOffering?> GetRecentPastForSlotAsync(Guid slotId, DateOnly today, int lookbackDays, CancellationToken ct = default);

    Task AddAsync(ClassOffering entity, CancellationToken ct = default);
    Task UpdateAsync(ClassOffering entity, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default);
}
