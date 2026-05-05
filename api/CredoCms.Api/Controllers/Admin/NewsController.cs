using CredoCms.Application.Common;
using CredoCms.Application.News;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/news")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class NewsController : ControllerBase
{
    private readonly INewsService _news;

    public NewsController(INewsService news) => _news = news;

    [HttpGet]
    public Task<PagedResult<NewsListItemDto>> ListAsync([FromQuery] NewsListQuery query, CancellationToken ct)
        => _news.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NewsDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var item = await _news.GetAsync(id, includeDeleted: true, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<NewsDetailDto>> CreateAsync([FromBody] CreateNewsItemRequest request, CancellationToken ct)
    {
        var result = await _news.CreateAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Item!.Id }, result.Item)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NewsDetailDto>> UpdateAsync(Guid id, [FromBody] UpdateNewsItemRequest request, CancellationToken ct)
    {
        var result = await _news.UpdateAsync(id, request, ct);
        if (result.Succeeded) return Ok(result.Item);
        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound(new { errors = result.Errors });
        return BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _news.SoftDeleteAsync(id, ct);
        if (result.Succeeded) return NoContent();
        return BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<NewsDetailDto>> RestoreAsync(Guid id, CancellationToken ct)
    {
        var result = await _news.RestoreAsync(id, ct);
        return result.Succeeded ? Ok(result.Item) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/hard")]
    public async Task<ActionResult> HardDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _news.HardDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
