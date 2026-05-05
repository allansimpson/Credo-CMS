using CredoCms.Application.Search;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/search")]
public sealed class PublicSearchController : ControllerBase
{
    private readonly ISearchIndexer _search;
    public PublicSearchController(ISearchIndexer search) => _search = search;

    [HttpGet]
    public Task<SearchResults> SearchAsync(
        [FromQuery] string q = "",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => _search.SearchAsync(q, IsAuthenticatedMember(), page, pageSize, ct);

    private bool IsAuthenticatedMember()
    {
        if (User?.Identity?.IsAuthenticated != true) return false;
        return User.IsInRole(SystemConstants.Roles.Member)
            || User.IsInRole(SystemConstants.Roles.Editor)
            || User.IsInRole(SystemConstants.Roles.Administrator);
    }
}
