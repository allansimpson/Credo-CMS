using CredoCms.Application.Common;
using CredoCms.Domain.Pages;

namespace CredoCms.Application.Pages;

/// <summary>
/// Materialized data access for Pages. Returns concrete domain entities or
/// DTO projections; never <c>IQueryable</c> (per the Application↔Infrastructure
/// rule documented in IMPLEMENTATION_NOTES).
/// </summary>
public interface IPageRepository
{
    Task<PagedResult<PageListItemDto>> ListAsync(PageListQuery query, CancellationToken ct = default);

    Task<Page?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);

    Task<Page?> GetBySlugAsync(string slug, CancellationToken ct = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default);

    Task<List<PublicPageDto>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default);

    Task AddAsync(Page page, CancellationToken ct = default);

    Task UpdateAsync(Page page, CancellationToken ct = default);

    Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default);
}
