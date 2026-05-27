using CredoCms.Application.Leaders;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/leaders")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class LeadersController : ControllerBase
{
    private readonly ILeaderService _svc;
    public LeadersController(ILeaderService svc) => _svc = svc;

    [HttpGet]
    public Task<List<LeaderDto>> ListAsync(CancellationToken ct) => _svc.ListAsync(ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaderDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var item = await _svc.GetAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<LeaderDto>> CreateAsync([FromBody] CreateLeaderRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(req, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Item!.Id }, result.Item)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeaderDto>> UpdateAsync(Guid id, [FromBody] UpdateLeaderRequest req, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, req, ct);
        if (result.Succeeded) return Ok(result.Item);
        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound(new { errors = result.Errors });
        return BadRequest(new { errors = result.Errors });
    }

    /// <summary>Hard-delete (Administrator only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = SystemConstants.Roles.Administrator)]
    public async Task<ActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _svc.DeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
