using CredoCms.Application.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

/// <summary>
/// Admin CRUD for class slots. Administrator-only — Editors can author public
/// content but slot configuration is treated as church operations and gated
/// to admins, matching the rule for Groups.
/// </summary>
[ApiController]
[Route("api/admin/class-slots")]
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public sealed class ClassSlotsController : ControllerBase
{
    private readonly IClassService _classes;
    public ClassSlotsController(IClassService classes) => _classes = classes;

    [HttpGet]
    public Task<List<AdminClassSlotListItemDto>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default)
        => _classes.ListSlotsAdminAsync(search, includeInactive, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminClassSlotDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var s = await _classes.GetSlotAdminAsync(id, ct);
        return s is null ? NotFound() : Ok(s);
    }

    [HttpPost]
    public async Task<ActionResult<AdminClassSlotDetailDto>> CreateAsync([FromBody] CreateClassSlotRequest request, CancellationToken ct)
    {
        var result = await _classes.CreateSlotAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Slot!.Id }, result.Slot)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminClassSlotDetailDto>> UpdateAsync(
        Guid id, [FromBody] UpdateClassSlotRequest request, CancellationToken ct)
    {
        var result = await _classes.UpdateSlotAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Slot) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _classes.SoftDeleteSlotAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
