using CredoCms.Application.Common;
using CredoCms.Domain.Blog;

namespace CredoCms.Application.Blog;

public interface IBlogRepository
{
    Task<BlogPost?> GetAsync(Guid id, CancellationToken ct = default);
    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken ct = default);

    Task<PagedResult<BlogPost>> ListAdminAsync(BlogListQuery query, CancellationToken ct = default);

    /// <summary>
    /// Public list — published, non-future-dated, non-members-only by default,
    /// pinned-first, then PublishedAt desc. <paramref name="includeMembersOnly"/>
    /// is true when the caller is authenticated.
    /// </summary>
    Task<PagedResult<BlogPost>> ListPublicAsync(
        string? category,
        bool includeMembersOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<List<BlogPost>> ListByAuthorAsync(Guid authorUserId, bool publicOnly, CancellationToken ct = default);

    Task AddAsync(BlogPost post, IReadOnlyCollection<Guid> tagIds, CancellationToken ct = default);
    Task UpdateAsync(BlogPost post, IReadOnlyCollection<Guid> tagIds, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default);

    Task<List<Guid>> GetTagIdsAsync(Guid blogPostId, CancellationToken ct = default);
}
