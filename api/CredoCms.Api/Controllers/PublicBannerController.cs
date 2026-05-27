using CredoCms.Application.Announcements;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/banner")]
public sealed class PublicBannerController : ControllerBase
{
    private readonly IAnnouncementBannerService _svc;
    public PublicBannerController(IAnnouncementBannerService svc) => _svc = svc;

    /// <summary>Returns 200 with the active banner DTO, or 204 if no banner is currently active.</summary>
    [HttpGet]
    public async Task<ActionResult<PublicAnnouncementBannerDto?>> GetAsync(CancellationToken ct)
    {
        var b = await _svc.GetActivePublicAsync(ct);
        return b is null ? NoContent() : Ok(b);
    }
}
