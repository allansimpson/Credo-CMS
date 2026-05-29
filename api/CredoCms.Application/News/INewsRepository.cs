using CredoCms.Application.Common;
using CredoCms.Domain.News;

namespace CredoCms.Application.News;

public interface INewsRepository
{
    Task<PagedResult<NewsListItemDto>> ListAsync(NewsListQuery query, CancellationToken ct = default);

    Task<NewsItem?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);

    Task<NewsItem?> GetBySlugAsync(string slug, CancellationToken ct = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default);

    Task<PagedResult<PublicNewsItemDto>> ListPublicAsync(
        bool includeMembersOnly,
        DateTimeOffset nowUtc,
        int page,
        int pageSize,
        string? category = null,
        CancellationToken ct = default);

    /// <summary>Distinct category names currently used by non-deleted news
    /// items. Powers usage-protection on Site Settings.</summary>
    Task<List<string>> GetUsedCategoriesAsync(CancellationToken ct = default);

    Task AddAsync(NewsItem item, CancellationToken ct = default);

    Task UpdateAsync(NewsItem item, CancellationToken ct = default);

    Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default);
}
