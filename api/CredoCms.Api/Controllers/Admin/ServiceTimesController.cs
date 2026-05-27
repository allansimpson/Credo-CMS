using CredoCms.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/service-times")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class ServiceTimesController : ControllerBase
{
    private readonly IServiceTimeService _svc;
    public ServiceTimesController(IServiceTimeService svc) => _svc = svc;

    [HttpGet]
    public Task<List<ServiceTimeDto>> ListAsync([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        => _svc.ListAsync(includeDeleted, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceTimeDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var item = await _svc.GetAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceTimeDto>> CreateAsync([FromBody] CreateServiceTimeRequest request, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Item!.Id }, result.Item)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceTimeDto>> UpdateAsync(Guid id, [FromBody] UpdateServiceTimeRequest request, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, request, ct);
        if (result.Succeeded) return Ok(result.Item);
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
    public async Task<ActionResult<ServiceTimeDto>> RestoreAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.RestoreAsync(id, ct);
        return result.Succeeded ? Ok(result.Item) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/hard")]
    public async Task<ActionResult> HardDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.HardDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
