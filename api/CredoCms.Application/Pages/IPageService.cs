using CredoCms.Application.Common;

namespace CredoCms.Application.Pages;

public sealed record PageOperationResult(bool Succeeded, string[] Errors, PageDetailDto? Page);

/// <summary>
/// High-level orchestration over the Page repository: validation, audit
/// logging, system-page guards, slug-collision checks. Controllers depend on
/// this interface, never on the repository directly.
/// </summary>
public interface IPageService
{
    Task<PagedResult<PageListItemDto>> ListAsync(PageListQuery query, CancellationToken ct = default);

    Task<PageDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);

    Task<PublicPageDto?> GetPublicBySlugAsync(string slug, bool includeMembersOnly, CancellationToken ct = default);

    Task<List<PublicPageDto>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default);

    Task<PageOperationResult> CreateAsync(CreatePageRequest request, CancellationToken ct = default);

    Task<PageOperationResult> UpdateAsync(Guid id, UpdatePageRequest request, CancellationToken ct = default);

    Task<PageOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<PageOperationResult> RestoreAsync(Guid id, CancellationToken ct = default);

    Task<PageOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default);
}
