using CredoCms.Application.Sermons;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/sermons/series")]
public sealed class PublicSermonSeriesController : ControllerBase
{
    private readonly ISermonSeriesService _svc;
    public PublicSermonSeriesController(ISermonSeriesService svc) => _svc = svc;

    [HttpGet]
    public Task<List<PublicSermonSeriesDto>> ListAsync(CancellationToken ct) => _svc.ListPublicAsync(ct);

    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicSermonSeriesDto>> GetAsync(string slug, CancellationToken ct)
    {
        var series = await _svc.GetPublicBySlugAsync(slug, ct);
        return series is null ? NotFound() : Ok(series);
    }
}
