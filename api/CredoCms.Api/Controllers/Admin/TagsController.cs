using CredoCms.Application.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/tags")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class TagsController : ControllerBase
{
    private readonly ITagService _svc;
    public TagsController(ITagService svc) => _svc = svc;

    /// <summary>Autocomplete suggest endpoint for the SPA tag chip input.</summary>
    [HttpGet("search")]
    public Task<List<TagDto>> SearchAsync(
        [FromQuery] string q = "",
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
        => _svc.SearchAsync(q, limit, ct);
}
