using CredoCms.Application.Common;
using CredoCms.Application.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/search")]
public sealed class SearchAdminController : ControllerBase
{
    private readonly ISearchIndexer _search;
    private readonly IAuditLogger _audit;

    public SearchAdminController(ISearchIndexer search, IAuditLogger audit)
    {
        _search = search; _audit = audit;
    }

    /// <summary>Admin global search. Returns matches across all entity types
    /// (Page, NewsItem, Sermon, SermonSeries, Event, Leader, Document) and
    /// includes unpublished/draft entries — admins need to find work in
    /// progress.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminShell)]
    public Task<SearchResults> SearchAsync(
        [FromQuery] string q = "",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        => _search.SearchAllAsync(q, page, pageSize, ct);

    [HttpPost("rebuild")]
    [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
    public async Task<IActionResult> RebuildAsync(CancellationToken ct)
    {
        await _search.RebuildAllAsync(ct);
        await _audit.WriteAsync("Search.Rebuilt", "SearchIndex",
            details: new { TriggeredAt = DateTimeOffset.UtcNow }, cancellationToken: ct);
        return NoContent();
    }
}
