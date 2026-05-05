using CredoCms.Application.Common;
using CredoCms.Application.Sermons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/sermons")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class SermonsController : ControllerBase
{
    private readonly ISermonService _svc;
    public SermonsController(ISermonService svc) => _svc = svc;

    [HttpGet]
    public Task<PagedResult<SermonListItemDto>> ListAsync([FromQuery] SermonListQuery query, CancellationToken ct)
        => _svc.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SermonDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var sermon = await _svc.GetAsync(id, includeDeleted: true, ct);
        return sermon is null ? NotFound() : Ok(sermon);
    }

    [HttpPost]
    public async Task<ActionResult<SermonDetailDto>> CreateAsync([FromBody] CreateSermonRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(req, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Sermon!.Id }, result.Sermon)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SermonDetailDto>> UpdateAsync(Guid id, [FromBody] UpdateSermonRequest req, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, req, ct);
        if (result.Succeeded) return Ok(result.Sermon);
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
    public async Task<ActionResult<SermonDetailDto>> RestoreAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.RestoreAsync(id, ct);
        return result.Succeeded ? Ok(result.Sermon) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/hard")]
    public async Task<ActionResult> HardDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.HardDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
