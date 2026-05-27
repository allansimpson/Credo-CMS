using CredoCms.Application.Groups;
using CredoCms.Domain.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

/// <summary>
/// Admin / editor surface for Groups. Permission rules — what each role can do
/// — live entirely in <see cref="IGroupService"/>; this controller only shapes
/// HTTP responses. The route gate here is "admin shell" (Administrator or
/// Editor); finer-grained checks (e.g. Administrator-only promote-to-leader)
/// are enforced by the service and surface as a 400 / forbidden message in
/// the body.
/// </summary>
[ApiController]
[Route("api/admin/groups")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class GroupsController : ControllerBase
{
    private readonly IGroupService _groups;
    public GroupsController(IGroupService groups) => _groups = groups;

    [HttpGet]
    public Task<List<AdminGroupListItemDto>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default)
        => _groups.ListAdminAsync(search, includeInactive, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminGroupDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var g = await _groups.GetAdminAsync(id, ct);
        return g is null ? NotFound() : Ok(g);
    }

    [HttpPost]
    public async Task<ActionResult<AdminGroupDetailDto>> CreateAsync([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        var result = await _groups.CreateAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Group!.Id }, result.Group)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminGroupDetailDto>> UpdateAsync(Guid id, [FromBody] UpdateGroupRequest request, CancellationToken ct)
    {
        var result = await _groups.UpdateAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Group) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _groups.SoftDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    // ---- roster -----------------------------------------------------------

    [HttpGet("{id:guid}/memberships")]
    public Task<List<AdminMembershipDto>> ListMembershipsAsync(
        Guid id,
        [FromQuery] GroupMembershipStatus? status,
        CancellationToken ct = default)
        => _groups.ListMembershipsAsync(id, status, ct);

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<AdminMembershipDto>> AddMemberAsync(
        Guid id,
        [FromBody] AddMemberRequest request,
        CancellationToken ct)
    {
        var result = await _groups.AddMemberAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Membership) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMemberAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var result = await _groups.RemoveMemberAsync(id, userId, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    public sealed record SetLeaderRequest(bool IsLeader);

    [HttpPut("{id:guid}/members/{userId:guid}/leader")]
    [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
    public async Task<ActionResult<AdminMembershipDto>> SetLeaderAsync(
        Guid id, Guid userId, [FromBody] SetLeaderRequest request, CancellationToken ct)
    {
        var result = await _groups.SetLeaderAsync(id, userId, request.IsLeader, ct);
        return result.Succeeded ? Ok(result.Membership) : BadRequest(new { errors = result.Errors });
    }

    // ---- pending requests --------------------------------------------------

    [HttpPost("memberships/{membershipId:guid}/approve")]
    public async Task<ActionResult<AdminMembershipDto>> ApproveAsync(Guid membershipId, CancellationToken ct)
    {
        var result = await _groups.ApproveJoinRequestAsync(membershipId, ct);
        return result.Succeeded ? Ok(result.Membership) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("memberships/{membershipId:guid}/decline")]
    public async Task<ActionResult<AdminMembershipDto>> DeclineAsync(Guid membershipId, CancellationToken ct)
    {
        var result = await _groups.DeclineJoinRequestAsync(membershipId, ct);
        return result.Succeeded ? Ok(result.Membership) : BadRequest(new { errors = result.Errors });
    }
}
