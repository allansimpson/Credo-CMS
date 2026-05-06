using CredoCms.Application.Common;

namespace CredoCms.Application.Blog;

/// <summary>
/// Blog service. Permission rules:
///   create / edit / delete    Editor + Administrator
///   read public list/detail   anyone (members get the members-only set
///                             included)
///   read admin list/detail    Editor + Administrator
/// Reading time is recomputed on every save from the body word count.
/// Excerpt is auto-generated from body text when the caller leaves it null.
/// </summary>
public interface IBlogService
{
    // ---- public reads -----------------------------------------------------

    Task<PagedResult<BlogPostListItemDto>> ListPublicAsync(
        string? category, int page, int pageSize, CancellationToken ct = default);

    Task<BlogPostDetailDto?> GetPublicBySlugAsync(string slug, CancellationToken ct = default);

    Task<List<BlogPostListItemDto>> ListPublicByAuthorAsync(Guid authorUserId, CancellationToken ct = default);

    // ---- admin reads ------------------------------------------------------

    Task<PagedResult<BlogPostListItemDto>> ListAdminAsync(BlogListQuery query, CancellationToken ct = default);
    Task<BlogPostDetailDto?> GetAdminAsync(Guid id, CancellationToken ct = default);

    // ---- admin writes -----------------------------------------------------

    Task<BlogMutationResult> CreateAsync(CreateBlogPostRequest request, CancellationToken ct = default);
    Task<BlogMutationResult> UpdateAsync(Guid id, UpdateBlogPostRequest request, CancellationToken ct = default);
    Task<BlogMutationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
