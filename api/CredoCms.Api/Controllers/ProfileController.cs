using System.Security.Claims;
using CredoCms.Application.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Self-profile endpoints. Every action resolves the caller's user id from the
/// ClaimsPrincipal and passes it to <see cref="IProfileService"/>; the service
/// performs the per-user authorization check there. The controller is therefore
/// thin — its only job is claim → guid plumbing and HTTP shape.
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IProfileService _profile;
    public ProfileController(IProfileService profile) => _profile = profile;

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var profile = await _profile.GetProfileAsync(userId, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("personal")]
    public async Task<ActionResult<ProfileDto>> UpdatePersonalAsync(
        [FromBody] UpdatePersonalInfoRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _profile.UpdatePersonalInfoAsync(userId, request, ct);
        return result.Succeeded
            ? Ok(result.Profile)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("directory")]
    public async Task<ActionResult<ProfileDto>> UpdateDirectoryAsync(
        [FromBody] UpdateDirectoryRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _profile.UpdateDirectoryAsync(userId, request, ct);
        return result.Succeeded
            ? Ok(result.Profile)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("notifications")]
    public async Task<ActionResult<ProfileDto>> UpdateNotificationsAsync(
        [FromBody] UpdateNotificationsRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _profile.UpdateNotificationsAsync(userId, request, ct);
        return result.Succeeded
            ? Ok(result.Profile)
            : BadRequest(new { errors = result.Errors });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }
}
