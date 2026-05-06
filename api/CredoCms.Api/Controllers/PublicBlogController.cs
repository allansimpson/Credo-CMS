using CredoCms.Application.Blog;
using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Public-facing blog endpoints. Members-only posts are filtered server-side
/// based on the caller's authentication state, so the cache key has to vary
/// on auth — the existing <c>MembersAuthVary</c> policy handles that.
/// </summary>
[ApiController]
[Route("api/public/blog")]
public sealed class PublicBlogController : ControllerBase
{
    private readonly IBlogService _blog;
    public PublicBlogController(IBlogService blog) => _blog = blog;

    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "MembersAuthVary", Duration = 120,
        Tags = new[] { OutputCacheTags.Blog })]
    public Task<PagedResult<BlogPostListItemDto>> ListAsync(
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
        => _blog.ListPublicAsync(category, page, pageSize, ct);

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "MembersAuthVary", Duration = 120,
        Tags = new[] { OutputCacheTags.Blog })]
    public async Task<ActionResult<BlogPostDetailDto>> GetAsync(string slug, CancellationToken ct)
    {
        var post = await _blog.GetPublicBySlugAsync(slug, ct);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpGet("authors/{userId:guid}")]
    [AllowAnonymous]
    public Task<List<BlogPostListItemDto>> ListByAuthorAsync(Guid userId, CancellationToken ct)
        => _blog.ListPublicByAuthorAsync(userId, ct);
}
