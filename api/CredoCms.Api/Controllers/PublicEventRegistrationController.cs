using System.Security.Claims;
using System.Threading.RateLimiting;
using CredoCms.Application.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/events/{slug}/register")]
public sealed class PublicEventRegistrationController : ControllerBase
{
    private readonly IEventRegistrationService _registrations;
    private readonly IEventService _events;
    private readonly IRegistrationTokenSigner _tokens;

    public PublicEventRegistrationController(
        IEventRegistrationService registrations,
        IEventService events,
        IRegistrationTokenSigner tokens)
    {
        _registrations = registrations;
        _events = events;
        _tokens = tokens;
    }

    [HttpPost]
    [EnableRateLimiting("event-register")]
    public async Task<ActionResult<SubmitRegistrationResponse>> SubmitAsync(
        string slug,
        [FromBody] SubmitRegistrationRequest req,
        CancellationToken ct)
    {
        var evt = await _events.GetPublicBySlugAsync(slug, includeMembersOnly: true, ct);
        if (evt is null) return NotFound();

        Guid? userId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var uid)) userId = uid;

        var result = await _registrations.SubmitAsync(evt.Id, req, userId, ct);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });

        var token = _tokens.Sign(result.Registration!.Id, TimeSpan.FromDays(60));
        return Ok(new SubmitRegistrationResponse(result.Registration, token));
    }

    [HttpGet("cancel")]
    public ActionResult ValidateCancelToken([FromQuery] string token)
    {
        if (!_tokens.TryValidate(token, out _))
            return BadRequest(new { errors = new[] { "Invalid or expired cancel link." } });
        return Ok(new { ok = true });
    }

    public sealed record CancelRequest(string Token, string? Reason);

    [HttpPost("cancel")]
    public async Task<ActionResult> CancelAsync(string slug, [FromBody] CancelRequest req, CancellationToken ct)
    {
        if (!_tokens.TryValidate(req.Token, out var registrationId))
            return BadRequest(new { errors = new[] { "Invalid or expired cancel link." } });
        var ok = await _registrations.CancelAsync(registrationId, req.Reason, ct);
        return ok ? NoContent() : NotFound();
    }
}

public sealed record SubmitRegistrationResponse(RegistrationDto Registration, string CancelToken);
