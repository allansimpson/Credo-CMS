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

    /// <summary>
    /// Returns the public DTO for a page regardless of published/members-only
    /// state. Used by the editor's "Preview" button so admins can see drafts
    /// rendered through the public template machinery.
    /// </summary>
    [HttpGet("preview/{slug}")]
    public async Task<ActionResult<PublicPageDto>> GetPreviewAsync(string slug, CancellationToken ct)
    {
        var page = await _pages.GetPreviewBySlugAsync(slug, ct);
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

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<PageDetailDto>> PublishAsync(Guid id, CancellationToken ct)
    {
        var result = await _pages.PublishAsync(id, ct);
        if (result.Succeeded) return Ok(result.Page);
        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound(new { errors = result.Errors });
        return BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/unpublish")]
    public async Task<ActionResult<PageDetailDto>> UnpublishAsync(Guid id, CancellationToken ct)
    {
        var result = await _pages.UnpublishAsync(id, ct);
        if (result.Succeeded) return Ok(result.Page);
        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound(new { errors = result.Errors });
        return BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/discard-draft")]
    public async Task<ActionResult<PageDetailDto>> DiscardDraftAsync(Guid id, CancellationToken ct)
    {
        var result = await _pages.DiscardDraftAsync(id, ct);
        if (result.Succeeded) return Ok(result.Page);
        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound(new { errors = result.Errors });
        return BadRequest(new { errors = result.Errors });
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
