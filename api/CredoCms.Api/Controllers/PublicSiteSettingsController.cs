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
    /// <summary>The cookie the SPA writes on Accept; presence means the
    /// visitor has authorized non-essential tracking.</summary>
    private const string ConsentCookieName = "cms_consent";

    private readonly ISiteSettingsService _service;

    public PublicSiteSettingsController(ISiteSettingsService service) => _service = service;

    [HttpGet("public")]
    public async Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken ct)
    {
        var dto = await _service.GetPublicAsync(ct).ConfigureAwait(false);

        // Ga4MeasurementId is omitted from the response
        // until the visitor has accepted cookies. The cookie banner can
        // make its accept/decline decision from the AnalyticsProvider flag
        // alone; the loader needs the id, but only after consent has been
        // captured. The SPA re-fetches site-settings after Accept to
        // surface the now-populated id.
        if (!HasAcceptedConsent(Request))
        {
            dto = dto with { Ga4MeasurementId = null };
        }
        return dto;
    }

    private static bool HasAcceptedConsent(HttpRequest request) =>
        request.Cookies.TryGetValue(ConsentCookieName, out var v)
        && string.Equals(v, "accepted", StringComparison.Ordinal);
}
