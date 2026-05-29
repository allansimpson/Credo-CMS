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

    /// <summary>Admin preview — returns the public DTO shape for any
    /// non-deleted page, regardless of published / members-only state. Used
    /// by the page editor's "Preview" button so drafts can be previewed in
    /// the actual public renderer.</summary>
    Task<PublicPageDto?> GetPreviewBySlugAsync(string slug, CancellationToken ct = default);

    Task<List<PublicPageDto>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default);

    Task<PageOperationResult> CreateAsync(CreatePageRequest request, CancellationToken ct = default);

    Task<PageOperationResult> UpdateAsync(Guid id, UpdatePageRequest request, CancellationToken ct = default);

    /// <summary>Promote the page to published. If a draft exists, draft fields
    /// are copied onto the live columns; otherwise just flips IsPublished.</summary>
    Task<PageOperationResult> PublishAsync(Guid id, CancellationToken ct = default);

    /// <summary>Clear the Draft* columns without affecting live content.</summary>
    Task<PageOperationResult> DiscardDraftAsync(Guid id, CancellationToken ct = default);

    /// <summary>Take a published page back to draft state without altering
    /// content.</summary>
    Task<PageOperationResult> UnpublishAsync(Guid id, CancellationToken ct = default);

    Task<PageOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<PageOperationResult> RestoreAsync(Guid id, CancellationToken ct = default);

    Task<PageOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default);
}
