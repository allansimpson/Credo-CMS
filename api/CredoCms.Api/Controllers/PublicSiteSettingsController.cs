using CredoCms.Application.SiteSettingsManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

/// <summary>
/// Returns the public-facing subset of Site Settings (branding, contact info,
/// social links). Anonymous-accessible — used by the SPA on every page load to
/// drive theming and footer.
/// </summary>
[ApiController]
[Route("api/site-settings")]
[AllowAnonymous]
public sealed class PublicSiteSettingsController : ControllerBase
{
    private readonly ISiteSettingsService _service;

    public PublicSiteSettingsController(ISiteSettingsService service) => _service = service;

    [HttpGet("public")]
    public Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken ct) => _service.GetPublicAsync(ct);
}
