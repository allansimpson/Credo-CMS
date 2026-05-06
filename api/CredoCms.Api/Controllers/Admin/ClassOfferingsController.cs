using CredoCms.Application.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

/// <summary>
/// Admin CRUD for class offerings (the rotating curricula filling each
/// <see cref="Application.Classes.AdminClassSlotDetailDto"/>). Administrator-
/// only; same gate as the slot controller.
/// </summary>
[ApiController]
[Route("api/admin/class-offerings")]
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public sealed class ClassOfferingsController : ControllerBase
{
    private readonly IClassService _classes;
    public ClassOfferingsController(IClassService classes) => _classes = classes;

    [HttpGet]
    public Task<List<AdminClassOfferingDto>> ListAsync(
        [FromQuery] Guid? classSlotId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] OfferingStatusFilter status = OfferingStatusFilter.All,
        CancellationToken ct = default)
        => _classes.ListOfferingsAdminAsync(new AdminClassOfferingsQuery(classSlotId, fromDate, toDate, status), ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminClassOfferingDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var o = await _classes.GetOfferingAdminAsync(id, ct);
        return o is null ? NotFound() : Ok(o);
    }

    [HttpPost]
    public async Task<ActionResult<AdminClassOfferingDto>> CreateAsync([FromBody] CreateClassOfferingRequest request, CancellationToken ct)
    {
        var result = await _classes.CreateOfferingAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Offering!.Id }, result.Offering)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminClassOfferingDto>> UpdateAsync(
        Guid id, [FromBody] UpdateClassOfferingRequest request, CancellationToken ct)
    {
        var result = await _classes.UpdateOfferingAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Offering) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _classes.SoftDeleteOfferingAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
