using CredoCms.Application.Common;
using CredoCms.Application.Sermons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/sermon-series")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class SermonSeriesController : ControllerBase
{
    private readonly ISermonSeriesService _svc;
    public SermonSeriesController(ISermonSeriesService svc) => _svc = svc;

    [HttpGet]
    public Task<PagedResult<SermonSeriesListItemDto>> ListAsync([FromQuery] SermonSeriesListQuery query, CancellationToken ct)
        => _svc.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SermonSeriesDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var series = await _svc.GetAsync(id, includeDeleted: true, ct);
        return series is null ? NotFound() : Ok(series);
    }

    [HttpPost]
    public async Task<ActionResult<SermonSeriesDetailDto>> CreateAsync([FromBody] CreateSermonSeriesRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(req, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Series!.Id }, result.Series)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SermonSeriesDetailDto>> UpdateAsync(Guid id, [FromBody] UpdateSermonSeriesRequest req, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, req, ct);
        if (result.Succeeded) return Ok(result.Series);
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
    public async Task<ActionResult<SermonSeriesDetailDto>> RestoreAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.RestoreAsync(id, ct);
        return result.Succeeded ? Ok(result.Series) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/hard")]
    public async Task<ActionResult> HardDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.HardDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
