using CredoCms.Application.Common;
using CredoCms.Application.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/search")]
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public sealed class SearchAdminController : ControllerBase
{
    private readonly ISearchIndexer _search;
    private readonly IAuditLogger _audit;

    public SearchAdminController(ISearchIndexer search, IAuditLogger audit)
    {
        _search = search; _audit = audit;
    }

    [HttpPost("rebuild")]
    public async Task<IActionResult> RebuildAsync(CancellationToken ct)
    {
        await _search.RebuildAllAsync(ct);
        await _audit.WriteAsync("Search.Rebuilt", "SearchIndex",
            details: new { TriggeredAt = DateTimeOffset.UtcNow }, cancellationToken: ct);
        return NoContent();
    }
}
