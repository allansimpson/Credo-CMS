using CredoCms.Application.Common;

namespace CredoCms.Application.News;

public sealed record NewsOperationResult(bool Succeeded, string[] Errors, NewsDetailDto? Item);

public interface INewsService
{
    Task<PagedResult<NewsListItemDto>> ListAsync(NewsListQuery query, CancellationToken ct = default);

    Task<NewsDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);

    Task<PublicNewsDetailDto?> GetPublicBySlugAsync(string slug, bool includeMembersOnly, CancellationToken ct = default);

    Task<PagedResult<PublicNewsItemDto>> ListPublicAsync(
        bool includeMembersOnly, int page, int pageSize, CancellationToken ct = default);

    Task<NewsOperationResult> CreateAsync(CreateNewsItemRequest request, CancellationToken ct = default);

    Task<NewsOperationResult> UpdateAsync(Guid id, UpdateNewsItemRequest request, CancellationToken ct = default);

    Task<NewsOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<NewsOperationResult> RestoreAsync(Guid id, CancellationToken ct = default);

    Task<NewsOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default);
}
