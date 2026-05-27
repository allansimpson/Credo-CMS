using CredoCms.Application.Prayer;
using CredoCms.Domain.Prayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

/// <summary>
/// Admin moderation surface for prayer requests. Editor + Administrator
/// (the AdminShell policy). Service enforces the same permission set.
/// </summary>
[ApiController]
[Route("api/admin/prayer-requests")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class PrayerRequestsController : ControllerBase
{
    private readonly IPrayerRequestService _prayer;
    public PrayerRequestsController(IPrayerRequestService prayer) => _prayer = prayer;

    [HttpGet]
    public Task<List<AdminPrayerRequestDto>> ListAsync(
        [FromQuery] PrayerRequestStatus? status,
        [FromQuery] bool? isAnonymous,
        [FromQuery] string? search,
        CancellationToken ct = default)
        => _prayer.ListAdminAsync(new AdminPrayerListQuery(status, isAnonymous, search), ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminPrayerRequestDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var detail = await _prayer.GetAdminAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("{id:guid}/updates")]
    public async Task<ActionResult<AdminPrayerRequestDto>> AddUpdateAsync(
        Guid id, [FromBody] AddPrayerUpdateRequest request, CancellationToken ct)
    {
        var result = await _prayer.AddUpdateAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Request) : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<AdminPrayerRequestDto>> ChangeStatusAsync(
        Guid id, [FromBody] ChangePrayerStatusRequest request, CancellationToken ct)
    {
        var result = await _prayer.ChangeStatusAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Request) : BadRequest(new { errors = result.Errors });
    }

    public sealed record BulkArchiveRequest(IReadOnlyList<Guid> Ids);

    [HttpPost("bulk-archive")]
    public async Task<ActionResult<int>> BulkArchiveAsync([FromBody] BulkArchiveRequest request, CancellationToken ct)
    {
        var moved = await _prayer.BulkArchiveAsync(request.Ids ?? Array.Empty<Guid>(), ct);
        return Ok(new { count = moved });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _prayer.DeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
