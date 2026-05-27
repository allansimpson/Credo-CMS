using CredoCms.Application.Common;
using CredoCms.Application.News;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/news")]
public sealed class PublicNewsController : ControllerBase
{
    private readonly INewsService _news;

    public PublicNewsController(INewsService news) => _news = news;

    [HttpGet]
    public Task<PagedResult<PublicNewsItemDto>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        => _news.ListPublicAsync(IsAuthenticatedMember(), page, pageSize, ct);

    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicNewsDetailDto>> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var item = await _news.GetPublicBySlugAsync(slug, IsAuthenticatedMember(), ct);
        return item is null ? NotFound() : Ok(item);
    }

    private bool IsAuthenticatedMember()
    {
        if (User?.Identity?.IsAuthenticated != true) return false;
        return User.IsInRole(SystemConstants.Roles.Member)
            || User.IsInRole(SystemConstants.Roles.Editor)
            || User.IsInRole(SystemConstants.Roles.Administrator);
    }
}
