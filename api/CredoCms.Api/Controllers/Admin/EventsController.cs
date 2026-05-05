using CredoCms.Application.Common;
using CredoCms.Application.Events;
using CredoCms.Domain.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/events")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class EventsController : ControllerBase
{
    private readonly IEventService _svc;
    public EventsController(IEventService svc) => _svc = svc;

    [HttpGet]
    public Task<PagedResult<EventListItemDto>> ListAsync([FromQuery] EventListQuery query, CancellationToken ct)
        => _svc.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var evt = await _svc.GetAsync(id, includeDeleted: true, ct);
        return evt is null ? NotFound() : Ok(evt);
    }

    [HttpPost]
    public async Task<ActionResult<EventDetailDto>> CreateAsync([FromBody] CreateEventRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(req, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Event!.Id }, result.Event)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventDetailDto>> UpdateAsync(Guid id, [FromBody] UpdateEventRequest req, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, req, ct);
        if (result.Succeeded) return Ok(result.Event);
        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound(new { errors = result.Errors });
        return BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.SoftDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<EventDetailDto>> RestoreAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.RestoreAsync(id, ct);
        return result.Succeeded ? Ok(result.Event) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/hard")]
    public async Task<ActionResult> HardDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.HardDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    public sealed record SkipOccurrenceRequest(DateOnly Date, string? Reason);

    [HttpPost("{id:guid}/skip-occurrence")]
    public async Task<ActionResult> SkipOccurrenceAsync(Guid id, [FromBody] SkipOccurrenceRequest req, CancellationToken ct)
    {
        await _svc.SkipOccurrenceAsync(id, req.Date, req.Reason, ct);
        return NoContent();
    }

    public sealed record OverrideRequest(
        DateOnly OriginalOccurrenceDate,
        DateTimeOffset? OverrideStartsAt,
        DateTimeOffset? OverrideEndsAt,
        string? OverrideLocation,
        string? OverrideDescriptionJson,
        bool IsCanceled);

    [HttpPost("{id:guid}/occurrence-override")]
    public async Task<ActionResult> OverrideOccurrenceAsync(Guid id, [FromBody] OverrideRequest req, CancellationToken ct)
    {
        await _svc.SaveOccurrenceOverrideAsync(new EventOccurrenceOverride
        {
            EventId = id,
            OriginalOccurrenceDate = req.OriginalOccurrenceDate,
            OverrideStartsAt = req.OverrideStartsAt,
            OverrideEndsAt = req.OverrideEndsAt,
            OverrideLocation = req.OverrideLocation,
            OverrideDescriptionJson = req.OverrideDescriptionJson,
            IsCanceled = req.IsCanceled,
        }, ct);
        return NoContent();
    }
}
