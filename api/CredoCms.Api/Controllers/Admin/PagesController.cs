using CredoCms.Application.Common;
using CredoCms.Application.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/pages")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class PagesController : ControllerBase
{
    private readonly IPageService _pages;

    public PagesController(IPageService pages) => _pages = pages;

    [HttpGet]
    public Task<PagedResult<PageListItemDto>> ListAsync([FromQuery] PageListQuery query, CancellationToken ct)
        => _pages.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PageDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var page = await _pages.GetAsync(id, includeDeleted: true, ct);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpPost]
    public async Task<ActionResult<PageDetailDto>> CreateAsync([FromBody] CreatePageRequest request, CancellationToken ct)
    {
        var result = await _pages.CreateAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Page!.Id }, result.Page)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PageDetailDto>> UpdateAsync(Guid id, [FromBody] UpdatePageRequest request, CancellationToken ct)
    {
        var result = await _pages.UpdateAsync(id, request, ct);
        if (result.Succeeded) return Ok(result.Page);
        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound(new { errors = result.Errors });
        return BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _pages.SoftDeleteAsync(id, ct);
        if (result.Succeeded) return NoContent();
        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound(new { errors = result.Errors });
        return BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<PageDetailDto>> RestoreAsync(Guid id, CancellationToken ct)
    {
        var result = await _pages.RestoreAsync(id, ct);
        return result.Succeeded
            ? Ok(result.Page)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/hard")]
    public async Task<ActionResult> HardDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _pages.HardDeleteAsync(id, ct);
        return result.Succeeded
            ? NoContent()
            : BadRequest(new { errors = result.Errors });
    }
}
