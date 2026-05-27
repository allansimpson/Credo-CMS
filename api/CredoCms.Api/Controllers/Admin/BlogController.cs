using CredoCms.Application.Blog;
using CredoCms.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/blog")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class BlogController : ControllerBase
{
    private readonly IBlogService _blog;
    public BlogController(IBlogService blog) => _blog = blog;

    [HttpGet]
    public Task<PagedResult<BlogPostListItemDto>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] Guid? authorUserId,
        [FromQuery] bool? isPublished,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => _blog.ListAdminAsync(new BlogListQuery(search, category, authorUserId, isPublished, includeDeleted, page, pageSize), ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BlogPostDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var post = await _blog.GetAdminAsync(id, ct);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost]
    public async Task<ActionResult<BlogPostDetailDto>> CreateAsync([FromBody] CreateBlogPostRequest request, CancellationToken ct)
    {
        var result = await _blog.CreateAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Post!.Id }, result.Post)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BlogPostDetailDto>> UpdateAsync(
        Guid id, [FromBody] UpdateBlogPostRequest request, CancellationToken ct)
    {
        var result = await _blog.UpdateAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Post) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _blog.SoftDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
