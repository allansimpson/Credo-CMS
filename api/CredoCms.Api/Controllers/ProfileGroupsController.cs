using CredoCms.Application.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

/// <summary>
/// "My memberships" surface for the authenticated user. The service resolves
/// the caller from <see cref="Application.Common.ICurrentUserService"/>; the
/// controller does no additional plumbing.
/// </summary>
[ApiController]
[Route("api/profile/groups")]
[Authorize]
public sealed class ProfileGroupsController : ControllerBase
{
    private readonly IGroupService _groups;
    public ProfileGroupsController(IGroupService groups) => _groups = groups;

    [HttpGet]
    public Task<List<ProfileMembershipDto>> ListAsync(CancellationToken ct)
        => _groups.ListMyMembershipsAsync(ct);

    [HttpPost("leave/{groupId:guid}")]
    public async Task<IActionResult> LeaveAsync(Guid groupId, CancellationToken ct)
    {
        var result = await _groups.LeaveAsync(groupId, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
