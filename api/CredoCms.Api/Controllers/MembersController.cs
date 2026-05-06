using CredoCms.Application.Common;
using CredoCms.Application.Members;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Authenticated members directory. Privacy is enforced in two layers:
///   • <see cref="MembersDirectoryQueries"/> only returns rows where
///     <c>IsListedInDirectory &amp;&amp; IsActive</c>.
///   • <see cref="MembersDirectoryService"/> nulls out fields the member did
///     not opt in to share.
/// The controller is therefore thin — its only job is the role gate.
/// </summary>
[ApiController]
[Route("api/members")]
[Authorize(Roles = SystemConstants.Roles.Member + "," + SystemConstants.Roles.Editor + "," + SystemConstants.Roles.Administrator)]
public sealed class MembersController : ControllerBase
{
    private readonly IMembersDirectoryService _members;
    public MembersController(IMembersDirectoryService members) => _members = members;

    [HttpGet]
    public Task<PagedResult<MemberListItemDto>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken ct = default)
        => _members.ListAsync(new MembersDirectoryQuery(search, page, pageSize), ct);

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<MemberDetailDto>> GetAsync(Guid userId, CancellationToken ct)
    {
        var detail = await _members.GetByIdAsync(userId, ct);
        // Returning 404 for both "doesn't exist" and "not opted into directory"
        // keeps the API from doubling as a user-id probe.
        return detail is null ? NotFound() : Ok(detail);
    }
}
