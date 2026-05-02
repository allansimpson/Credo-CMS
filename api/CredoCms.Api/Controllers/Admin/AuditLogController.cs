using CredoCms.Application.Auditing;
using CredoCms.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-log")]
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public sealed class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLog;

    public AuditLogController(IAuditLogService auditLog) => _auditLog = auditLog;

    [HttpGet]
    public Task<PagedResult<AuditLogEntryDto>> ListAsync([FromQuery] AuditLogQuery query, CancellationToken ct)
        => _auditLog.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditLogEntryDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var entry = await _auditLog.GetAsync(id, ct);
        return entry is null ? NotFound() : Ok(entry);
    }
}
