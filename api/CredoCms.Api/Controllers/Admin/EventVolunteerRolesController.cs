using CredoCms.Application.Volunteers;
using CredoCms.Domain.Volunteers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/events/{eventId:guid}/volunteer-roles")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class EventVolunteerRolesController : ControllerBase
{
    private readonly IEventVolunteerService _service;
    public EventVolunteerRolesController(IEventVolunteerService service) => _service = service;

    [HttpGet]
    public Task<List<EventVolunteerRole>> ListAsync(Guid eventId, CancellationToken ct)
        => _service.ListRolesAsync(eventId, ct);

    [HttpPost]
    public async Task<ActionResult<EventVolunteerRole>> CreateAsync(Guid eventId, [FromBody] CreateRoleRequest input, CancellationToken ct)
    {
        try { return Ok(await _service.CreateRoleAsync(eventId, input, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { errors = new[] { ex.Message } }); }
    }

    [HttpPut("{roleId:guid}")]
    public async Task<ActionResult<EventVolunteerRole>> UpdateAsync(Guid roleId, [FromBody] UpdateRoleRequest input, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateRoleAsync(roleId, input, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { errors = new[] { ex.Message } }); }
    }

    [HttpDelete("{roleId:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid roleId, CancellationToken ct)
    {
        await _service.DeleteRoleAsync(roleId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/admin/events/{eventId:guid}/volunteer-signups")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class EventVolunteerSignupsAdminController : ControllerBase
{
    private readonly IEventVolunteerService _service;
    public EventVolunteerSignupsAdminController(IEventVolunteerService service) => _service = service;

    [HttpGet]
    public Task<List<EventVolunteerSignup>> ListAsync(
        Guid eventId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
        => _service.ListEventSignupsAsync(eventId, from, to, ct);
}

[ApiController]
[Route("api/events/{eventId:guid}/volunteer")]
public sealed class EventVolunteerSignupsController : ControllerBase
{
    private readonly IEventVolunteerService _service;
    public EventVolunteerSignupsController(IEventVolunteerService service) => _service = service;

    [HttpPost("signup")]
    [Authorize]
    public async Task<ActionResult<EventVolunteerSignup>> SignUpAsync(
        Guid eventId, [FromBody] SignUpRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.SignUpAsync(req.RoleId, req.OccurrenceDate, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { errors = new[] { ex.Message } }); }
    }

    [HttpPost("cancel")]
    [Authorize]
    public async Task<ActionResult> CancelAsync(
        Guid eventId, [FromBody] CancelSignupRequest req, CancellationToken ct)
    {
        try { await _service.CancelSignupAsync(req.SignupId, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { errors = new[] { ex.Message } }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    public sealed record SignUpRequest(Guid RoleId, DateOnly OccurrenceDate);
    public sealed record CancelSignupRequest(Guid SignupId);
}

[ApiController]
[Route("api/profile/volunteer")]
[Authorize]
public sealed class ProfileVolunteerController : ControllerBase
{
    private readonly IEventVolunteerService _service;
    public ProfileVolunteerController(IEventVolunteerService service) => _service = service;

    [HttpGet]
    public Task<List<EventVolunteerSignup>> MyUpcomingAsync(CancellationToken ct) =>
        _service.ListMyUpcomingAsync(ct);
}
