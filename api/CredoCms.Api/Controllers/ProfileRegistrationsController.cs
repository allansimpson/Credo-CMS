using System.Security.Claims;
using CredoCms.Application.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/profile/registrations")]
[Authorize]
public sealed class ProfileRegistrationsController : ControllerBase
{
    private readonly IEventRegistrationService _svc;
    public ProfileRegistrationsController(IEventRegistrationService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<List<MyRegistrationDto>>> ListAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _svc.ListMyRegistrationsAsync(userId, ct));
    }

    public sealed record CancelRequest(string? Reason);

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> CancelAsync(Guid id, [FromBody] CancelRequest? req, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await _svc.CancelMyRegistrationAsync(userId, id, req?.Reason, ct)
            ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }
}
