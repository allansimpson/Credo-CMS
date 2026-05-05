using System.Text;
using CredoCms.Application.Events;
using CredoCms.Domain.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/events/{eventId:guid}")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class EventRegistrationsAdminController : ControllerBase
{
    private readonly IEventRegistrationService _svc;
    public EventRegistrationsAdminController(IEventRegistrationService svc) => _svc = svc;

    [HttpGet("registration-fields")]
    public Task<List<RegistrationFieldDto>> ListFieldsAsync(Guid eventId, CancellationToken ct)
        => _svc.ListFieldsAsync(eventId, ct);

    [HttpPost("registration-fields")]
    public async Task<ActionResult<RegistrationFieldDto>> AddFieldAsync(Guid eventId, [FromBody] CreateRegistrationFieldRequest req, CancellationToken ct)
    {
        var f = await _svc.AddFieldAsync(eventId, req, ct);
        return f is null ? BadRequest() : Ok(f);
    }

    [HttpPut("registration-fields/{fieldId:guid}")]
    public async Task<ActionResult<RegistrationFieldDto>> UpdateFieldAsync(Guid fieldId, [FromBody] CreateRegistrationFieldRequest req, CancellationToken ct)
    {
        var f = await _svc.UpdateFieldAsync(fieldId, req, ct);
        return f is null ? NotFound() : Ok(f);
    }

    [HttpDelete("registration-fields/{fieldId:guid}")]
    public async Task<ActionResult> RemoveFieldAsync(Guid fieldId, CancellationToken ct)
        => await _svc.RemoveFieldAsync(fieldId, ct) ? NoContent() : NotFound();

    [HttpGet("registrations")]
    public Task<List<RegistrationDto>> ListAsync(Guid eventId,
        [FromQuery] EventRegistrationStatus? status, CancellationToken ct)
        => _svc.ListForEventAsync(eventId, status, ct);

    [HttpGet("registrations/{regId:guid}")]
    public async Task<ActionResult<RegistrationDto>> GetAsync(Guid regId, CancellationToken ct)
    {
        var r = await _svc.GetAsync(regId, ct);
        return r is null ? NotFound() : Ok(r);
    }

    public sealed record CancelRequest(string? Reason);

    [HttpPost("registrations/{regId:guid}/cancel")]
    public async Task<ActionResult> CancelAsync(Guid regId, [FromBody] CancelRequest? req, CancellationToken ct)
        => await _svc.CancelAsync(regId, req?.Reason, ct) ? NoContent() : NotFound();

    [HttpPost("registrations/{regId:guid}/resend-confirmation")]
    public async Task<ActionResult> ResendAsync(Guid regId, CancellationToken ct)
        => await _svc.ResendConfirmationAsync(regId, ct) ? NoContent() : NotFound();

    [HttpGet("registrations/export.csv")]
    public async Task<IActionResult> ExportCsvAsync(Guid eventId, CancellationToken ct)
    {
        var csv = await _svc.ExportCsvAsync(eventId, ct);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"event-{eventId}-registrations.csv");
    }
}
