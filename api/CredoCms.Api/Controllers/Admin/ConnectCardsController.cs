using CredoCms.Application.ConnectCard;
using CredoCms.Domain.ConnectCard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

/// <summary>
/// Admin moderation surface for connect cards. AdminShell policy
/// (Editor + Administrator); the service double-gates each mutation.
/// </summary>
[ApiController]
[Route("api/admin/connect-cards")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class ConnectCardsController : ControllerBase
{
    private readonly IConnectCardService _service;
    public ConnectCardsController(IConnectCardService service) => _service = service;

    [HttpGet]
    public Task<List<AdminConnectCardListItemDto>> ListAsync(
        [FromQuery] ConnectCardStatus? status,
        [FromQuery] bool? isFirstTimeVisitor,
        [FromQuery] string? search,
        CancellationToken ct = default)
        => _service.ListAdminAsync(new AdminConnectCardListQuery(status, isFirstTimeVisitor, search), ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminConnectCardDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var detail = await _service.GetAdminAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<AdminConnectCardDetailDto>> UpdateStatusAsync(
        Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateStatusAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}/notes")]
    public async Task<ActionResult<AdminConnectCardDetailDto>> UpdateNotesAsync(
        Guid id, [FromBody] UpdateNotesRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateNotesAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/resend")]
    public async Task<IActionResult> ResendAsync(Guid id, CancellationToken ct)
    {
        var ok = await _service.ResendAcknowledgmentAsync(id, ct);
        return ok ? NoContent() : BadRequest(new { errors = new[] { "Could not resend acknowledgment." } });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var ok = await _service.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
