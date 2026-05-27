using System.Security.Claims;
using CredoCms.Application.Pages;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/pages")]
public sealed class PublicPagesController : ControllerBase
{
    private readonly IPageService _pages;

    public PublicPagesController(IPageService pages) => _pages = pages;

    [HttpGet]
    public Task<List<PublicPageDto>> ListAsync(CancellationToken ct)
        => _pages.ListPublicAsync(IsAuthenticatedMember(), ct);

    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicPageDto>> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var page = await _pages.GetPublicBySlugAsync(slug, IsAuthenticatedMember(), ct);
        return page is null ? NotFound() : Ok(page);
    }

    /// <summary>
    /// Members and Editors and Administrators all see members-only content
    /// (anyone authenticated counts as "logged-in member" for content gating).
    /// </summary>
    private bool IsAuthenticatedMember()
    {
        if (User?.Identity?.IsAuthenticated != true) return false;
        return User.IsInRole(SystemConstants.Roles.Member)
            || User.IsInRole(SystemConstants.Roles.Editor)
            || User.IsInRole(SystemConstants.Roles.Administrator);
    }
}
