using CredoCms.Application.Caching;
using CredoCms.Application.Sermons;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/sermons/series")]
public sealed class PublicSermonSeriesController : ControllerBase
{
    private readonly ISermonSeriesService _svc;
    public PublicSermonSeriesController(ISermonSeriesService svc) => _svc = svc;

    [HttpGet]
    public Task<List<PublicSermonSeriesDto>> ListAsync(CancellationToken ct) => _svc.ListPublicAsync(ct);

    /// <summary>
    /// Richer projection used by the public by-series browse page. Adds
    /// counts, the active/complete flag, the flagship "latest sermon" for
    /// the hero, the truncated description, and the derived scope label.
    /// Cached 60s; busted on sermon/series publish.
    /// </summary>
    [HttpGet("with-stats")]
    [OutputCache(Duration = 60, Tags = new[] { OutputCacheTags.SermonSeries, OutputCacheTags.Sermons })]
    public Task<List<PublicSermonSeriesWithStatsDto>> ListWithStatsAsync(CancellationToken ct)
        => _svc.ListPublicWithStatsAsync(ct);

    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicSermonSeriesDto>> GetAsync(string slug, CancellationToken ct)
    {
        var series = await _svc.GetPublicBySlugAsync(slug, ct);
        return series is null ? NotFound() : Ok(series);
    }
}
