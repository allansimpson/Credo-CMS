using CredoCms.Application.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

/// <summary>
/// Generic version-history controller. Routes look like:
///   GET    /api/admin/{entityType}/{id}/history
///   GET    /api/admin/{entityType}/{id}/history/asof?ts=...
///   POST   /api/admin/{entityType}/{id}/history/restore (body: { asOf })
///
/// Unknown entity types or missing entities return 404 (covert), so admin
/// callers fishing for entity types they shouldn't know about don't get a
/// signal.
/// </summary>
[ApiController]
[Route("api/admin/{entityType}/{id:guid}/history")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class VersionHistoryController : ControllerBase
{
    private readonly IVersionedEntityHandlerRegistry _registry;
    public VersionHistoryController(IVersionedEntityHandlerRegistry registry) => _registry = registry;

    [HttpGet]
    public async Task<ActionResult<List<VersionListItem>>> ListAsync(string entityType, Guid id, CancellationToken ct)
    {
        var handler = _registry.Resolve(entityType);
        if (handler is null) return NotFound();
        var items = await handler.ListAsync(id, ct);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpGet("asof")]
    public async Task<ActionResult<VersionSnapshot>> GetAsOfAsync(
        string entityType, Guid id, [FromQuery] DateTimeOffset ts, CancellationToken ct)
    {
        var handler = _registry.Resolve(entityType);
        if (handler is null) return NotFound();
        var snap = await handler.GetAsOfAsync(id, ts, ct);
        return snap is null ? NotFound() : Ok(snap);
    }

    public sealed record RestoreRequest(DateTimeOffset AsOf);

    [HttpPost("restore")]
    public async Task<ActionResult<VersionRestoreResult>> RestoreAsync(
        string entityType, Guid id, [FromBody] RestoreRequest req, CancellationToken ct)
    {
        var handler = _registry.Resolve(entityType);
        if (handler is null) return NotFound();
        var result = await handler.RestoreAsync(id, req.AsOf, ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
