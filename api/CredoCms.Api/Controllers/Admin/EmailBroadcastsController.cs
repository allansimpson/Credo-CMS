using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/broadcasts")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class EmailBroadcastsController : ControllerBase
{
    private readonly IEmailBroadcastService _service;
    private readonly IEmailBroadcastRepository _broadcasts;
    private readonly IEmailBroadcastRecipientRepository _recipients;

    public EmailBroadcastsController(
        IEmailBroadcastService service,
        IEmailBroadcastRepository broadcasts,
        IEmailBroadcastRecipientRepository recipients)
    {
        _service = service;
        _broadcasts = broadcasts;
        _recipients = recipients;
    }

    [HttpGet]
    public Task<PagedResult<EmailBroadcast>> ListAsync(
        [FromQuery] BroadcastStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => _broadcasts.ListAsync(status, page, pageSize, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmailBroadcast>> GetAsync(Guid id, CancellationToken ct)
    {
        var b = await _broadcasts.GetAsync(id, ct);
        return b is null ? NotFound() : Ok(b);
    }

    [HttpGet("{id:guid}/recipients")]
    public Task<PagedResult<EmailBroadcastRecipient>> RecipientsAsync(
        Guid id,
        [FromQuery] RecipientStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => _recipients.ListAsync(id, status, page, pageSize, ct);

    [HttpPost("{id:guid}/preview-recipients")]
    public Task<RecipientPreview> PreviewAsync(Guid id, CancellationToken ct)
        => _service.PreviewRecipientsAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<EmailBroadcast>> CreateAsync([FromBody] BroadcastDraftInput input, CancellationToken ct)
    {
        var b = await _service.CreateDraftAsync(input, ct);
        return CreatedAtAction(nameof(GetAsync), new { id = b.Id }, b);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmailBroadcast>> UpdateAsync(Guid id, [FromBody] BroadcastDraftInput input, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateDraftAsync(id, input, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { errors = new[] { ex.Message } }); }
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<EmailBroadcast>> SendAsync(Guid id, CancellationToken ct)
    {
        try { return Ok(await _service.SendNowAsync(id, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { errors = new[] { ex.Message } }); }
    }

    [HttpPost("{id:guid}/schedule")]
    public async Task<ActionResult<EmailBroadcast>> ScheduleAsync(Guid id, [FromBody] ScheduleRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.ScheduleAsync(id, req.SendAt, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { errors = new[] { ex.Message } }); }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> CancelAsync(Guid id, CancellationToken ct)
    {
        try { await _service.CancelAsync(id, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { errors = new[] { ex.Message } }); }
    }

    public sealed record ScheduleRequest(DateTimeOffset SendAt);
}
