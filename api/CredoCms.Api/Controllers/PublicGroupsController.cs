using CredoCms.Application.Caching;
using CredoCms.Application.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Public-facing groups endpoints. Visibility (Public vs MembersOnly vs Hidden)
/// and roster gating are enforced at the service layer; this controller simply
/// passes through. The list response is cached for 1 minute and tagged
/// <see cref="OutputCacheTags.Groups"/>; group create/edit/delete invalidates
/// the tag through <see cref="IOutputCacheInvalidator"/>.
/// </summary>
[ApiController]
[Route("api/public/groups")]
public sealed class PublicGroupsController : ControllerBase
{
    private readonly IGroupService _groups;
    public PublicGroupsController(IGroupService groups) => _groups = groups;

    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "MembersAuthVary", Duration = 60,
        Tags = new[] { OutputCacheTags.Groups })]
    public Task<List<PublicGroupListItemDto>> ListAsync(CancellationToken ct)
        => _groups.ListPublicAsync(ct);

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "MembersAuthVary", Duration = 60,
        Tags = new[] { OutputCacheTags.Groups })]
    public async Task<ActionResult<PublicGroupDetailDto>> GetAsync(string slug, CancellationToken ct)
    {
        var detail = await _groups.GetPublicBySlugAsync(slug, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("{slug}/request-join")]
    [Authorize]
    public async Task<IActionResult> RequestJoinAsync(string slug, [FromBody] JoinRequestRequest request, CancellationToken ct)
    {
        var result = await _groups.SubmitJoinRequestAsync(slug, request, ct);
        // 200 with a placeholder body (the public route doesn't expose admin
        // membership shape). Errors surface through the standard envelope.
        return result.Succeeded ? Ok(new { requested = true }) : BadRequest(new { errors = result.Errors });
    }
}
