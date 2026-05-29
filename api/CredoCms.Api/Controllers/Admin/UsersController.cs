using CredoCms.Application.Common;
using CredoCms.Application.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public sealed class UsersController : ControllerBase
{
    private readonly IUserAdminService _users;

    public UsersController(IUserAdminService users) => _users = users;

    [HttpGet]
    public Task<PagedResult<UserListItemDto>> ListAsync([FromQuery] UserListQuery query, CancellationToken ct)
        => _users.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await _users.GetAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDetailDto>> CreateAsync([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _users.CreateAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.User!.Id }, result.User)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDetailDto>> UpdateAsync(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _users.UpdateAsync(id, request, ct);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<UserDetailDto>> DeactivateAsync(Guid id, CancellationToken ct)
    {
        var result = await _users.DeactivateAsync(id, ct);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<UserDetailDto>> ReactivateAsync(Guid id, CancellationToken ct)
    {
        var result = await _users.ReactivateAsync(id, ct);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/force-logout")]
    public async Task<ActionResult<UserDetailDto>> ForceLogoutAsync(Guid id, CancellationToken ct)
    {
        var result = await _users.ForceLogoutAsync(id, ct);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/send-password-reset")]
    public async Task<ActionResult<UserDetailDto>> SendPasswordResetAsync(Guid id, CancellationToken ct)
    {
        var result = await _users.SendPasswordResetEmailAsync(id, ct);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/resend-invitation")]
    public async Task<ActionResult<UserDetailDto>> ResendInvitationAsync(Guid id, CancellationToken ct)
    {
        var result = await _users.ResendInvitationEmailAsync(id, ct);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> HardDeleteAsync(Guid id, [FromBody] HardDeleteUserRequest request, CancellationToken ct)
    {
        var result = await _users.HardDeleteAsync(id, request, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}/profile-fields")]
    public async Task<ActionResult<UserDetailDto>> UpdateProfileFieldsAsync(
        Guid id,
        [FromBody] UpdateUserProfileFieldsRequest request,
        CancellationToken ct)
    {
        var result = await _users.UpdateProfileFieldsAsync(id, request, ct);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/reset-notifications")]
    public async Task<ActionResult<UserDetailDto>> ResetNotificationsAsync(Guid id, CancellationToken ct)
    {
        var result = await _users.ResetNotificationsAsync(id, ct);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("{id:guid}/admin-notes")]
    public async Task<ActionResult<AdminUserNotesDto>> GetAdminNotesAsync(Guid id, CancellationToken ct)
    {
        var notes = await _users.GetAdminNotesAsync(id, ct);
        return notes is null ? NotFound() : Ok(notes);
    }
}
