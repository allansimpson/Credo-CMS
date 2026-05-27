using CredoCms.Application.Prayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Member-facing prayer request endpoints. The service uses the
/// <see cref="Application.Common.ICurrentUserService"/>-resolved caller
/// for every permission check; the controller is purely HTTP shape.
/// </summary>
[ApiController]
[Route("api/prayer-requests")]
[Authorize]
public sealed class MemberPrayerRequestsController : ControllerBase
{
    private readonly IPrayerRequestService _prayer;
    public MemberPrayerRequestsController(IPrayerRequestService prayer) => _prayer = prayer;

    [HttpGet]
    public Task<List<PrayerRequestListItemDto>> ListAsync(CancellationToken ct)
        => _prayer.ListMemberVisibleAsync(ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MemberPrayerRequestDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var detail = await _prayer.GetMemberAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitAsync([FromBody] SubmitPrayerRequestRequest request, CancellationToken ct)
    {
        var result = await _prayer.SubmitAsync(request, ct);
        // We return the admin DTO from the service for consistency, but
        // expose only the member shape over HTTP. The new id is enough for
        // the SPA — it will refetch the member view to pick up display rules.
        return result.Succeeded
            ? Ok(new { id = result.Request!.Id })
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> EditAsync(Guid id, [FromBody] EditPrayerRequestRequest request, CancellationToken ct)
    {
        var result = await _prayer.EditAsync(id, request, ct);
        return result.Succeeded
            ? Ok(new { id = result.Request!.Id })
            : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _prayer.DeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/prayed")]
    public async Task<ActionResult<int>> MarkPrayedAsync(Guid id, CancellationToken ct)
        => Ok(new { count = await _prayer.MarkPrayedForAsync(id, ct) });

    [HttpDelete("{id:guid}/prayed")]
    public async Task<ActionResult<int>> UnmarkPrayedAsync(Guid id, CancellationToken ct)
        => Ok(new { count = await _prayer.UnmarkPrayedForAsync(id, ct) });
}
